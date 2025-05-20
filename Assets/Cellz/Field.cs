// Field.cs

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages an area of "cells":
///  - Per-cell inner & outer radius
///  - Collision damping
///  - Click-to-select (left-click)
///  - Right-click to split (calls cell.Split())
///  - WASD/zoom + drag to pan the Voronoi region
///  - Per-pixel Voronoi coloring & border detection
///  - Overlap-based repulsive forces
///  - AddCell(...) and RemoveCell(...)
///  - Now delegates per-cell ID pass to Cell.Render()
/// </summary>
public class Field : MonoBehaviour
{
    [Header("Display Reference")]
    public Display display;

    [Header("Cell Count / Spawn")]
    public int circleCount = 10;
    public Vector2 spawnRange = new Vector2(10, 10);

    [Header("Per-Cell Radius Settings")]
    public float minCircleRadius = 0.5f;
    public float maxCircleRadius = 2f;

    [Header("Camera Settings (Voronoi offset)")]
    public float cameraMoveSpeed = 10f;

    [Header("Zoom Settings")]
    public float zoomSpeed = 5f;

    [Header("Voronoi Area")]
    public float voronoiX = -10f;
    public float voronoiY = -10f;
    public float voronoiWidth = 20f;
    public float voronoiHeight = 20f;
    public Color backgroundColor = Color.black;
    public float innerDarkFactor = 0.5f;

    [Header("Pressure / Repulsion")]
    public float repulsionStrength = 5f;

    private Color borderColor = Color.white;

    // dragging state
    private bool isDragging = false;
    private Vector2Int dragStartPixel;
    private Vector2 startVoronoiPosAtDrag;

    // cell storage
    private Dictionary<int, Cell> cells = new();
    private HashSet<int> toBeRemoved = new();
    private List<Cell> toBeAdded = new();
    private Dictionary<int, QuadTreeValue> values = new();

    private Cell selectedCell = null;

    QuadTreeNode quadtree;
    private bool useQuadTree = false;

    [Header("Rendering Mode")]
    public bool gpuRendering = false;

    [Header("Edge / Colour ComputeShader")]
    public ComputeShader edgeComputeShader;
    public int threadGroupSize = 8;

    [Header("GPU Resources")]
    public RenderTexture rt;      // shown by Display
    private RenderTexture idRT;   // per-pixel winner IDs
    private ComputeBuffer cellColorsBuf;  // ID→colour LUT

