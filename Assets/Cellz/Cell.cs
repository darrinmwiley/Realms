// Cell.cs

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents one “cell”:
///  - Stores the cell’s radii, color, ID
///  - Holds the Rigidbody2D / CircleCollider2D references
///  - Handles collision damping in OnCollisionEnter2D
///  - Grows over time, capped at a maximumSize
///  - Stores a reference to an ICellBehavior for AI/steering
///  - Splitting is handled here, so any code can call cell.Split()
///  - Now owns its own Voronoi compute‐shader dispatch via the shared static shader
/// </summary>
public class Cell : MonoBehaviour, IRenderable
{
    /* ─────────── Static, shared ComputeShader cache ─────────── */

    private const string CS_PATH   = "ComputeShaders/Voronoi"; // under Assets/Resources/
    private const string CS_KERNEL = "CSPerCell";
    private const int    TG_SIZE   = 8;                        // matches your shader

    private static ComputeShader _cs;
    private static int           _kernel;
    private static bool          _ready;
    private static ComputeBuffer _dummyNeighborBuf;

    private static void EnsureShaderLoaded()
    {
        if (_ready) return;
        _cs = Resources.Load<ComputeShader>(CS_PATH);
        if (_cs == null)
        {
            Debug.LogError($"Cell: failed to load ComputeShader at Resources/{CS_PATH}.compute");
            return;
        }
        _kernel = _cs.FindKernel(CS_KERNEL);

        _dummyNeighborBuf = new ComputeBuffer(1, sizeof(float) * 4);
        _dummyNeighborBuf.SetData(new Vector4[] { Vector4.zero });

        _ready = true;
    }

    /* ─────────── Inspector / runtime fields ─────────── */

    [Header("Per-Cell Settings")]
    [Tooltip("A unique ID assigned by the Field script at spawn time.")]
    public int cellID;

    public bool dead;                // not used by rendering

    public static int nextCellId = 0;

    [Tooltip("Inner radius used for collision & darker color.")]
    public float innerRadius;

    [Tooltip("Outer radius used for Voronoi detection & color influence.")]
    public float outerRadius;

    [Tooltip("Base color of this cell.")]
    public Color color;

    [Header("Growth Settings")]
    [Tooltip("How fast the cell’s outer radius grows per second.")]
    public float growthRate = 0.5f;

    [Tooltip("Maximum outer radius allowed.")]
    public float maximumSize = 4f;

    [Header("Movement Limits (used by behaviors)")]
    [Tooltip("Maximum speed (units/sec) this cell can move via AI or user input.")]
    public float maxSpeed = 30f;

    [Tooltip("Maximum acceleration (units/sec^2) for AI or user input.")]
    public float maxAcceleration = 30f;

    [HideInInspector] public ICellBehavior behavior;
    [HideInInspector] public Field         field;
    [HideInInspector] public Rigidbody2D   rb;
    [HideInInspector] public CircleCollider2D outerCollider;
    [HideInInspector] public CircleCollider2D innerCollider;

    /* ─────────── Growth ─────────── */

    public void HandleGrowth()
    {
        if (outerRadius < maximumSize)
        {
            outerRadius += growthRate * Time.deltaTime;
            if (outerRadius > maximumSize)
                outerRadius = maximumSize;

            innerRadius = outerRadius * 0.5f;
            UpdateColliderSizes();
        }
    }

    private void UpdateColliderSizes()
    {
        if (outerCollider) outerCollider.radius = outerRadius;
        if (innerCollider) innerCollider.radius = innerRadius;
    }

    /* ─────────── GPU Voronoi dispatch ─────────── */

    /// <summary>
    /// Writes this cell’s region into <paramref name="idRT"/> using the shared
    /// Voronoi compute shader.  Field will colourise / edge-detect afterwards.
    /// </summary>
    public void Render(
        RenderTexture idRT,
        int           mappedIdPlusOne,
        float         vorX, float vorY,
        float         vorW, float vorH,
        int           texW, int texH)
    {
        EnsureShaderLoaded();
        if (!_ready) return;

        // compute inv scales
        float invW = texW / vorW;
        float invH = texH / vorH;

        Vector2 pos  = transform.position;
        Vector2 wMin = pos - Vector2.one * outerRadius;
        Vector2 wMax = pos + Vector2.one * outerRadius;

        int minX = Mathf.Clamp(Mathf.FloorToInt((wMin.x - vorX) * invW), 0, texW);
        int minY = Mathf.Clamp(Mathf.FloorToInt((wMin.y - vorY) * invH), 0, texH);
        int maxX = Mathf.Clamp(Mathf.CeilToInt ((wMax.x - vorX) * invW), 0, texW);
        int maxY = Mathf.Clamp(Mathf.CeilToInt ((wMax.y - vorY) * invH), 0, texH);

        int w = maxX - minX;
        int h = maxY - minY;
        if (w <= 0 || h <= 0) return;  // off-screen

        // gather overlapping neighbors
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, outerRadius);
        List<Vector4> nbList = new List<Vector4>();
        foreach (var col in hits)
            if (col.TryGetComponent<Cell>(out var oc) && oc.cellID != cellID)
                nbList.Add(new Vector4(
                    oc.transform.position.x,
                    oc.transform.position.y,
                    1f / oc.outerRadius,
                    0f));

