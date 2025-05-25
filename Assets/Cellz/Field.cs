// Field.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages an area of cells and bullets, rendering with a GPU Voronoi pass.
/// </summary>
public class Field : MonoBehaviour
{
    /* ─────────── Inspector ‑ configurable ─────────── */

    [Header("Display Reference")]
    public Display display;

    [Header("Cell Count / Spawn")]
    public int circleCount = 10;
    public Vector2 spawnRange = new Vector2(10, 10);

    [Header("Per‑Cell Radius Settings")]
    public float minCircleRadius = 0.5f;
    public float maxCircleRadius = 2f;

    [Header("Camera Settings (Voronoi offset)")]
    public float cameraMoveSpeed = 10f;

    [Header("Zoom Settings")]
    public float zoomSpeed = 5f;

    [Header("Voronoi Area")]
    public float voronoiX = -10f;
    public float voronoiY = -10f;
    public float voronoiWidth  = 20f;
    public float voronoiHeight = 20f;
    public Color backgroundColor = Color.black;
    public float innerDarkFactor = 0.5f;

    [Header("Pressure / Repulsion")]
    public float repulsionStrength = 5f;

    private readonly Color borderColor = Color.white;

    [Header("Edge / Colour ComputeShader")]
    public ComputeShader edgeComputeShader;

    /* ─────────── Private state ─────────── */

    // dragging
    private bool      isDragging;
    private Vector2Int dragStartPixel;
    private Vector2   startVoronoiPosAtDrag;

    // storage
    private readonly Dictionary<int, Cell> cells       = new();
    private readonly HashSet<int>          toBeRemoved = new();
    private readonly List<Cell>            toBeAdded   = new();

    private readonly List<Bullet> activeBullets        = new();
    private readonly List<Bullet> bulletsToBeRemoved   = new();

    private Cell selectedCell = null;

    // Quad‑tree (optional, kept for future)
    private QuadTreeNode                       quadtree;
    private readonly Dictionary<int, QuadTreeValue> values = new();
    private bool useQuadTree = false;

    // GPU objects
    public RenderTexture rt;          // colour buffer shown by Display
    private RenderTexture idRT;       // integer IDs per pixel
    private ComputeBuffer cellColorsBuf; // ID‑>colour LUT

    /* ─────────── Unity: Start / Destroy ─────────── */

    private void Start()
    {
        if (display == null)
        {
            Debug.LogError("Field: assign a Display in the Inspector.");
            enabled = false;
            return;
        }

        int w = display.GetWidth();
        int h = display.GetHeight();

        // Colour RT
        rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
        {
            enableRandomWrite = true,
            filterMode        = FilterMode.Point,
            wrapMode          = TextureWrapMode.Clamp
        };
        rt.Create();
        display.rt = rt;

        // ID RT
        idRT = new RenderTexture(w, h, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode        = FilterMode.Point,
            wrapMode          = TextureWrapMode.Clamp
        };
        idRT.Create();

        cellColorsBuf = new ComputeBuffer(circleCount + 1, sizeof(float) * 4);

        if (useQuadTree)
            quadtree = new QuadTreeNode(new Vector2(-300, 300), new Vector2(600, 600));

        // spawn some test cells
        for (int i = 0; i < circleCount; i++)
            AddRangedCell();
    }

    private void OnDestroy()
    {
        cellColorsBuf?.Release();
        idRT?.Release();
    }

    /* ─────────── Unity: Update / FixedUpdate ─────────── */

    private void Update()
    {
        HandleVoronoiOffset();
        HandleCellSelection();
        HandleCellSplit();

        /* GPU path only – CPU fallback removed */
        RenderGpu();
        display.Render();

        if (useQuadTree)
            quadtree.Draw(display,
                          new Vector2(voronoiX, voronoiY + voronoiHeight),
                          new Vector2(voronoiWidth, voronoiHeight));
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        foreach (var kv in cells)
        {
            var c = kv.Value;

            if (useQuadTree)
            {
                var val   = values[c.cellID];
                val.bbox.tl   = new Vector2(c.transform.position.x - c.outerRadius,
                                            c.transform.position.y + c.outerRadius);
                val.bbox.size = new Vector2(c.outerRadius * 2, c.outerRadius * 2);
                val.UpdateTree();
            }

            c.HandleGrowth();
            c.behavior?.PerformBehavior(dt, c, this);
        }

        ActuallyRemoveCells();
        ActuallyAddCells();
        ApplyRepulsionForces();

        // purge bullets that have expired
        foreach (var b in bulletsToBeRemoved)
        {
            activeBullets.Remove(b);
            Destroy(b.gameObject);
        }
        bulletsToBeRemoved.Clear();
    }