    private void Start()
    {
        if (display == null)
        {
            Debug.LogError("Field: assign a Display in the Inspector.");
            enabled = false;
            return;
        }

        int w = display.GetWidth(), h = display.GetHeight();

        // Colour RT
        rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        rt.Create();
        display.rt = rt;

        // ID RT
        idRT = new RenderTexture(w, h, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        idRT.Create();

        // initial colour LUT buffer
        cellColorsBuf = new ComputeBuffer(circleCount + 1, sizeof(float) * 4);

        // quadtree?
        if (useQuadTree)
            quadtree = new QuadTreeNode(new Vector2(-300, 300), new Vector2(600, 600));

        // spawn some
        for (int i = 0; i < 500; i++)
            AddIdleCell();
    }

    private void OnDestroy()
    {
        cellColorsBuf?.Release();
        idRT?.Release();
    }

    private void Update()
    {
        HandleVoronoiOffset();
        HandleCellSelection();
        HandleCellSplit();

        if (!gpuRendering)
        {
            // CPU fallback
            display.Clear();
            DrawVoronoiToDisplay();
        }
        else
        {
            // NEW GPU path
            RenderGpu();
        }

        display.Render();

        if (useQuadTree)
            quadtree.Draw(display, new Vector2(voronoiX, voronoiY + voronoiHeight), new Vector2(voronoiWidth, voronoiHeight));

        // optional click debug
        if (Input.GetMouseButtonDown(0))
        {
            var pix = display.TranslateMouseToTextureCoordinates();
            if (pix.HasValue)
            {
                Vector2 world = PixelToWorld(pix.Value.x, pix.Value.y);
            }
        }
    }

    /// <summary>
    /// The new GPU orchestrator: clears RTs, asks each Cell to write into idRT,
    /// then runs the edge-detect + colour compute into rt, and pulls to Texture2D.
    /// </summary>
    private void RenderGpu()
    {
        int pw = display.GetWidth(), ph = display.GetHeight();

        // clear colour RT
        display.ClearRT();

        // clear ID RT
        var old = RenderTexture.active;
        RenderTexture.active = idRT;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = old;

        // build compact mapping: cellID → [0..N-1]
        var cellIDs = new List<int>(cells.Keys);
        var idToIndex = new Dictionary<int, int>(cellIDs.Count);
        for (int i = 0; i < cellIDs.Count; i++)
            idToIndex[cellIDs[i]] = i;

        // prepare colour LUT (slot 0 = background)
        int N = cellIDs.Count;
        Vector4[] cols = new Vector4[N + 1];
        cols[0] = backgroundColor;
        for (int i = 0; i < N; i++)
            cols[i + 1] = cells[cellIDs[i]].color;

        if (cellColorsBuf == null || cellColorsBuf.count != cols.Length)
        {
            cellColorsBuf?.Release();
            cellColorsBuf = new ComputeBuffer(cols.Length, sizeof(float) * 4);
        }
        cellColorsBuf.SetData(cols);

        // per-cell ID pass
        foreach (var c in cells.Values)
        {
            int mapped = idToIndex[c.cellID] + 1;
            c.Render(idRT, mapped,
                     voronoiX, voronoiY, voronoiWidth, voronoiHeight,
                     pw, ph);
        }

        // edge-detect + colour
        int k = edgeComputeShader.FindKernel("CSMain");
        edgeComputeShader.SetInts("texSize", pw, ph);
        edgeComputeShader.SetVector("borderColor", borderColor);
        edgeComputeShader.SetVector("bgColor", backgroundColor);
        edgeComputeShader.SetBuffer(k, "CellColors", cellColorsBuf);
        edgeComputeShader.SetTexture(k, "IDTex", idRT);
        edgeComputeShader.SetTexture(k, "OutputTex", rt);
        int gx = Mathf.CeilToInt(pw / 8f);
        int gy = Mathf.CeilToInt(ph / 8f);
        edgeComputeShader.Dispatch(k, gx, gy, 1);

        // copy to Texture2D
        display.PullRTToTexture();
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        foreach (var kv in cells)
        {
            var c = kv.Value;
            if (useQuadTree)
            {
                var val = values[c.cellID];
                val.bbox.tl = new Vector2(c.transform.position.x - c.outerRadius, c.transform.position.y + c.outerRadius);
                val.bbox.size = new Vector2(c.outerRadius * 2, c.outerRadius * 2);
                val.UpdateTree();
            }
            c.HandleGrowth();
            c.behavior?.PerformBehavior(dt, c, this);
        }

        ActuallyRemoveCells();
        ActuallyAddCells();
        ApplyRepulsionForces();
    }

    private Cell AddBoidsCell()
    {
        Vector2 pos = new Vector2(
            Random.Range(-spawnRange.x, spawnRange.x),
            Random.Range(-spawnRange.y, spawnRange.y));
        float r = Random.Range(minCircleRadius / 6f, maxCircleRadius / 6f);
        var behavior = new BoidsBehavior();
        var cell = Cell.NewBuilder()
                       .SetField(this)
                       .SetPosition(pos)
                       .SetInnerRadius(r)
                       .SetOuterRadius(r * 2f)
                       .SetColor(Random.ColorHSV())
                       .SetMaximumSize(1f)
                       .SetGrowthRate(.1f)
                       .SetBehavior(behavior)
                       .Build();
        behavior.RegisterCell(cell);
        return cell;
    }

    private Cell AddIdleCell()
    {
        Vector2 pos = new Vector2(
            Random.Range(-spawnRange.x, spawnRange.x),
            Random.Range(-spawnRange.y, spawnRange.y));
        float r = Random.Range(minCircleRadius, maxCircleRadius);
        return Cell.NewBuilder()
                   .SetField(this)
                   .SetPosition(pos)
                   .SetInnerRadius(r)
                   .SetOuterRadius(r * 2f)
                   .SetColor(Random.ColorHSV())
                   .SetBehavior(new IdleBehavior())
                   .Build();
    }

    public void AddCell(Cell c) => toBeAdded.Add(c);
    private void ActuallyAddCells()
    {
        foreach (var c in toBeAdded)
        {
            cells[c.cellID] = c;
            if (useQuadTree)
            {
                values[c.cellID] = new QuadTreeValue(
                    new Vector2(c.transform.position.x - c.outerRadius, c.transform.position.y + c.outerRadius),
                    new Vector2(c.outerRadius * 2, c.outerRadius * 2));
                quadtree.Add(values[c.cellID]);
            }
        }
        toBeAdded.Clear();
    }

    public void RemoveCell(Cell c) => toBeRemoved.Add(c.cellID);
    private void ActuallyRemoveCells()
    {
        foreach (int id in toBeRemoved)
        {
            if (!cells.ContainsKey(id)) continue;
            var c = cells[id];
            if (c == selectedCell) selectedCell = null;
            cells.Remove(id);
            if (useQuadTree)
            {
                var val = values[id];
                val.quadTreeNode.Remove(val);
                values.Remove(id);
            }
            Destroy(c.gameObject);
        }
        toBeRemoved.Clear();
    }

    private void HandleCellSelection()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        var pix = display.TranslateMouseToTextureCoordinates();
        if (!pix.HasValue) return;
        Vector2 world = PixelToWorld(pix.Value.x, pix.Value.y);

        float best = float.MaxValue;
        Cell chosen = null;
        foreach (var kv in cells)
        {
            var c = kv.Value;
            float d = Vector2.Distance(world, c.transform.position);
            if (d < c.innerRadius && d < best)
            {
                best = d;
                chosen = c;
            }
        }
        if (chosen != null && !(chosen.behavior is BoidsBehavior))
        {
            if (selectedCell != null && selectedCell != chosen)
                selectedCell.behavior = new IdleBehavior();
            chosen.behavior = new ControlledBehavior();
            selectedCell = chosen;
        }
    }

