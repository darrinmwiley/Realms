using UnityEngine;
using System.Collections.Generic;
using System;

public class CellSim : MonoBehaviour
{
    public Display display;

    // --- Parameters ---
    [Header("Cell Parameters")]
    public float initialRadius = 10f;
    public float fluidIncreaseAmount = 5.0f;

    [Header("Pressures & Costs")]
    public float atmosphericPressure = 1.0f;   // Baseline for non-border empty squares
    public float borderPressure = 100f;        // Very high pressure for squares along the simulation border
    public float transmissionCost = 0.05f;     // Pressure difference needed to spread or revert

    [Header("Roundness Bonus")]
    public float roundnessBonus = 0.1f; // For occupant boundary squares: +/- this bonus if dist < or > desired radius

    private int[,] gridState;   // 0=empty, else cell ID
    private int nextCellID = 1;
    private Dictionary<int, Cell> cells = new Dictionary<int, Cell>();

    // We'll keep a local pressure array each frame
    private float[,] localPressure;

    // Debug / Hover Info
    private Vector2Int? hoveredPixel = null;
    private float hoveredLocalPressure = 0f;
    private int? hoveredCellID = null;
    private int hoveredCellArea = 0;
    private Vector2 hoveredCellCOM = Vector2.zero;

    private Cell selectedCell;

    void Start()
    {
        if (display == null)
        {
            Debug.LogError("Display is not assigned!");
            return;
        }

        int w = display.GetWidth();
        int h = display.GetHeight();
        gridState = new int[w, h];
        localPressure = new float[w, h];

        // Place one cell for demonstration
        PlaceCell(20, 20, Mathf.RoundToInt(initialRadius), Color.red);

        PlaceCell(40, 40, Mathf.RoundToInt(initialRadius), Color.blue);
    }

    void Update()
    {
        // 1) Optional: add fluid on left-click
        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int? mp = display.TranslateMouseToTextureCoordinates();
            if (mp.HasValue)
            {
                int x = mp.Value.x;
                int y = mp.Value.y;
                int cid = gridState[x, y];
                if (cid > 0 && cells.ContainsKey(cid))
                {
                    cells[cid].AddFluid(fluidIncreaseAmount);
                    selectedCell = cells[cid];
                }
            }
        }

        // 3) Recompute each cell's area & center of mass (after last frame's updates)
        RefreshCellProperties();

        // 4) Compute local pressures (including roundness & movement biases)
        ComputeLocalPressures();

        // 5) Perform expansions & contractions in two passes
        PerformExpansionsAndContractions();

        // 6) Update hover info (debug details)
        UpdateHoverInfo();

