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

    private Dictionary<Vector2Int, int> gridState;   // 0=empty, else cell ID
    private int nextCellID = 1;
    private Dictionary<int, Cell> cells = new Dictionary<int, Cell>();

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
        gridState = new Dictionary<Vector2Int, int>();

        // Place one cell for demonstration
        PlaceCell(15, 15, Mathf.RoundToInt(initialRadius), Color.red);

        PlaceCell(45, 45, Mathf.RoundToInt(initialRadius), Color.blue);
        PlaceCell(45, 15, Mathf.RoundToInt(initialRadius), Color.yellow);
        PlaceCell(15, 45, Mathf.RoundToInt(initialRadius), Color.green);
    }

    public int GetCellId(Vector2Int position)
    {
        if(gridState.ContainsKey(position))
        {
            return gridState[position];
        }
        return 0;
    }

    public Cell GetCellAt(Vector2Int position)
    {
        if(GetCellId(position) == 0)
        {
            return null;
        }
        return cells[GetCellId(position)];
    }

    public static int GetCellId(Vector2Int position, Dictionary<Vector2Int, int> originalGrid, Dictionary<Vector2Int, int> updates)
    {
        if(updates.ContainsKey(position))
        {
            return updates[position];
        }
        if(originalGrid.ContainsKey(position))
        {
            return originalGrid[position];
        }
        return 0;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int? mp = display.TranslateMouseToTextureCoordinates();
            if (mp.HasValue)
            {
                int x = mp.Value.x;
                int y = mp.Value.y;
                int cid = GetCellId(new Vector2Int(x, y));
                if (cid > 0 && cells.ContainsKey(cid))
                {
                    cells[cid].AddFluid(fluidIncreaseAmount);
                    selectedCell = cells[cid];
                }
            }
        }

        //update center of masses once per tick for stability
        foreach (var kvp in cells)
        {
            Cell c = kvp.Value;
            if (c.Area > 0)
            {
                c.CenterOfMass = c.SumPosition / c.Area;
            }
        }
        PerformExpansionsAndContractions();
        UpdateHoverInfo();
        Render();
    }

    public float GetPressure(int x, int y, Dictionary<Vector2Int, int> original, Dictionary<Vector2Int, int> updates)
    {
        int cid = GetCellId(new Vector2Int(x, y), original, updates);
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
        if(IsBoundaryPixel(x,y, original, updates))
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

    bool IsBoundaryPixel(int x, int y, Dictionary<Vector2Int, int> original, Dictionary<Vector2Int, int> updates)
    {
        int w = display.GetWidth();
        int h = display.GetHeight();
        int cid = GetCellId(new Vector2Int(x, y), original, updates);
        if (cid == 0) return false;
        return cells[cid].BoundaryPixels.Contains(new Vector2Int(x, y));

        int[] dxs = { 0, 0, -1, 1 };
        int[] dys = { -1, 1, 0, 0 };

        for (int i = 0; i < 4; i++)
        {
            int nx = x + dxs[i];
            int ny = y + dys[i];
            if (nx < 0 || nx >= w || ny < 0 || ny >= h) return true;
            if (GetCellId(new Vector2Int(nx, ny), original, updates) != cid) return true;
        }
        return false;
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
        Dictionary<Vector2Int, int> updates = new Dictionary<Vector2Int, int>();
        ExpandOccupants(gridState, updates);

        // Pass 2: contractions
        ContractOccupants(gridState, updates);

        // commit updates
        foreach (var kvp in updates)
        {
            if(kvp.Value == 0 && gridState.ContainsKey(kvp.Key))
            {
                gridState.Remove(kvp.Key);
            }
            else
            {
                gridState[kvp.Key] = kvp.Value;
            }
        }
    }

    /// <summary>
    /// occupant -> empty expansions if occupant pressure > neighbor empty pressure + cost
    /// This is occupant squares flowing outward to empty squares.
    /// </summary>
    void ExpandOccupants(Dictionary<Vector2Int, int> original, Dictionary<Vector2Int, int> updates)
    {
        int w = display.GetWidth();
        int h = display.GetHeight();

        int[] dxs = { 0, 0, -1, 1 };
        int[] dys = { -1, 1, 0, 0 };

        foreach(var kvp in cells)
        {
            int cid = kvp.Key;
            Cell c = kvp.Value;
            HashSet<Vector2Int> boundaryPixelsCopy = new HashSet<Vector2Int>(c.BoundaryPixels);
            foreach(var pixel in boundaryPixelsCopy)
            {
                int x = pixel.x;
                int y = pixel.y;
                if (cid != GetCellId(new Vector2Int(x, y), original, updates)) continue; 
                
                float pHere = GetPressure(x, y, original, updates);

                for (int i = 0; i < 4; i++)
                {
                    int nx = x + dxs[i];
                    int ny = y + dys[i];
                    if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;

                    int neighborCID = GetCellId(new Vector2Int(nx, ny), original, updates);
                    if (neighborCID != cid) // occupant => empty
                    {
                        float pNeighbor = GetPressure(nx, ny, original, updates);
                        if (pHere > pNeighbor + transmissionCost)
                        {
                            updates[new Vector2Int(nx, ny)] = cid;
                            if(neighborCID != 0)
                            {
                                cells[neighborCID].Contract(new Vector2Int(nx, ny), original, updates);
                            }
                            cells[cid].Expand(new Vector2Int(nx, ny), original, updates);
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
    void ContractOccupants(Dictionary<Vector2Int, int> original, Dictionary<Vector2Int, int> updates)
    {
        int w = display.GetWidth();
        int h = display.GetHeight();

        int[] dxs = { 0, 0, -1, 1 };
        int[] dys = { -1, 1, 0, 0 };

        foreach(var kvp in cells)
        {
            int cid = kvp.Key;
            Cell c = kvp.Value;
            HashSet<Vector2Int> boundaryPixelsCopy = new HashSet<Vector2Int>(c.BoundaryPixels);
            foreach(var pixel in boundaryPixelsCopy)
            {
                int x = pixel.x;
                int y = pixel.y;

                if (GetCellId(new Vector2Int(x,y), original, updates) != cid) continue; // skip empty or invalid occupant

                float pHere = GetPressure(x, y, original, updates);

                // If occupant local pressure is too low compared to an adjacent empty neighbor,
                // revert occupant => empty.
                for (int i = 0; i < 4; i++)
                {
                    int nx = x + dxs[i];
                    int ny = y + dys[i];
                    if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;

                    if (GetCellId(new Vector2Int(nx, ny), original, updates) == 0)
                    {
                        if (pHere + transmissionCost < atmosphericPressure)
                        {
                            // occupant can't hold this square => revert to air
                            updates[new Vector2Int(x, y)] = 0;
                            cells[cid].Contract(new Vector2Int(x, y), original, updates);
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
            hoveredLocalPressure = GetPressure(x, y, gridState, gridState);

            int cid = GetCellId(new Vector2Int(x, y), gridState, gridState);
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
                int cid = GetCellId(new Vector2Int(x, y), gridState, gridState);

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
        int cid = GetCellId(new Vector2Int(x, y), gridState, gridState);
        if (cid == 0) return false;

        return cells[cid].BoundaryPixels.Contains(new Vector2Int(x, y));
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
                    gridState[new Vector2Int(i, j)] = cellID;
                    sumPos += new Vector2(i, j);
                    area++;
                    pixels.Add(new Vector2Int(i,j));
                }
            }
        }

        if (area > 0)
        {
            Vector2 com = sumPos / area;
            cells[cellID] = new Cell(cellID, area, com, c, pixels, gridState);
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
    public HashSet<Vector2Int> BoundaryPixels {get; set;}

    public Color Color { get; set; }

    // We'll track sum of occupant pixel positions so we can quickly recompute center of mass
    public Vector2 SumPosition { get; set; }

    public Cell(int id, int area, Vector2 centerOfMass, Color color, HashSet<Vector2Int> pixels, Dictionary<Vector2Int, int> cellsGrid)
    {
        ID = id;
        Area = pixels.Count;
        SumPosition = Vector2.zero;
        Pixels = pixels;
        BoundaryPixels = new HashSet<Vector2Int>();
        foreach(var pixel in pixels){
            SumPosition += pixel;
            BoundaryAddCheck(pixel, cellsGrid, cellsGrid);
        }
        FluidContent = area;  // Start with fluid == area
        CenterOfMass = centerOfMass;
        Color = color;
        
    }

    public void BoundaryRemoveCheck(Vector2Int position, Dictionary<Vector2Int, int> original, Dictionary<Vector2Int, int> updates){
        if(!Pixels.Contains(position))
        {
            return;
        }
        if(!BoundaryPixels.Contains(position))
        {
            return;
        }
        int[] dxs = { 0, 0, -1, 1 };
        int[] dys = { -1, 1, 0, 0 };
        for(int i = 0;i<4;i++)
        {
            if(CellSim.GetCellId(new Vector2Int(position.x + dxs[i], position.y + dys[i]), original, updates) != ID)
            {
                return;
            }
        }
        BoundaryPixels.Remove(position);
    }

    public void BoundaryAddCheck(Vector2Int position, Dictionary<Vector2Int, int> original, Dictionary<Vector2Int, int> updates)
    {
        if(!Pixels.Contains(position))
        {
            return;
        }
        if(BoundaryPixels.Contains(position))
        {
            return;
        }
        int[] dxs = { 0, 0, -1, 1 };
        int[] dys = { -1, 1, 0, 0 };
        for (int i = 0; i < 4; i++)
        {
            if (CellSim.GetCellId(new Vector2Int(position.x + dxs[i], position.y + dys[i]), original, updates) != ID)
            {
                BoundaryPixels.Add(position);
                return;
            }
        }
    }

    public void Expand(Vector2Int position, Dictionary<Vector2Int, int> original, Dictionary<Vector2Int, int> updates){
        Pixels.Add(position);
        Area++;
        SumPosition += position;
        BoundaryPixels.Add(position);
        int[] dxs = { 0, 0, -1, 1 };
        int[] dys = { -1, 1, 0, 0 };
        for( int i = 0;i<4;i++)
        {
            BoundaryRemoveCheck(new Vector2Int(position.x + dxs[i], position.y + dys[i]), original, updates);
        }
    }

    public void Contract(Vector2Int position, Dictionary<Vector2Int, int> original, Dictionary<Vector2Int, int> updates){
        Pixels.Remove(position);
        Area--;
        SumPosition -= position;
        BoundaryPixels.Remove(position);
        int[] dxs = { 0, 0, -1, 1 };
        int[] dys = { -1, 1, 0, 0 };
        for( int i = 0;i<4;i++)
        {
            BoundaryAddCheck(new Vector2Int(position.x + dxs[i], position.y + dys[i]), original, updates);
        }
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