    private void HandleCellSplit()
    {
        if (!Input.GetMouseButtonDown(1)) return;
        var pix = display.TranslateMouseToTextureCoordinates();
        if (!pix.HasValue) return;
        Vector2 world = PixelToWorld(pix.Value.x, pix.Value.y);

        float best = float.MaxValue;
        Cell chosen = null;
        foreach (var kv in cells)
        {
            var c = kv.Value;
            float d = Vector2.Distance(world, c.transform.position);
            if (d < c.innerRadius && d < best)
            {
                best = d;
                chosen = c;
            }
        }
        chosen?.Split();
    }

    private void ApplyRepulsionForces()
    {
        var filter = new ContactFilter2D { useTriggers = true };
        var overlaps = new List<Collider2D>();
        foreach (var kv in cells)
        {
            var cA = kv.Value;
            if (!cA.outerCollider) continue;
            overlaps.Clear();
            int cnt = cA.outerCollider.OverlapCollider(filter, overlaps);
            for (int i = 0; i < cnt; i++)
            {
                var other = overlaps[i]?.GetComponentInParent<Cell>();
                if (other == null || other == cA) continue;
                if (cA.GetInstanceID() >= other.GetInstanceID()) continue;

                Vector2 delta = cA.transform.position - other.transform.position;
                float dist = delta.magnitude;
                float combined = cA.outerRadius + other.outerRadius;
                if (dist < combined && dist > 0.001f)
                {
                    Vector2 dir = delta.normalized;
                    float overlap = 1f - (dist / combined);
                    float strength = overlap * repulsionStrength;
                    cA.rb.AddForce(dir * strength);
                    other.rb.AddForce(-dir * strength);
                }
            }
        }
    }

