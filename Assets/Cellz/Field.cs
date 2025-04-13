using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages an area of "cells":
///  - Per-cell inner & outer radius
///  - Collision damping
///  - Click-to-select (left-click)
///  - Right-click to split
///  - WASD/zoom to pan the Voronoi region
///  - Left-click drag to pan
///  - Per-pixel Voronoi coloring & border detection
///  - Overlap-based repulsive forces
///  - Default maxSpeed/maxAcceleration assigned to each cell
///  - Selection sets a cell to ControlledBehavior; others idle by default
/// </summary>
public class Field : MonoBehaviour
{
    [Header("Display Reference")]
    public Display display; // Assign your Display component here in the Inspector

    [Header("Cell Count / Spawn")]
    public int circleCount = 10;
    public Vector2 spawnRange = new Vector2(10, 10);

    [Header("Per-Cell Radius Settings")]
    [Tooltip("Each cell gets a random radius in this range, used for BOTH inner and outer radius (scaled in code).")]
    public float minCircleRadius = 0.5f;
    public float maxCircleRadius = 2f;

    [Header("Default Movement Limits")]
    [Tooltip("Default max speed for newly spawned cells.")]
    public float defaultMaxSpeed = 5f;
    [Tooltip("Default max acceleration for newly spawned cells.")]
    public float defaultMaxAcceleration = 10f;

    [Header("Drag / Physics")]
    [Tooltip("Drag factor applied to each cell’s rigidbody.")]
    public float circleDrag = 1f;

    [Header("Camera Settings (Now used for Voronoi offset)")]
    [Tooltip("How quickly WASD modifies the Voronoi offset.")]
    public float cameraMoveSpeed = 10f;

    [Header("Zoom Settings")]
    [Tooltip("How quickly the mouse wheel zoom occurs.")]
    public float zoomSpeed = 5f;

    [Header("Voronoi Area")]
    public float voronoiX = -10f;
    public float voronoiY = -10f;
    public float voronoiWidth = 20f;
    public float voronoiHeight = 20f;

    [Tooltip("Background color if no cell covers that pixel.")]
    public Color backgroundColor = Color.black;

    [Tooltip("Inner radius darkening factor. 0=black, 1=same color.")]
    public float innerDarkFactor = 0.5f;

    [Header("Pressure / Repulsion")]
    [Tooltip("Strength of repulsive force when two cells overlap within outerRadius.")]
    public float repulsionStrength = 5f;

    private Color borderColor = Color.white;

    // For dragging the Voronoi "camera" via left-click
    private bool isDragging = false;
    private Vector2Int dragStartPixel;
    private Vector2 startVoronoiPosAtDrag;

    // We store all spawned Cell components here
    private List<Cell> cells = new List<Cell>();

    // Keep track of whichever cell is currently user-controlled (if any)
    private Cell currentlyControlledCell = null;

    private int nextCellID = 100; // We'll assign new IDs to split-off cells

    private void Start()
    {
        if (display == null)
        {
            Debug.LogError("No Display assigned to Field. Please link one in the Inspector.");
            return;
        }

        // Spawn the initial batch of cells
        for (int i = 0; i < circleCount; i++)
        {
            SpawnCell(i);
        }
    }

    private void Update()
    {
        HandleVoronoiOffset();
        HandleCellSelection(); // Left click
        HandleCellSplit();     // Right click

        // Draw Voronoi each frame
        DrawVoronoiToDisplay();
    }

    private void FixedUpdate()
    {
        // Let each cell's behavior run
        float dt = Time.fixedDeltaTime;
        foreach (Cell c in cells)
        {
            c.behavior?.PerformBehavior(dt, c, this);
        }

        // Then apply repulsive forces
        ApplyRepulsionForces();
    }

