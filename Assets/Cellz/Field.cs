using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages an area of "cells":
///  - Per-cell inner & outer radius
///  - Collision damping
///  - Click-to-select (left-click)
///  - Right-click to split (just calls cell.Split())
///  - WASD/zoom to pan the Voronoi region
///  - Left-click drag to pan
///  - Per-pixel Voronoi coloring & border detection
///  - Overlap-based repulsive forces
///  - Now has AddCell(...) and RemoveCell(...)
/// </summary>
public class Field : MonoBehaviour
{
    [Header("Display Reference")]
    public Display display; // Assign your Display component here in the Inspector

    [Header("Cell Count / Spawn")]
    public int circleCount = 10;
    public Vector2 spawnRange = new Vector2(10, 10);

    [Header("Per-Cell Radius Settings")]
    public float minCircleRadius = 0.5f;
    public float maxCircleRadius = 2f;

    [Header("Camera Settings (Now used for Voronoi offset)")]
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

    // For dragging the Voronoi "camera" via left-click
    private bool isDragging = false;
    private Vector2Int dragStartPixel;
    private Vector2 startVoronoiPosAtDrag;

    // The global list of all spawned cells
    private List<Cell> cells = new List<Cell>();

    private Cell selectedCell = null;

    private void Start()
    {
        if (display == null)
        {
            Debug.LogError("No Display assigned to Field. Please link one in the Inspector.");
            return;
        }

        AddNewCell(1, new IdleBehavior());
        AddNewCell(2, new IdleBehavior());
        //AddNewCell(2, new BoidsBehavior());
    }

    private void Update()
    {
        HandleVoronoiOffset();
        HandleCellSelection(); // left-click
        HandleCellSplit();     // right-click

        DrawVoronoiToDisplay();
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        // Let each cell's behavior run
        for (int i = cells.Count - 1;i>=0;i--)
        {
            Cell c = cells[i];
            c.HandleGrowth();
            c.behavior?.PerformBehavior(dt, c, this);
        }

        // Then apply repulsive forces
        ApplyRepulsionForces();
    }

    /// <summary>
    /// Creates a brand-new Cell at a random position, random radius,
    /// sets up colliders, default behavior, etc.
    /// </summary>
    private Cell AddNewCell(int cellIndex, ICellBehavior behavior)
    {
        Vector2 position = new Vector2(
            Random.Range(-spawnRange.x, spawnRange.x),
            Random.Range(-spawnRange.y, spawnRange.y)
        );

        float randomRadius = Random.Range(minCircleRadius, maxCircleRadius);

        return Cell.NewBuilder()
            .SetField(this)
            .SetGameObjectName("Cell_" + cellIndex)
            .SetCellID(cellIndex)
            .SetPosition(position)
            .SetColor(Random.ColorHSV())
            .SetInnerRadius(randomRadius)
            .SetOuterRadius(randomRadius * 2f)
            .SetBehavior(behavior)
            .Build();
    }

    /// <summary>
    /// Add the given cell to our global 'cells' list (and anything else needed).
    /// </summary>
    public void AddCell(Cell c)
    {
        // You can do checks or events here if needed
        cells.Add(c);
    }

    /// <summary>
    /// Remove the given cell from our global list. (Cell calls this from within Split()).
    /// </summary>
    public void RemoveCell(Cell c)
    {
        // You can do checks or events here if needed
        cells.Remove(c);
        if( c == selectedCell)
        {
            selectedCell = null; // Deselect if we were selected
        }
    }

    /// <summary>
    /// Left-click to select/focus a cell. (Optional old logic if you have a "ControlledBehavior".)
    /// </summary>
    private void HandleCellSelection()
    {
        if (Input.GetMouseButtonDown(0)) // left
        {
            Vector2Int? pixelCoords = display.TranslateMouseToTextureCoordinates();
            if (!pixelCoords.HasValue) return;
            Vector2 clickWorldPos = PixelToWorld(pixelCoords.Value.x, pixelCoords.Value.y);

            // e.g. find which cell was clicked
            float closestDistance = float.MaxValue;
            Cell clickedCell = null;
            foreach (Cell c in cells)
            {
                float dist = Vector2.Distance(clickWorldPos, c.transform.position);
                if (dist < c.innerRadius && dist < closestDistance)
                {
                    closestDistance = dist;
                    clickedCell = c;
                }
            }

            // If you have logic to set behavior = new ControlledBehavior(), do it here
            if (clickedCell != null && !(clickedCell.behavior is BoidsBehavior))
            {
                if(selectedCell != null && selectedCell != clickedCell)
                {
                    // Deselect previous cell
                    selectedCell.behavior = new IdleBehavior();
                }
                clickedCell.behavior = new ControlledBehavior();
                selectedCell = clickedCell;
            }
        }
    }

