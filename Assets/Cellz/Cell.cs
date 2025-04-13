using UnityEngine;

/// <summary>
/// Represents one "cell":
///  - Stores the cell’s radii, color, ID
///  - Holds the rigidbody/collider references
///  - Handles collision damping in OnCollisionEnter2D
///  - Grows over time, capped at a maximumSize
///  - Now also stores a reference to an ICellBehavior for AI/steering
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Cell : MonoBehaviour
{
    [Header("Per-Cell Settings")]
    [Tooltip("A unique ID assigned by the Field script at spawn time.")]
    public int cellID;

    [Tooltip("Inner radius used for collision & darker color.")]
    public float innerRadius;

    [Tooltip("Outer radius used for Voronoi detection & color influence.")]
    public float outerRadius;

    [Tooltip("Base color of this cell.")]
    public Color color;

    [Header("Collision Damping")]
    [Tooltip("Velocity is multiplied by this factor upon collision (0 = stop dead, 1 = no slowdown).")]
    public float collisionDampFactor = 0.8f;

    [Header("Growth Settings")]
    [Tooltip("How fast the cell’s outer radius grows per second.")]
    public float growthRate = 0.5f;

    [Tooltip("Maximum outer radius allowed.")]
    public float maximumSize = 4f;

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

    /// <summary>
    /// The assigned behavior that will steer this cell (Idle, Flee, etc.).
    /// </summary>
    [HideInInspector] public ICellBehavior behavior;

    private void Awake()
    {
        // Grab the Rigidbody2D (which is required by [RequireComponent])
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // GROWTH LOGIC (unchanged)
        if (outerRadius < maximumSize)
        {
            outerRadius += growthRate * Time.deltaTime;
            if (outerRadius > maximumSize)
                outerRadius = maximumSize;

            // Example: keep the inner radius half of outer
            innerRadius = outerRadius * 0.5f;
            UpdateColliderSizes();
        }

        // We do NOT call behavior here, because we plan to do it in Field.FixedUpdate()
        // so that we can apply forces in sync with the physics engine.
    }

    /// <summary>
    /// Updates the attached circle collider radii so the physics shapes match our radius values.
    /// </summary>
    private void UpdateColliderSizes()
    {
        if (outerCollider != null)
        {
            outerCollider.radius = outerRadius;
        }
        if (innerCollider != null)
        {
            innerCollider.radius = innerRadius;
        }
    }

    /// <summary>
    /// Called when another collider hits our inner collider. We damp velocities for collision behavior.
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (rb != null)
        {
            rb.velocity *= collisionDampFactor;

            Rigidbody2D otherRb = collision.rigidbody;
            if (otherRb != null)
            {
                otherRb.velocity *= collisionDampFactor;
            }
        }
    }
}
