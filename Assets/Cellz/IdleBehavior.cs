using UnityEngine;

/// <summary>
/// A simple "idle" behavior that alternates between having no target
/// (waiting) and picking a random nearby spot to wander toward.
/// When the cell arrives within its outerRadius of that spot,
/// it waits again before picking another target.
/// </summary>
public class IdleBehavior : ICellBehavior
{
    // How long we wait with no target.
    [SerializeField] private float waitTime = 2f;

    // How long we wander before picking another new spot (if we never reach it).
    [SerializeField] private float maxWanderTime = 5f;

    // How far from the cell's current position the random target will be.
    [SerializeField] private float targetRange = 20f;

    // How forcefully we move toward the target.
    [SerializeField] private float moveForce = 200f;

    // Track how much time we have left in the current phase (waiting or wandering).
    private float timer = 0f;

    // Are we currently waiting with no target, or do we have an active target to move toward?
    private bool hasTarget = false;

    // The random spot we decided to move toward.
    private Vector2 targetPos;

    public void PerformBehavior(float deltaTime, Cell cell, Field field)
    {
        // Phase 1: If we currently do not have a target, we are "waiting."
        if (!hasTarget)
        {
            timer -= deltaTime;
            if (timer <= 0f)
            {
                // Time to pick a new target and start wandering
                hasTarget = true;
                timer = maxWanderTime; // We'll wander up to this long

                // Pick a random point around our current position
                Vector2 randomOffset = Random.insideUnitCircle * targetRange;
                targetPos = (Vector2)cell.transform.position + randomOffset;
            }
        }
        // Phase 2: We have a target, try to move toward it
        else
        {
            // Gently steer the cell toward the targetPos
            Vector2 dir = targetPos - (Vector2)cell.transform.position;

            // Check if we've arrived (within the cell's outerRadius)
            // or we've been wandering too long
            bool arrived = (dir.magnitude <= cell.outerRadius);
            timer -= deltaTime;
            if (arrived || timer <= 0f)
            {
                // Switch back to waiting, no target
                hasTarget = false;
                timer = waitTime;
                return; // We'll stop moving this frame
            }

            // Otherwise, move if not arrived
            if (dir.sqrMagnitude > 0.01f)
            {
                dir.Normalize();
                cell.rb.AddForce(dir * moveForce);
            }
        }
    }
}