    /// <summary>
    /// Reads WASD + left mouse dragging + mouse wheel zoom for the Voronoi "camera."
    /// </summary>
    private void HandleVoronoiOffset()
    {
        // 1) WASD Panning (only if not currently dragging)
        if (!isDragging)
        {
            if (Input.GetKey(KeyCode.W)) voronoiY += cameraMoveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.S)) voronoiY -= cameraMoveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.A)) voronoiX -= cameraMoveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.D)) voronoiX += cameraMoveSpeed * Time.deltaTime;
        }

        // 2) Left Mouse Drag for "camera" movement
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

        // 3) Mouse Wheel Zoom (only if not currently dragging)
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
    /// Left-click to select a cell (if within innerRadius).
    /// If we successfully select a new cell, we set it to ControlledBehavior,
    /// and revert the previously controlled cell to IdleBehavior.
    /// </summary>
    private void HandleCellSelection()
    {
        if (Input.GetMouseButtonDown(0)) // left mouse
        {
            Vector2Int? pixelCoords = display.TranslateMouseToTextureCoordinates();
            if (!pixelCoords.HasValue) return;

            Vector2 clickWorldPos = PixelToWorld(pixelCoords.Value.x, pixelCoords.Value.y);

            float closestDistance = float.MaxValue;
            Cell closestCell = null;

            foreach (Cell c in cells)
            {
                float dist = Vector2.Distance(clickWorldPos, c.transform.position);
                if (dist < c.innerRadius && dist < closestDistance)
                {
                    closestDistance = dist;
                    closestCell = c;
                }
            }

            if (closestCell != null)
            {
                // If there's a currently controlled cell that's different, revert it to Idle
                if (currentlyControlledCell != null && currentlyControlledCell != closestCell)
                {
                    currentlyControlledCell.behavior = new IdleBehavior();
                }
                // Mark the new cell as controlled
                currentlyControlledCell = closestCell;
                currentlyControlledCell.behavior = new ControlledBehavior();
            }
            else
            {
                // If we clicked empty space, revert the currently controlled cell to Idle
                if (currentlyControlledCell != null)
                {
                    currentlyControlledCell.behavior = new IdleBehavior();
                    currentlyControlledCell = null;
                }
            }
        }
    }

    /// <summary>
    /// Right-click to split a cell into two smaller cells of half the area each.
    /// </summary>
    private void HandleCellSplit()
    {
        if (Input.GetMouseButtonDown(1)) // right mouse
        {
            Vector2Int? pixelCoords = display.TranslateMouseToTextureCoordinates();
            if (!pixelCoords.HasValue) return;

            Vector2 clickWorldPos = PixelToWorld(pixelCoords.Value.x, pixelCoords.Value.y);

            // Find which cell was clicked (if any)
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
                SplitCell(clickedCell);
            }
        }
    }

    /// <summary>
    /// Splits the given cell into two new cells of half the area each (radius = oldRadius / sqrt(2)).
    /// Places them near the old cell’s position, then destroys the old cell.
    /// </summary>
    private void SplitCell(Cell oldCell)
    {
        // If the old cell is currently controlled, we’ll un-control it
        if (currentlyControlledCell == oldCell)
        {
            currentlyControlledCell = null;
        }

        float oldOuter = oldCell.outerRadius;
        float oldInner = oldCell.innerRadius;

        // The new radius to achieve half-area => oldRadius / sqrt(2).
        float newOuter = oldOuter / Mathf.Sqrt(2f);
        float newInner = newOuter * 0.5f; // same ratio as before

        float mass = oldCell.rb.mass;

        // We'll spawn them around the old position with a small random offset
        Vector2 oldPos = oldCell.transform.position;
        Vector2 offset = Random.insideUnitCircle.normalized * 0.25f;

        // Create child A
        SpawnChildCell(
            oldPos + offset,
            newOuter,
            newInner,
            mass,
            oldCell.color,
            oldCell.collisionDampFactor,
            oldCell.growthRate,
            oldCell.maximumSize
        );

        // Create child B
        SpawnChildCell(
            oldPos - offset,
            newOuter,
            newInner,
            mass,
            oldCell.color,
            oldCell.collisionDampFactor,
            oldCell.growthRate,
            oldCell.maximumSize
        );

        // Remove the old cell from the list
        cells.Remove(oldCell);
        Destroy(oldCell.gameObject);
    }

    /// <summary>
    /// Spawns a new cell GameObject with the Cell component, random position,
    /// random radius, etc. Called initially at Start().
    /// Each new cell defaults to IdleBehavior, but we can override later.
    /// </summary>
    private void SpawnCell(int cellIndex)
    {
        GameObject cellObj = new GameObject("Cell_" + cellIndex);
        cellObj.transform.position = new Vector3(
            Random.Range(-spawnRange.x, spawnRange.x),
            Random.Range(-spawnRange.y, spawnRange.y),
            0f
        );

        Cell cellComp = cellObj.AddComponent<Cell>();

        // Assign IDs & references
        cellComp.cellID = cellIndex;
        cellComp.rb.gravityScale = 0f;
        cellComp.rb.drag = circleDrag;
        cellComp.rb.angularDrag = 0f;
        cellComp.rb.mass = 10f;

        // Assign default movement limits
        cellComp.maxSpeed = defaultMaxSpeed;
        cellComp.maxAcceleration = defaultMaxAcceleration;

        // Pick random radius
        float randomRadius = Random.Range(minCircleRadius, maxCircleRadius);
        cellComp.innerRadius = randomRadius;
        cellComp.outerRadius = randomRadius * 2f;

        // Pick random color
        cellComp.color = Random.ColorHSV();

        // Add outer collider for Voronoi detection & store it
        CircleCollider2D outerColl = cellObj.AddComponent<CircleCollider2D>();
        outerColl.radius = cellComp.outerRadius;
        outerColl.isTrigger = true;
        cellComp.outerCollider = outerColl;

        // The inner collider for collisions:
        CircleCollider2D innerColl = cellObj.AddComponent<CircleCollider2D>();
        innerColl.radius = cellComp.innerRadius;
        cellComp.innerCollider = innerColl;

        // Default to IdleBehavior
        cellComp.behavior = new IdleBehavior();

        // Keep track of this new cell
        cells.Add(cellComp);
    }

    /// <summary>
    /// Spawns a cell with specific parameters (used when splitting).
    /// </summary>
    private Cell SpawnChildCell(
        Vector2 position,
        float outerR,
        float innerR,
        float mass,
        Color color,
        float collisionDamp,
        float growthRate,
        float maxSize
    )
    {
        GameObject cellObj = new GameObject("Cell_" + nextCellID);
        nextCellID++;

        cellObj.transform.position = position;

        Cell cellComp = cellObj.AddComponent<Cell>();
        cellComp.cellID = cellComp.GetInstanceID(); // or store (nextCellID - 1) if you want
        cellComp.rb.gravityScale = 0f;
        cellComp.rb.drag = circleDrag;
        cellComp.rb.angularDrag = 0f;
        cellComp.rb.mass = mass;

        // Give child same default speed/accel (or vary if you like)
        cellComp.maxSpeed = defaultMaxSpeed;
        cellComp.maxAcceleration = defaultMaxAcceleration;

        // Set radius
        cellComp.innerRadius = innerR;
        cellComp.outerRadius = outerR;

        // Keep color
        cellComp.color = color;

        // Growth & collision damping
        cellComp.collisionDampFactor = collisionDamp;
        cellComp.growthRate = growthRate;
        cellComp.maximumSize = maxSize;

        // Outer collider
        CircleCollider2D outerColl = cellObj.AddComponent<CircleCollider2D>();
        outerColl.radius = cellComp.outerRadius;
        outerColl.isTrigger = true;
        cellComp.outerCollider = outerColl;

        // Inner collider
        CircleCollider2D innerColl = cellObj.AddComponent<CircleCollider2D>();
        innerColl.radius = cellComp.innerRadius;
        cellComp.innerCollider = innerColl;

        // Child also defaults to IdleBehavior
        cellComp.behavior = new IdleBehavior();

        // Add to our cells list
        cells.Add(cellComp);

        return cellComp;
    }

    /// <summary>
    /// Adds a gentle repulsive force between overlapping cells, using their outer colliders.
    /// Called from FixedUpdate.
    /// (Repulsion remains force-based, while AI/user movement uses velocity-based logic.)
    /// </summary>
    private void ApplyRepulsionForces()
    {
        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = false,
            useDepth = false
        };

        List<Collider2D> overlaps = new List<Collider2D>(16);

        for (int i = 0; i < cells.Count; i++)
        {
            Cell cA = cells[i];
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

                    // Keep this as a force-based push
                    Vector2 force = pushDir * strength;
                    cA.rb.AddForce(force);
                    cB.rb.AddForce(-force);
                }
            }
        }
    }

    /// <summary>
    /// Runs the Voronoi pixel-by-pixel algorithm, storing the winner ID in an array,
    /// applying "inner radius" darkening, and marking 1-pixel borders in white.
    /// </summary>
    private void DrawVoronoiToDisplay()
    {
        int texWidth = display.GetWidth();
        int texHeight = display.GetHeight();

        // We'll store both the "winning ID" and the "pixel color" for each pixel
        int[] winners = new int[texWidth * texHeight];
        Color[] colors = new Color[texWidth * texHeight];

        float left = voronoiX;
        float right = voronoiX + voronoiWidth;
        float bottom = voronoiY;
        float top = voronoiY + voronoiHeight;

        int layerMask = Physics2D.DefaultRaycastLayers;

        // ---- First Pass ----
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

                // Very short vertical raycast from pixelWorldPos upward
                Vector2 rayStart = pixelWorldPos + Vector2.up * 0.001f;
                Vector2 rayDir = Vector2.down;
                float rayDist = 0.002f;

                RaycastHit2D[] hits = Physics2D.RaycastAll(rayStart, rayDir, rayDist, layerMask);

                float minFractionDist = float.MaxValue;
                float winningDist = float.MaxValue;
                Cell winningCell = null;

                for (int h = 0; h < hits.Length; h++)
                {
                    Cell cData = hits[h].collider.GetComponentInParent<Cell>();
                    if (cData == null) continue;

                    float distToCenter = Vector2.Distance(pixelWorldPos, cData.transform.position);
                    if (distToCenter < cData.outerRadius)
                    {
                        float fractionDist = distToCenter / cData.outerRadius;
                        if (fractionDist < minFractionDist)
                        {
                            minFractionDist = fractionDist;
                            winningDist = distToCenter;
                            winningCell = cData;
                        }
                    }
                }

                // Decide color & record winner
                if (winningCell == null)
                {
                    winners[index] = -1;  // no cell
                    colors[index] = backgroundColor;
                }
                else
                {
                    winners[index] = winningCell.cellID;

                    // If within the cell's inner radius => darker color
                    if (winningDist < winningCell.innerRadius)
                    {
                        Color origColor = winningCell.color;
                        colors[index] = new Color(
                            origColor.r * innerDarkFactor,
                            origColor.g * innerDarkFactor,
                            origColor.b * innerDarkFactor,
                            1f
                        );
                    }
                    else
                    {
                        // Otherwise, normal cell color
                        colors[index] = winningCell.color;
                    }
                }
            }
        }

        // ---- Second Pass ----
        // Mark any pixel whose winner differs from any neighbor as a border => white
        int[] neighborOffsets =
        {
            -1,  0,  // up
             1,  0,  // down
             0, -1,  // left
             0,  1,  // right
            -1, -1,  // up-left
            -1,  1,  // up-right
             1, -1,  // down-left
             1,  1   // down-right
        };

        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                int index = y * texWidth + x;
                int myWinner = winners[index];

                if (myWinner == -1)
                    continue; // no cell => do not color border

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

        // ---- Render final colors ----
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

    /// <summary>
    /// Convert from a pixel coordinate [0..(texWidth - 1)] x [0..(texHeight - 1)]
    /// to a world position in [voronoiX..(X+Width), voronoiY..(Y+Height)].
    /// </summary>
    private Vector2 PixelToWorld(int px, int py)
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