    /// <summary>
    /// Right-click to split a cell. We just find the clicked cell and call cell.Split().
    /// </summary>
    private void HandleCellSplit()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Vector2Int? pixelCoords = display.TranslateMouseToTextureCoordinates();
            if (!pixelCoords.HasValue) return;
            Vector2 clickWorldPos = PixelToWorld(pixelCoords.Value.x, pixelCoords.Value.y);

            float closestDistance = float.MaxValue;
            Cell clickedCell = null;
            foreach (Cell c in cells)
            {
                float dist = Vector2.Distance(clickWorldPos, c.transform.position);
                if (dist < c.innerRadius && dist < closestDistance)
                {
                    closestDistance = dist;
                    clickedCell = c;
                }
            }

            if (clickedCell != null)
            {
                clickedCell.Split(); // The actual logic is in Cell.Split()
            }
        }
    }

    /// <summary>
    /// Applies a gentle repulsive force between overlapping cells.
    /// Called from FixedUpdate.
    /// </summary>
    private void ApplyRepulsionForces()
    {
        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = false,
            useDepth = false
        };

        var overlaps = new List<Collider2D>(16);

        foreach (Cell cA in cells)
        {
            if (!cA.outerCollider) continue;

            overlaps.Clear();
            int count = cA.outerCollider.OverlapCollider(filter, overlaps);

            for (int k = 0; k < count; k++)
            {
                Collider2D otherColl = overlaps[k];
                if (!otherColl) continue;

                Cell cB = otherColl.GetComponentInParent<Cell>();
                if (cB == null || cB == cA) continue;

                // Avoid doubling forces if we see the pair from both sides
                if (cA.transform.GetInstanceID() >= cB.transform.GetInstanceID())
                    continue;

                Vector2 posA = cA.transform.position;
                Vector2 posB = cB.transform.position;
                Vector2 delta = posA - posB;
                float dist = delta.magnitude;

                float combinedOuter = cA.outerRadius + cB.outerRadius;
                if (dist < combinedOuter && dist > 0.001f)
                {
                    Vector2 pushDir = delta.normalized;
                    float overlapFactor = 1f - (dist / combinedOuter);
                    float strength = overlapFactor * repulsionStrength;

                    Vector2 force = pushDir * strength;
                    cA.rb.AddForce(force);
                    cB.rb.AddForce(-force);
                }
            }
        }
    }

    /// <summary>
    /// Reads WASD + left mouse dragging + mouse wheel zoom for the Voronoi "camera."
    /// </summary>
    private void HandleVoronoiOffset()
    {
        // 1) WASD panning
        if (!isDragging)
        {
            if (Input.GetKey(KeyCode.W)) voronoiY += cameraMoveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.S)) voronoiY -= cameraMoveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.A)) voronoiX -= cameraMoveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.D)) voronoiX += cameraMoveSpeed * Time.deltaTime;
        }

        // 2) Left mouse drag
        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int? pix = display.TranslateMouseToTextureCoordinates();
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
            Vector2Int? pix = display.TranslateMouseToTextureCoordinates();
            if (pix.HasValue)
            {
                int dx = pix.Value.x - dragStartPixel.x;
                int dy = pix.Value.y - dragStartPixel.y;

                float worldPerPixelX = voronoiWidth / display.GetWidth();
                float worldPerPixelY = voronoiHeight / display.GetHeight();

                voronoiX = startVoronoiPosAtDrag.x - dx * worldPerPixelX;
                voronoiY = startVoronoiPosAtDrag.y - dy * worldPerPixelY;
            }
        }

        // 3) Mouse Wheel Zoom
        if (!isDragging)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                float centerX = voronoiX + voronoiWidth * 0.5f;
                float centerY = voronoiY + voronoiHeight * 0.5f;

                float scaleFactor = 1f - scroll * zoomSpeed;
                scaleFactor = Mathf.Max(scaleFactor, 0.01f);

                float newWidth = voronoiWidth * scaleFactor;
                float newHeight = voronoiHeight * scaleFactor;

                voronoiX = centerX - newWidth * 0.5f;
                voronoiY = centerY - newHeight * 0.5f;
                voronoiWidth = newWidth;
                voronoiHeight = newHeight;
            }
        }
    }

    /// <summary>
    /// Runs the Voronoi pixel-by-pixel algorithm, storing the winner ID in an array,
    /// applying "inner radius" darkening, and marking 1-pixel borders in white.
    /// </summary>
    private void DrawVoronoiToDisplay()
    {
        int texWidth  = display.GetWidth();
        int texHeight = display.GetHeight();

        int[] winners  = new int[texWidth * texHeight];
        Color[] colors = new Color[texWidth * texHeight];

        float left = voronoiX;
        float right = voronoiX + voronoiWidth;
        float bottom = voronoiY;
        float top = voronoiY + voronoiHeight;

        int layerMask = Physics2D.DefaultRaycastLayers;

        // First pass: figure out which cell "owns" each pixel
        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                int index = y * texWidth + x;

                float nx = x / (float)(texWidth - 1);
                float ny = y / (float)(texHeight - 1);

                float worldX = Mathf.Lerp(left, right, nx);
                float worldY = Mathf.Lerp(bottom, top, ny);
                Vector2 pixelWorldPos = new Vector2(worldX, worldY);

                Vector2 rayStart = pixelWorldPos + Vector2.up * 0.001f;
                Vector2 rayDir = Vector2.down;
                float rayDist = 0.002f;

                RaycastHit2D[] hits = Physics2D.RaycastAll(rayStart, rayDir, rayDist, layerMask);

                float minFracDist = float.MaxValue;
                float winningDist = float.MaxValue;
                Cell winningCell = null;

                for (int h = 0; h < hits.Length; h++)
                {
                    Cell cData = hits[h].collider.GetComponentInParent<Cell>();
                    if (!cData) continue;

                    float distToCenter = Vector2.Distance(pixelWorldPos, cData.transform.position);
                    if (distToCenter < cData.outerRadius)
                    {
                        float fracDist = distToCenter / cData.outerRadius;
                        if (fracDist < minFracDist)
                        {
                            minFracDist = fracDist;
                            winningDist = distToCenter;
                            winningCell = cData;
                        }
                    }
                }

                if (winningCell == null)
                {
                    winners[index] = -1;
                    colors[index] = backgroundColor;
                }
                else
                {
                    winners[index] = winningCell.cellID;
                    if (winningDist < winningCell.innerRadius)
                    {
                        // Darker color
                        Color orig = winningCell.color;
                        colors[index] = new Color(
                            orig.r * innerDarkFactor,
                            orig.g * innerDarkFactor,
                            orig.b * innerDarkFactor,
                            1f
                        );
                    }
                    else
                    {
                        colors[index] = winningCell.color;
                    }
                }
            }
        }

        // Second pass: mark borders
        int[] neighborOffsets =
        {
            -1,  0,  // up
             1,  0,  // down
             0, -1,  // left
             0,  1,  // right
            -1, -1,
            -1,  1,
             1, -1,
             1,  1
        };

        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                int index = y * texWidth + x;
                int myWinner = winners[index];
                if (myWinner == -1) continue; // no cell => ignore

                bool isBorder = false;
                for (int n = 0; n < neighborOffsets.Length; n += 2)
                {
                    int dy = neighborOffsets[n];
                    int dx = neighborOffsets[n + 1];
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx < 0 || nx >= texWidth || ny < 0 || ny >= texHeight)
                        continue;

                    int neighborIdx = ny * texWidth + nx;
                    int neighborWinner = winners[neighborIdx];
                    if (neighborWinner != myWinner)
                    {
                        isBorder = true;
                        break;
                    }
                }

                if (isBorder)
                {
                    colors[index] = borderColor;
                }
            }
        }

        // Render final colors
        display.Clear();
        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                display.SetPixel(x, y, colors[y * texWidth + x]);
            }
        }
        display.Render();
    }

    public Vector2 PixelToWorld(int px, int py)
    {
        float texW = display.GetWidth();
        float texH = display.GetHeight();
        float nx = px / (texW - 1f);
        float ny = py / (texH - 1f);
        float worldX = Mathf.Lerp(voronoiX, voronoiX + voronoiWidth, nx);
        float worldY = Mathf.Lerp(voronoiY, voronoiY + voronoiHeight, ny);
        return new Vector2(worldX, worldY);
    }
}
