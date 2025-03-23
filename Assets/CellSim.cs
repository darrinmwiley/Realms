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
        PlaceCell(15, 15, Mathf.RoundToInt(initialRadius), Color.red);

        PlaceCell(45, 45, Mathf.RoundToInt(initialRadius), Color.blue);
        PlaceCell(45, 15, Mathf.RoundToInt(initialRadius), Color.yellow);
        PlaceCell(15, 45, Mathf.RoundToInt(initialRadius), Color.green);
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
        foreach (var kvp in cells)
        {
            Cell c = kvp.Value;
            if (c.Area > 0)
            {
                c.CenterOfMass = c.SumPosition / c.Area;
            }
        }
    }

    public float GetPressure(int x, int y, int[,] cellsGrid)
    {
        int cid = cellsGrid[x,y];
        if(x == 0 || y == 0 || x == display.GetWidth() - 1 || y == display.GetHeight() - 1)
        {
            return borderPressure;
        }
        if(cid == 0)
        {
            return atmosphericPressure;
        }
        Cell c = cells[cid];
        float p = c.GetPressure();
        if(IsBoundaryPixel(x,y, cellsGrid))
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
        return p;
    }

    bool IsBoundaryPixel(int x, int y, int[,] cellsGrid)
    {
        int w = display.GetWidth();
        int h = display.GetHeight();
        int cid = cellsGrid[x, y];
        if (cid == 0) return false;

        int[] dxs = { 0, 0, -1, 1 };
        int[] dys = { -1, 1, 0, 0 };

        for (int i = 0; i < 4; i++)
        {
            int nx = x + dxs[i];
            int ny = y + dys[i];
            if (nx < 0 || nx >= w || ny < 0 || ny >= h) return true;
            if (cellsGrid[nx, ny] != cid) return true;
        }
        return false;
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
                if (cid <= 0 || cid != nextGrid[x,y]) continue; 
                
                float pHere = GetPressure(x, y, nextGrid);

                for (int i = 0; i < 4; i++)
                {
                    int nx = x + dxs[i];
                    int ny = y + dys[i];
                    if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;

                    int neighborCID = nextGrid[nx, ny];
                    if (neighborCID != cid) // occupant => empty
                    {
                        float pNeighbor = GetPressure(nx, ny, nextGrid);
                        if (pHere > pNeighbor + transmissionCost)
                        {
                            nextGrid[nx, ny] = cid;
                            if(neighborCID != 0)
                            {
                                cells[neighborCID].Contract(new Vector2Int(nx, ny));
                            }
                            cells[cid].Expand(new Vector2Int(nx, ny));
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
                int cid = gridState[x, y];
                if (cid == 0 || nextGrid[x,y] != cid) continue; // skip empty or invalid occupant

                float pHere = GetPressure(x, y, nextGrid);

                // If occupant local pressure is too low compared to an adjacent empty neighbor,
                // revert occupant => empty.
                for (int i = 0; i < 4; i++)
                {
                    int nx = x + dxs[i];
                    int ny = y + dys[i];
                    if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;

                    if (nextGrid[nx, ny] == 0)
                    {
                        if (pHere + transmissionCost < atmosphericPressure)
                        {
                            // occupant can't hold this square => revert to air
                            nextGrid[x, y] = 0;
                            cells[cid].Contract(new Vector2Int(x, y));
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

        Vector2 direction = Vector2.zero;
        if (Input.GetKey(KeyCode.UpArrow)) { direction = direction + new Vector2(0, 1); }
        if (Input.GetKey(KeyCode.DownArrow)) { direction = direction + new Vector2(0, -1); }
        if (Input.GetKey(KeyCode.LeftArrow)) { direction = direction + new Vector2(-1, 0); }
        if (Input.GetKey(KeyCode.RightArrow)) { direction = direction + new Vector2(1, 0); }
        return center + direction.normalized * 2;
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

        HashSet<Vector2Int> pixels = new HashSet<Vector2Int>();

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
                    pixels.Add(new Vector2Int(i,j));
                }
            }
        }

        if (area > 0)
        {
            Vector2 com = sumPos / area;
            cells[cellID] = new Cell(cellID, area, com, c, pixels);
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

    public HashSet<Vector2Int> Pixels {get; set;}

    public Color Color { get; set; }

    // We'll track sum of occupant pixel positions so we can quickly recompute center of mass
    public Vector2 SumPosition { get; set; }

    public Cell(int id, int area, Vector2 centerOfMass, Color color, HashSet<Vector2Int> pixels)
    {
        ID = id;
        Area = pixels.Count;
        SumPosition = Vector2.zero;
        foreach(var pixel in pixels)
            SumPosition += pixel;
        FluidContent = area;  // Start with fluid == area
        CenterOfMass = centerOfMass;
        Color = color;
        Pixels = pixels;
    }

    public void Expand(Vector2Int position){
        Pixels.Add(position);
        Area++;
        SumPosition += position;
    }

    public void Contract(Vector2Int position){
        Pixels.Remove(position);
        Area--;
        SumPosition -= position;
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