        ComputeBuffer nbBuf = null;
        if (nbList.Count > 0)
        {
            _cs.SetInt("neighborCount", nbList.Count);
            nbBuf = new ComputeBuffer(nbList.Count, sizeof(float) * 4);
            nbBuf.SetData(nbList.ToArray());
            _cs.SetBuffer(_kernel, "Neighbors", nbBuf);
        }
        else
        {
            _cs.SetInt("neighborCount", 0);
            _cs.SetBuffer(_kernel, "Neighbors", _dummyNeighborBuf);
        }

        // set uniforms + UAV
        _cs.SetVector("cellCenter", new Vector4(pos.x, pos.y, 0f, 0f));
        _cs.SetFloat ("invRadius", 1f / outerRadius);
        _cs.SetInt   ("cellID",    mappedIdPlusOne);
        _cs.SetInts  ("minPixel",  minX, minY);
        _cs.SetInts  ("maxPixel",  maxX, maxY);
        _cs.SetFloat ("invW",      invW);
        _cs.SetFloat ("invH",      invH);
        _cs.SetFloat ("originX",   vorX);
        _cs.SetFloat ("originY",   vorY);
        _cs.SetTexture(_kernel, "IDResult", idRT);

        int gx = Mathf.CeilToInt(w / (float)TG_SIZE);
        int gy = Mathf.CeilToInt(h / (float)TG_SIZE);
        _cs.Dispatch(_kernel, gx, gy, 1);

        nbBuf?.Release();
    }

    /* ─────────── Splitting ─────────── */

    public Cell[] Split()
    {
        if (behavior is BoidsBehavior boids) boids.UnregisterCell(this);

        float newOuter = outerRadius / Mathf.Sqrt(2f);
        float newInner = newOuter * 0.5f;

        Vector2 oldPos = transform.position;
        Vector2 offset = innerRadius / 8f * Random.insideUnitCircle.normalized;

        var childA = CreateChildCell(oldPos + offset, newOuter, newInner, rb.mass, color, growthRate, maximumSize);
        var childB = CreateChildCell(oldPos - offset, newOuter, newInner, rb.mass, color, growthRate, maximumSize);

        RemoveSelf();
        return new[] { childA, childB };
    }

    public void RemoveSelf()
    {
        behavior?.OnCellDestroyed(this);
        field.RemoveCell(this);
    }

    private Cell CreateChildCell(Vector2 pos, float oR, float iR, float mass, Color col, float gr, float ms)
    {
        return Cell.NewBuilder()
            .SetPosition(pos)
            .SetInnerRadius(iR)
            .SetOuterRadius(oR)
            .SetColor(col)
            .SetGrowthRate(gr)
            .SetMaximumSize(ms)
            .SetMass(mass)
            .SetDrag(rb.drag)
            .SetAngularDrag(rb.angularDrag)
            .SetField(field)
            .SetBehavior(new IdleBehavior())
            .Build();
    }

    public static CellBuilder NewBuilder() => new CellBuilder();

    public class CellBuilder
    {
        private int cellID = nextCellId++;
        private float innerRadius = 1f;
        private float outerRadius = 2f;
        private Color color = Color.white;
        private float growthRate = 0.5f;
        private float maximumSize = 4f;
        private float maxSpeed = 30f;
        private float maxAcceleration = 30f;
        private float mass = 10f;
        private float drag = 1f;
        private float angularDrag = 0f;
        private Field field = null;
        private ICellBehavior behavior = null;
        private Vector2 position = Vector2.zero;

        public CellBuilder SetPosition(Vector2 p)     { position = p;       return this; }
        public CellBuilder SetInnerRadius(float r)    { innerRadius = r;    return this; }
        public CellBuilder SetOuterRadius(float r)    { outerRadius = r;    return this; }
        public CellBuilder SetColor(Color c)          { color = c;          return this; }
        public CellBuilder SetGrowthRate(float g)     { growthRate = g;     return this; }
        public CellBuilder SetMaximumSize(float m)    { maximumSize = m;    return this; }
        public CellBuilder SetMaxSpeed(float s)       { maxSpeed = s;       return this; }
        public CellBuilder SetMaxAcceleration(float a){ maxAcceleration = a;return this; }
        public CellBuilder SetMass(float m)           { mass = m;           return this; }
        public CellBuilder SetDrag(float d)           { drag = d;           return this; }
        public CellBuilder SetAngularDrag(float a)    { angularDrag = a;    return this; }
        public CellBuilder SetField(Field f)          { field = f;          return this; }
        public CellBuilder SetBehavior(ICellBehavior b){ behavior = b;      return this; }

        public Cell Build()
        {
            var go = new GameObject($"cell_{cellID}");
            go.transform.position = position;
            var cell = go.AddComponent<Cell>();
            cell.cellID      = cellID;
            cell.innerRadius = innerRadius;
            cell.outerRadius = outerRadius;
            cell.color       = color;
            cell.growthRate  = growthRate;
            cell.maximumSize = maximumSize;
            cell.maxSpeed    = maxSpeed;
            cell.maxAcceleration = maxAcceleration;
            cell.behavior    = behavior;
            cell.field       = field;

            var rb2d = go.AddComponent<Rigidbody2D>();
            rb2d.gravityScale = 0f;
            rb2d.mass         = mass;
            rb2d.drag         = drag;
            rb2d.angularDrag  = angularDrag;
            cell.rb = rb2d;

            cell.outerCollider = go.AddComponent<CircleCollider2D>();
            cell.outerCollider.radius    = outerRadius;
            cell.outerCollider.isTrigger = true;

            cell.innerCollider = go.AddComponent<CircleCollider2D>();
            cell.innerCollider.radius    = innerRadius;

            field?.AddCell(cell);
            return cell;
        }
    }
}
