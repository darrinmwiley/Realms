using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents one "cell":
///  - Stores the cell’s radii, color, ID
///  - Holds the rigidbody/collider references
///  - Handles collision damping in OnCollisionEnter2D
///  - Grows over time, capped at a maximumSize
///  - Also stores a reference to an ICellBehavior for AI/steering
///  - Splitting is handled here, so "any place" can call cell.Split().
/// </summary>
public class Cell : MonoBehaviour
{
    [Header("Per-Cell Settings")]
    [Tooltip("A unique ID assigned by the Field script at spawn time.")]
    public int cellID;

    public bool dead;

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

    public HashSet<int> neighborIds = new HashSet<int>();

    void OnTriggerEnter2D(Collider2D other)
    {
        var oc = other.GetComponentInParent<Cell>();
        if (oc != null && oc.cellID != this.cellID)
            neighborIds.Add(oc.cellID);
        Debug.Log("Cell " + cellID + " entered trigger with cell " + oc.cellID);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var oc = other.GetComponentInParent<Cell>();
        if (oc != null)
            neighborIds.Remove(oc.cellID);
            Debug.Log("Cell " + cellID + " exited trigger with cell " + oc.cellID);
    }

    /// <summary>
    /// The assigned behavior that will steer this cell (Idle, Boids, etc.).
    /// </summary>
    [HideInInspector] public ICellBehavior behavior;

    /// <summary>
    /// The field that spawned us, so we can call field.RemoveCell(this) or field.AddCell(child).
    /// </summary>
    [HideInInspector] public Field field;

    /// <summary>
    /// The rigidbody of this cell for movement, forces, etc.
    /// </summary>
    [HideInInspector] public Rigidbody2D rb;

    /// <summary>
    /// The outer collider, used for Voronoi detection & overlap-based repulsion.
    /// </summary>
    [HideInInspector] public CircleCollider2D outerCollider;

    /// <summary>
    /// The inner collider for collisions.
    /// </summary>
    [HideInInspector] public CircleCollider2D innerCollider;

    public void HandleGrowth()
    {
        // Growth logic
        if (outerRadius < maximumSize)
        {
            outerRadius += growthRate * Time.deltaTime;
            if (outerRadius > maximumSize)
                outerRadius = maximumSize;

            innerRadius = outerRadius * 0.5f;
            UpdateColliderSizes();
        }
        // Behavior is not called here. It's invoked in Field.FixedUpdate() for sync with physics.
    }

    /// <summary>
    /// Updates the attached circle collider radii so the physics shapes match our radius values.
    /// </summary>
    private void UpdateColliderSizes()
    {
        if (outerCollider) outerCollider.radius = outerRadius;
        if (innerCollider) innerCollider.radius = innerRadius;
    }

    /// <summary>
    /// Splits this cell into two child cells, then removes itself (destroy).
    /// The logic is fully contained here, so "any place" can call cell.Split().
    /// </summary>
    public Cell[] Split()
    {
        // 1) If the old cell is in a BoidsBehavior or some other shared behavior, un-register it
        if (behavior is BoidsBehavior boidsBeh)
        {
            boidsBeh.UnregisterCell(this);
        }

        // 2) Create two children with half the area => radius = oldRadius / sqrt(2)
        float newOuter = outerRadius / Mathf.Sqrt(2f);
        float newInner = newOuter * 0.5f;

        Vector2 oldPos = transform.position;
        Vector2 offset = innerRadius / 8 * Random.insideUnitCircle.normalized;

        // Child A
        Cell childA = CreateChildCell(
            oldPos + offset,
            newOuter,
            newInner,
            rb.mass,
            color,
            growthRate,
            maximumSize
        );

        // Child B
        Cell childB = CreateChildCell(
            oldPos - offset,
            newOuter,
            newInner,
            rb.mass,
            color,
            growthRate,
            maximumSize
        );

        /* If we had some behavior, let's give it to children too:
        // (Except for boids, we handle that via boidsBeh.UnregisterCell(...) above
        //  and re-register them below. But for non-boids, let's simply copy the same behavior reference.)
        if (!(behavior is BoidsBehavior))
        {
            childA.behavior = new IdleBehavior();
            childB.behavior = new IdleBehavior();
        }

        // If we were boids, re-register them in the same flock
        if (behavior is BoidsBehavior sameFlock)
        {
            sameFlock.OnCellAdded(childA);
            sameFlock.OnCellAdded(childB);
        }*/

        RemoveSelf();

        return new Cell[]{childA, childB};
    }

    public void RemoveSelf()
    {
        if(behavior != null)
        {
            behavior.OnCellDestroyed(this);
        }
        // 3) Remove ourselves from field
        field.RemoveCell(this);
    }