    /* ──────────────────────────────────────────────── */
    /*                  GPU  RENDER                    */
    /* ──────────────────────────────────────────────── */

    /// <summary>
    /// Clears the RTs, asks every <see cref="IRenderable"/> (cells first, then bullets)
    /// to stamp its ID into <c>idRT</c>, then runs the edge‑detect + colour compute
    /// shader into <c>rt</c>, finally pulling to the Display texture.
    /// </summary>
    private void RenderGpu()
    {
        int pw = display.GetWidth();
        int ph = display.GetHeight();

        /* clear colour RT */
        display.ClearRT();

        /* clear ID RT */
        var old = RenderTexture.active;
        RenderTexture.active = idRT;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = old;

        /* ── Build colour LUT & mapping ── */
        var colours = new List<Vector4> { backgroundColor }; // slot 0 = background

        var cellIdToMap = new Dictionary<int, int>();
        foreach (var c in cells.Values)
        {
            cellIdToMap[c.cellID] = colours.Count;
            colours.Add(c.color);
        }

        var bulletMapIndices = new List<int>();
        foreach (var b in activeBullets)
        {
            if (b == null) continue;
            bulletMapIndices.Add(colours.Count);
            colours.Add(b.color);
        }

        if (cellColorsBuf == null || cellColorsBuf.count != colours.Count)
        {
            cellColorsBuf?.Release();
            cellColorsBuf = new ComputeBuffer(colours.Count, sizeof(float) * 4);
        }
        cellColorsBuf.SetData(colours.ToArray());

        /* ── Per‑object ID pass ── */

        // cells first (so bullets overwrite on top)
        foreach (var c in cells.Values)
        {
            int map = cellIdToMap[c.cellID];          // already +1 offset via colours list
            c.Render(idRT, map,
                     voronoiX, voronoiY, voronoiWidth, voronoiHeight,
                     pw, ph);
        }

        // bullets
        for (int i = 0; i < activeBullets.Count; i++)
        {
            var b = activeBullets[i];
            if (b == null) continue;
            int map = bulletMapIndices[i];
            b.Render(idRT, map,
                     voronoiX, voronoiY, voronoiWidth, voronoiHeight,
                     pw, ph);
        }

        /* ── Edge detect + colour ── */
        int k = edgeComputeShader.FindKernel("CSMain");
        edgeComputeShader.SetInts  ("texSize", pw, ph);
        edgeComputeShader.SetVector("borderColor", borderColor);
        edgeComputeShader.SetVector("bgColor",     backgroundColor);
        edgeComputeShader.SetBuffer (k, "CellColors", cellColorsBuf);
        edgeComputeShader.SetTexture(k, "IDTex",      idRT);
        edgeComputeShader.SetTexture(k, "OutputTex",  rt);

        int gx = Mathf.CeilToInt(pw / 8f);
        int gy = Mathf.CeilToInt(ph / 8f);
        edgeComputeShader.Dispatch(k, gx, gy, 1);

        display.PullRTToTexture();
    }

    /* ──────────────────────────────────────────────── */
    /*             SPAWNING  /  MANAGEMENT             */
    /* ──────────────────────────────────────────────── */

