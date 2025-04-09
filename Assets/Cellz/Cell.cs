using UnityEngine;

/// <summary>
/// Represents one "cell" (formerly called a circle):
///  - Stores the cell’s radii, color, ID
///  - Holds the rigidbody/collider references
///  - Handles collision damping in OnCollisionEnter2D
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

    /// <summary>
    /// The rigidbody of this cell for movement, forces, etc.
    /// </summary>
    [HideInInspector] public Rigidbody2D rb;

    /// <summary>
    /// The outer collider, used for Voronoi detection & overlap-based repulsion.
    /// </summary>
    [HideInInspector] public CircleCollider2D outerCollider;

    private void Awake()
    {
        // Grab the Rigidbody2D (which is required by [RequireComponent])
        rb = GetComponent<Rigidbody2D>();
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