    /// <summary>
    /// Internal helper to create a child Cell GameObject.
    /// Then we call field.AddCell(...) so the Field knows about it.
    /// </summary>
    private Cell CreateChildCell(
        Vector2 position,
        float outerR,
        float innerR,
        float mass,
        Color col,
        float growthRate,
        float maxSize
    )
    {
        return Cell.NewBuilder()
            .SetOuterRadius(outerR)
            .SetInnerRadius(innerR)
            .SetColor(col)
            .SetGrowthRate(growthRate)
            .SetMaximumSize(maxSize)
            .SetMass(mass)
            .SetDrag(rb.drag)
            .SetAngularDrag(0f)
            .SetField(field)
            .SetPosition(position)
            .SetMaxSpeed(maxSpeed)
            .SetMaxAcceleration(maxAcceleration)
            .SetBehavior(new IdleBehavior())
            .Build(); // create the child cell
    }

    /// <summary>
    /// Static entry point to create a new builder for Cell.
    /// Usage:
    ///   var cell = Cell.NewBuilder()
    ///       .SetOuterRadius(4f)
    ///       ...
    ///       .Build();
    /// </summary>
    public static CellBuilder NewBuilder() => new CellBuilder();

    /// <summary>
    /// A fluent builder for creating and configuring a Cell MonoBehaviour.
    /// </summary>
    public class CellBuilder
    {
        // Backing fields for each property
        private int cellID = 0;
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

        private Vector2 position = Vector2.zero; // default position

        public CellBuilder SetPosition(Vector2 pos)
        {
            position = pos;
            return this;
        }

        // -------- Fluent Setters --------

        public CellBuilder SetInnerRadius(float r)
        {
            innerRadius = r;
            return this;
        }
        public CellBuilder SetOuterRadius(float r)
        {
            outerRadius = r;
            return this;
        }
        public CellBuilder SetColor(Color c)
        {
            color = c;
            return this;
        }

        public CellBuilder SetGrowthRate(float rate)
        {
            growthRate = rate;
            return this;
        }
        public CellBuilder SetMaximumSize(float size)
        {
            maximumSize = size;
            return this;
        }
        public CellBuilder SetMaxSpeed(float speed)
        {
            maxSpeed = speed;
            return this;
        }
        public CellBuilder SetMaxAcceleration(float accel)
        {
            maxAcceleration = accel;
            return this;
        }
        public CellBuilder SetMass(float m)
        {
            mass = m;
            return this;
        }
        public CellBuilder SetDrag(float d)
        {
            drag = d;
            return this;
        }
        public CellBuilder SetAngularDrag(float ad)
        {
            angularDrag = ad;
            return this;
        }

        public CellBuilder SetField(Field f)
        {
            field = f;
            return this;
        }
        public CellBuilder SetBehavior(ICellBehavior b)
        {
            behavior = b;
            return this;
        }

        // -------- Build Methods --------

        /// <summary>
        /// Creates a new GameObject, adds a Cell component, and configures it.
        /// </summary>
        public Cell Build()
        {
            GameObject go = new GameObject();
            return Configure(go);
        }

        /// <summary>
        /// Attaches a Cell component to an existing GameObject (if one isn’t already present),
        /// then configures it.
        /// </summary>
        public Cell Build(GameObject existingGameObject)
        {
            return Configure(existingGameObject);
        }

        /// <summary>
        /// The internal method that actually sets up the Cell’s fields and related components.
        /// </summary>
        private Cell Configure(GameObject go)
        {
            go.transform.position = position;

            Cell cell = go.GetComponent<Cell>();
            if (cell == null)
            {
                cell = go.AddComponent<Cell>();
            }

            // 2) Assign basic Cell fields
            cell.cellID = cellID;
            cell.innerRadius = innerRadius;
            cell.outerRadius = outerRadius;
            cell.color = color;
            cell.growthRate = growthRate;
            cell.maximumSize = maximumSize;
            cell.maxSpeed = maxSpeed;
            cell.maxAcceleration = maxAcceleration;
            cell.behavior = behavior;
            cell.field = field;

            // 3) Ensure a Rigidbody2D
            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = go.AddComponent<Rigidbody2D>();
            }
            rb.gravityScale = 0f;
            rb.mass = mass;
            rb.drag = drag;
            rb.angularDrag = angularDrag;
            cell.rb = rb;

            // 4) Outer & inner colliders
            CircleCollider2D outerC = go.AddComponent<CircleCollider2D>();
            outerC.radius = outerRadius;
            outerC.isTrigger = true;
            cell.outerCollider = outerC;

            CircleCollider2D innerC = go.AddComponent<CircleCollider2D>();
            innerC.radius = innerRadius;
            cell.innerCollider = innerC;

            cell.behavior = behavior;

            // 5) Provide a unique ID
            cell.cellID = Cell.nextCellId++;
            cell.gameObject.name = "cell_" + cell.cellID;

            if(field != null)
                field.AddCell(cell);

            return cell;
        }
    }
}