    // Ranged‑behaviour test spawn
    private Cell AddRangedCell()
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
                   .SetBehavior(new RangedBehavior())
                   .Build();
    }

    /*  BULLETS  */
    public void AddBullet(Vector2 position, Vector2 velocity, float radius, Color color)
    {
        var go = new GameObject("Bullet");
        var b  = go.AddComponent<Bullet>();
        b.Initialize(position, velocity, radius, color);
        activeBullets.Add(b);
    }

    public void RemoveBullet(Bullet b) => bulletsToBeRemoved.Add(b);

    /*  Cells */
    public void AddCell(Cell c)  => toBeAdded.Add(c);
    public void RemoveCell(Cell c) => toBeRemoved.Add(c.cellID);

    private void ActuallyAddCells()
    {
        foreach (var c in toBeAdded)
        {
            cells[c.cellID] = c;

            if (useQuadTree)
            {
                values[c.cellID] = new QuadTreeValue(
                    new Vector2(c.transform.position.x - c.outerRadius,
                                c.transform.position.y + c.outerRadius),
                    new Vector2(c.outerRadius * 2, c.outerRadius * 2));
                quadtree.Add(values[c.cellID]);
            }
        }
        toBeAdded.Clear();
    }

    private void ActuallyRemoveCells()
    {
        foreach (var id in toBeRemoved)
        {
            if (!cells.TryGetValue(id, out var c)) continue;

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

    /* ──────────────────────────────────────────────── */
    /*                 INPUT  HELPERS                  */
    /* ──────────────────────────────────────────────── */

    private void HandleCellSelection()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        var pix = display.TranslateMouseToTextureCoordinates();
        if (!pix.HasValue) return;
        Vector2 world = PixelToWorld(pix.Value.x, pix.Value.y);

        float best = float.MaxValue;
        Cell  chosen = null;

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
            selectedCell    = chosen;
        }
    }

    private void HandleCellSplit()
    {
        if (!Input.GetMouseButtonDown(1)) return;
        var pix = display.TranslateMouseToTextureCoordinates();
        if (!pix.HasValue) return;
        Vector2 world = PixelToWorld(pix.Value.x, pix.Value.y);

        float best = float.MaxValue;
        Cell  chosen = null;

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

    /*  Simple separation force between touching outer colliders  */
    private void ApplyRepulsionForces()
    {
        var filter   = new ContactFilter2D { useTriggers = true };
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

                Vector2 delta    = cA.transform.position - other.transform.position;
                float   dist     = delta.magnitude;
                float   combined = cA.outerRadius + other.outerRadius;

                if (dist < combined && dist > 0.001f)
                {
                    Vector2 dir      = delta.normalized;
                    float   overlap  = 1f - (dist / combined);
                    float   strength = overlap * repulsionStrength;

                    cA.rb.AddForce( dir * strength);
                    other.rb.AddForce(-dir * strength);
                }
            }
        }
    }

    /* ──────────────────────────────────────────────── */
    /*             CAMERA / WINDOW CONTROLS            */
    /* ──────────────────────────────────────────────── */

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
                isDragging            = true;
                dragStartPixel        = pix.Value;
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
                int   dx   = pix.Value.x - dragStartPixel.x;
                int   dy   = pix.Value.y - dragStartPixel.y;
                float perX = voronoiWidth  / display.GetWidth();
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
                float cx = voronoiX + voronoiWidth  * 0.5f;
                float cy = voronoiY + voronoiHeight * 0.5f;

                float sf = 1f - scroll * zoomSpeed;
                sf = Mathf.Max(sf, 0.01f);

                float nw = voronoiWidth  * sf;
                float nh = voronoiHeight * sf;

                voronoiX      = cx - nw * 0.5f;
                voronoiY      = cy - nh * 0.5f;
                voronoiWidth  = nw;
                voronoiHeight = nh;
            }
        }
    }
    
    public Dictionary<int, Cell> GetAllCells() => cells;

    /* ──────────────────────────────────────────────── */
    /*                  UTILITIES                      */
    /* ──────────────────────────────────────────────── */

    public Vector2 PixelToWorld(int px, int py)
    {
        float w = display.GetWidth();
        float h = display.GetHeight();
        float nx = px / (w - 1f);
        float ny = py / (h - 1f);

        return new Vector2(
            Mathf.Lerp(voronoiX, voronoiX + voronoiWidth, nx),
            Mathf.Lerp(voronoiY, voronoiY + voronoiHeight, ny));
    }

    public Vector2 WorldToPixel(float wx, float wy)
    {
        float w = display.GetWidth();
        float h = display.GetHeight();

        float px = Mathf.InverseLerp(voronoiX,               voronoiX + voronoiWidth,  wx) * (w - 1f);
        float py = Mathf.InverseLerp(voronoiY,               voronoiY + voronoiHeight, wy) * (h - 1f);

        return new Vector2(px, py);
    }
}