        // 7) Render final result
        Render();
    }

    /// <summary>
    /// Refresh each cell's area & center of mass by scanning the grid.
    /// This ensures expansions/contractions are reflected in the cell's data.
    /// </summary>
    void RefreshCellProperties()
    {
        // Reset each cell's area & sums
        foreach (var c in cells.Values)
        {
            c.Area = 0;
            c.SumPosition = Vector2.zero;
        }

        int w = display.GetWidth();
        int h = display.GetHeight();

        // Tally occupant squares
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int cid = gridState[x, y];
                if (cid > 0 && cells.ContainsKey(cid))
                {
                    cells[cid].Area++;
                    cells[cid].SumPosition += new Vector2(x, y);
                }
            }
        }

        // Now compute center of mass
        foreach (var kvp in cells)
        {
            Cell c = kvp.Value;
            if (c.Area > 0)
            {
                c.CenterOfMass = c.SumPosition / c.Area;
            }
        }
    }

    /// <summary>
    /// Recompute local pressures for occupant & empty squares.
    /// - border squares => borderPressure
    /// - empty interior => atmosphericPressure
    /// - occupant squares => base cell pressure + boundary bonus + direction bias + roundness bonus
    /// </summary>
    void ComputeLocalPressures()
    {
        int w = display.GetWidth();
        int h = display.GetHeight();

        // First pass: fill empty squares with baseline, occupant squares with placeholder
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool isBorder = (x == 0 || x == w - 1 || y == 0 || y == h - 1);
                int cid = gridState[x, y];

                if (cid == 0)
                {
                    // empty squares
                    localPressure[x, y] = (isBorder ? borderPressure : atmosphericPressure);
                }
                else
                {
                    // occupant squares => we'll define occupant local pressure in second pass
                    localPressure[x, y] = (isBorder ? borderPressure : 0f);
                }
            }
        }

        // Second pass: occupant squares (not on outer boundary)
        // Add base pressure, boundary bonus, direction biases, roundness bonus.
        int width = display.GetWidth();
        int height = display.GetHeight();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int cid = gridState[x, y];
                if (cid <= 0) 
                    continue; // skip empty squares
                if (!cells.ContainsKey(cid))
                    continue; // skip unknown occupant

                // occupant is on the grid border => localPressure stays at borderPressure
                bool isGridEdge = (x == 0 || x == width - 1 || y == 0 || y == height - 1);
                if (isGridEdge) 
                    continue;

                Cell c = cells[cid];
                float occupantPressure = c.GetPressure();
                float p = occupantPressure;

                // Roundness bonus for occupant boundary squares
                if (IsBoundaryPixel(x, y))
                {
                    float dx = x - GetEffectiveCenterOfMass(c).x;
                    float dy = y - GetEffectiveCenterOfMass(c).y;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float desiredRadius = 0f;
                    if (c.Area > 0)
                        desiredRadius = Mathf.Sqrt(c.Area / Mathf.PI);

                    float differenceRatio = (desiredRadius - dist) / desiredRadius;

                    p += roundnessBonus * differenceRatio;
                }

                localPressure[x, y] = p;
            }
        }
    }

    /// <summary>
    /// We do expansions in one pass, then do contractions in a second pass,
    /// and update the grid state at the end. This approach avoids flicker
    /// from expansions and contractions happening simultaneously on the same squares.
    /// </summary>
    void PerformExpansionsAndContractions()
    {
        int w = display.GetWidth();
        int h = display.GetHeight();

        // Pass 1: expansions
        int[,] afterExpansion = (int[,])gridState.Clone();
        ExpandOccupants(afterExpansion);

        // Pass 2: contractions
        int[,] afterContraction = (int[,])afterExpansion.Clone();
        ContractOccupants(afterContraction);

        // Now adopt the final grid
        gridState = afterContraction;
    }

    /// <summary>
    /// occupant -> empty expansions if occupant pressure > neighbor empty pressure + cost
    /// This is occupant squares flowing outward to empty squares.
    /// </summary>
    void ExpandOccupants(int[,] nextGrid)
    {
        int w = display.GetWidth();
        int h = display.GetHeight();

        int[] dxs = { 0, 0, -1, 1 };
        int[] dys = { -1, 1, 0, 0 };

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int cid = gridState[x, y];
                if (cid <= 0) continue; 
                
                float pHere = localPressure[x, y];

                for (int i = 0; i < 4; i++)
                {
                    int nx = x + dxs[i];
                    int ny = y + dys[i];
                    if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;

                    int neighborCID = gridState[nx, ny];
                    if (neighborCID != cid) // occupant => empty
                    {
                        float pNeighbor = localPressure[nx, ny];
                        if (pHere > pNeighbor + transmissionCost)
                        {
                            nextGrid[nx, ny] = cid;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// occupant -> empty contraction if occupant local pressure + cost < neighbor empty local pressure
    /// This effectively lets occupant squares revert to air if they can't maintain enough pressure
    /// in the presence of a higher pressure empty neighbor.
    /// </summary>
    void ContractOccupants(int[,] nextGrid)
    {
        int w = display.GetWidth();
        int h = display.GetHeight();

        int[] dxs = { 0, 0, -1, 1 };
        int[] dys = { -1, 1, 0, 0 };

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int cid = nextGrid[x, y];
                if (cid <= 0) continue; // skip empty or invalid occupant

                float pHere = localPressure[x, y];

                // If occupant local pressure is too low compared to an adjacent empty neighbor,
                // revert occupant => empty.
                for (int i = 0; i < 4; i++)
                {
                    int nx = x + dxs[i];
                    int ny = y + dys[i];
                    if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;

                    int neighborCID = nextGrid[nx, ny];
                    if (neighborCID == 0)
                    {
                        // neighbor is empty => check if occupant is "losing"
                        float pNeighbor = localPressure[nx, ny];
                        if (pHere + transmissionCost < pNeighbor)
                        {
                            // occupant can't hold this square => revert to air
                            nextGrid[x, y] = 0;
                            break; // done with this occupant square
                        }
                    }
                }
            }
        }
    }

    Vector2 GetEffectiveCenterOfMass(Cell c)
    {
        Vector2 center = c.CenterOfMass;
        if(c != selectedCell)
        {
            return center;
        }
        if (Input.GetKey(KeyCode.UpArrow))    { center = new Vector2(center.x, center.y + 2); }
        if (Input.GetKey(KeyCode.DownArrow))  { center = new Vector2(center.x, center.y - 2); }
        if (Input.GetKey(KeyCode.LeftArrow))  { center = new Vector2(center.x - 2, center.y); }
        if (Input.GetKey(KeyCode.RightArrow)) { center = new Vector2(center.x + 2, center.y); }
        return center;
    }

    /// <summary>
    /// For debug overlay: which cell are we hovering, local pressure, etc.
    /// </summary>
    void UpdateHoverInfo()
    {
        hoveredPixel = display.TranslateMouseToTextureCoordinates();
        hoveredLocalPressure = 0f;
        hoveredCellID = null;
        hoveredCellArea = 0;
        hoveredCellCOM = Vector2.zero;

        if (hoveredPixel.HasValue)
        {
            int x = hoveredPixel.Value.x;
            int y = hoveredPixel.Value.y;
            hoveredLocalPressure = localPressure[x, y];

            int cid = gridState[x, y];
            if (cid > 0 && cells.ContainsKey(cid))
            {
                hoveredCellID = cid;
                hoveredCellArea = cells[cid].Area;
                hoveredCellCOM = cells[cid].CenterOfMass;
            }
        }
    }

    /// <summary>
    /// Renders occupant squares (boundary vs. interior) and empty squares
    /// with border squares in blue, occupant boundaries in white, occupant interior in grey, etc.
    /// Also draws center of mass in yellow, hovered occupant pixel in magenta.
    /// </summary>
    void Render()
    {
        int w = display.GetWidth();
        int h = display.GetHeight();

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int cid = gridState[x, y];

                bool isGridEdge = (x == 0 || x == w - 1 || y == 0 || y == h - 1);
                if (cid == 0)
                {
                    // empty squares
                    display.SetPixel(x, y, isGridEdge ? Color.blue : Color.black);
                }
                else
                {
                    // occupant squares
                    if (isGridEdge)
                    {
                        // occupant on outer boundary => color them blue
                        display.SetPixel(x, y, Color.blue);
                    }
                    else
                    {
                        bool occupantBoundary = IsBoundaryPixel(x, y);
                        display.SetPixel(x, y, occupantBoundary ? Color.white : cells[cid].Color);
                    }
                }
            }
        }

        // Show each cell's center of mass in yellow
        foreach (var pair in cells)
        {
            var c = pair.Value;
            Vector2 cm = GetEffectiveCenterOfMass(c);
            int cx = Mathf.RoundToInt(cm.x);
            int cy = Mathf.RoundToInt(cm.y);
            if (cx >= 0 && cx < w && cy >= 0 && cy < h)
            {
                display.SetPixel(cx, cy, Color.yellow);
            }
        }

        // highlight hovered occupant pixel in magenta (if any)
        if (hoveredPixel.HasValue)
        {
            int hx = hoveredPixel.Value.x;
            int hy = hoveredPixel.Value.y;
            display.SetPixel(hx, hy, Color.magenta);
        }
    }

    bool IsBoundaryPixel(int x, int y)
    {
        int w = display.GetWidth();
        int h = display.GetHeight();
        int cid = gridState[x, y];
        if (cid == 0) return false;

        int[] dxs = { 0, 0, -1, 1 };
        int[] dys = { -1, 1, 0, 0 };

        for (int i = 0; i < 4; i++)
        {
            int nx = x + dxs[i];
            int ny = y + dys[i];
            if (nx < 0 || nx >= w || ny < 0 || ny >= h) return true;
            if (gridState[nx, ny] != cid) return true;
        }
        return false;
    }

    void PlaceCell(int x, int y, int r, Color c)
    {
        int w = display.GetWidth();
        int h = display.GetHeight();

        int cellID = nextCellID++;
        int area = 0;
        Vector2 sumPos = Vector2.zero;

        for (int i = Mathf.Max(0, x - r); i <= Mathf.Min(w - 1, x + r); i++)
        {
            for (int j = Mathf.Max(0, y - r); j <= Mathf.Min(h - 1, y + r); j++)
            {
                float distSqr = (i - x)*(i - x) + (j - y)*(j - y);
                if (distSqr <= r*r)
                {
                    gridState[i, j] = cellID;
                    sumPos += new Vector2(i, j);
                    area++;
                }
            }
        }

        if (area > 0)
        {
            Vector2 com = sumPos / area;
            cells[cellID] = new Cell(cellID, area, com, c);
        }
    }

    void OnGUI()
    {
        GUIStyle st = new GUIStyle();
        st.fontSize = 18;
        st.normal.textColor = Color.white;

        float yOff = 10f;
        float xOff = 10f;

        // Build debug info
        string info = $"TransmissionCost={transmissionCost:F2}" +
                      $"RoundnessBonus={roundnessBonus:F2}\n" +
                      $"click occupant => add fluid)\n\n";

        if (hoveredPixel.HasValue)
        {
            int hx = hoveredPixel.Value.x;
            int hy = hoveredPixel.Value.y;
            info += $"Hover: ({hx}, {hy}), P={hoveredLocalPressure:F2}\n";
            if (hoveredCellID.HasValue)
            {
                info += $"Cell ID: {hoveredCellID.Value}, Area={hoveredCellArea}, " +
                        $"COM=({hoveredCellCOM.x:F2},{hoveredCellCOM.y:F2})\n";
            }
            else
            {
                info += "Empty or border space.\n";
            }
        }
        else
        {
            info += "Mouse outside the display.\n";
        }

        GUI.Label(new Rect(xOff, yOff, 500, 200), info, st);
    }
}

// -----------------------------------------------------------------
// Cell class
// -----------------------------------------------------------------
public class Cell
{
    public int ID { get; private set; }
    public int Area { get; set; }
    public float FluidContent { get; private set; }
    public Vector2 CenterOfMass { get; set; }

    public Color Color { get; set; }

    // We'll track sum of occupant pixel positions so we can quickly recompute center of mass
    public Vector2 SumPosition { get; set; }

    public Cell(int id, int area, Vector2 centerOfMass, Color color)
    {
        ID = id;
        Area = area;
        FluidContent = area;  // Start with fluid == area
        CenterOfMass = centerOfMass;
        SumPosition = Vector2.zero;
        Color = color;
    }

    public void AddFluid(float amount)
    {
        FluidContent += amount;
    }

    /// <summary>
    /// Base cell pressure ignoring local bonuses (boundary, roundness, movement).
    /// </summary>
    public float GetPressure()
    {
        return (Area > 0) ? FluidContent / Area : 0f;
    }
}