    private void HandleVoronoiOffset()
    {
        if (!isDragging)
        {
            if (Input.GetKey(KeyCode.W)) voronoiY += cameraMoveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.S)) voronoiY -= cameraMoveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.A)) voronoiX -= cameraMoveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.D)) voronoiX += cameraMoveSpeed * Time.deltaTime;
        }
        if (Input.GetMouseButtonDown(0))
        {
            var pix = display.TranslateMouseToTextureCoordinates();
            if (pix.HasValue)
            {
                isDragging = true;
                dragStartPixel = pix.Value;
                startVoronoiPosAtDrag = new Vector2(voronoiX, voronoiY);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
        else if (isDragging && Input.GetMouseButton(0))
        {
            var pix = display.TranslateMouseToTextureCoordinates();
            if (pix.HasValue)
            {
                int dx = pix.Value.x - dragStartPixel.x;
                int dy = pix.Value.y - dragStartPixel.y;
                float perX = voronoiWidth / display.GetWidth();
                float perY = voronoiHeight / display.GetHeight();
                voronoiX = startVoronoiPosAtDrag.x - dx * perX;
                voronoiY = startVoronoiPosAtDrag.y - dy * perY;
            }
        }
        if (!isDragging)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 1e-4f)
            {
                float cx = voronoiX + voronoiWidth * .5f;
                float cy = voronoiY + voronoiHeight * .5f;
                float sf = 1f - scroll * zoomSpeed;
                sf = Mathf.Max(sf, .01f);
                float nw = voronoiWidth * sf;
                float nh = voronoiHeight * sf;
                voronoiX = cx - nw * .5f;
                voronoiY = cy - nh * .5f;
                voronoiWidth  = nw;
                voronoiHeight = nh;
            }
        }
    }

    private void DrawVoronoiToDisplay()
    {
        int w = display.GetWidth(), h = display.GetHeight();
        int[] winners = new int[w * h];
        Color[] cols = new Color[w * h];
        float left = voronoiX, right = voronoiX + voronoiWidth;
        float bottom = voronoiY, top = voronoiY + voronoiHeight;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int idx = y * w + x;
                float nx = x / (float)(w - 1);
                float ny = y / (float)(h - 1);
                Vector2 pw = new Vector2(
                    Mathf.Lerp(left, right, nx),
                    Mathf.Lerp(bottom, top, ny)
                );
                var hits = Physics2D.RaycastAll(pw + Vector2.up * .001f, Vector2.down, .002f);
                float bestFrac = float.MaxValue, bestDist = float.MaxValue;
                Cell best = null;
                foreach (var r in hits)
                {
                    var c = r.collider.GetComponentInParent<Cell>();
                    if (c == null) continue;
                    float d = Vector2.Distance(pw, c.transform.position);
                    if (d < c.outerRadius)
                    {
                        float frac = d / c.outerRadius;
                        if (frac < bestFrac)
                        {
                            bestFrac = frac;
                            bestDist = d;
                            best = c;
                        }
                    }
                }
                if (best == null)
                {
                    winners[idx] = -1;
                    cols[idx]    = backgroundColor;
                }
                else
                {
                    winners[idx] = best.cellID;
                    if (bestDist < best.innerRadius)
                    {
                        var o = best.color;
                        cols[idx] = new Color(o.r * innerDarkFactor, o.g * innerDarkFactor, o.b * innerDarkFactor, 1f);
                    }
                    else cols[idx] = best.color;
                }
            }
        }

        int[] neighOff = { -1,0,1,0,0,-1,0,1,-1,-1,-1,1,1,-1,1,1 };
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int idx = y * w + x;
                int me  = winners[idx];
                if (me == -1) continue;
                bool border = false;
                for (int i = 0; i < neighOff.Length; i += 2)
                {
                    int nx = x + neighOff[i+1], ny = y + neighOff[i];
                    if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                    if (winners[ny * w + nx] != me) { border = true; break; }
                }
                if (border) cols[idx] = borderColor;
            }
        }

        display.Clear();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                display.SetPixel(x, y, cols[y * w + x]);
        display.Render();
    }

    public Dictionary<int, Cell> GetAllCells() => cells;

    public Vector2 PixelToWorld(int px, int py)
    {
        float w = display.GetWidth(), h = display.GetHeight();
        float nx = px / (w - 1f), ny = py / (h - 1f);
        return new Vector2(
            Mathf.Lerp(voronoiX, voronoiX + voronoiWidth, nx),
            Mathf.Lerp(voronoiY, voronoiY + voronoiHeight, ny)
        );
    }
}
