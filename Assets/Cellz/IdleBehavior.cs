using UnityEngine;

/// <summary>
/// A simple "idle" behavior that alternates between having no target
/// (waiting) and picking a random nearby spot to wander toward.
/// When the cell arrives within its outerRadius of that spot,
/// it waits again before picking another target.
///
/// Uses velocity/acceleration logic:
/// - We compute an acceleration step to move the Cell’s velocity toward
///   the desired direction while respecting maxAcceleration and maxSpeed.
/// </summary>
public class IdleBehavior : ICellBehavior
{
    // How long we wait with no target.
    [SerializeField] private float waitTime = 2f;

    // How long we wander before picking another new spot (if we never reach it).
    [SerializeField] private float maxWanderTime = 5f;

    // How far from the cell's current position the random target will be.
    [SerializeField] private float targetRange = 5f;

    // Internal timer controlling the current phase (waiting or wandering).
    private float timer = 0f;

    // Are we currently waiting with no target, or do we have an active target to move toward?
    private bool hasTarget = false;

    // The random spot we decided to move toward.
    private Vector2 targetPos;

    public void PerformBehavior(float deltaTime, Cell cell, Field field)
    {
        // If we are currently waiting, decrement the timer until we pick a new target
        if (!hasTarget)
        {
            timer -= deltaTime;
            if (timer <= 0f)
            {
                // Start wandering
                hasTarget = true;
                timer = maxWanderTime;

                // Pick a random point around our current position
                Vector2 randomOffset = Random.insideUnitCircle * targetRange;
                targetPos = (Vector2)cell.transform.position + randomOffset;
            }
        }
        else
        {
            // We have a target, attempt to move toward it
            Vector2 toTarget = targetPos - (Vector2)cell.transform.position;
            float distToTarget = toTarget.magnitude;

            // If we arrive or run out of wander time, switch to waiting
            timer -= deltaTime;
            if (distToTarget <= cell.outerRadius || timer <= 0f)
            {
                hasTarget = false;
                timer = waitTime;
                return;
            }

            // Otherwise, compute an acceleration step:
            // 1) Desired velocity is maxSpeed in 'toTarget' direction
            Vector2 desiredVelocity = toTarget.normalized * cell.maxSpeed;
            Vector2 currentVelocity = cell.rb.velocity;
            // 2) The velocity difference
            Vector2 deltaV = desiredVelocity - currentVelocity;
            // 3) Limit by maxAcceleration * deltaTime
            float maxDelta = cell.maxAcceleration * deltaTime;
            if (deltaV.magnitude > maxDelta)
            {
                deltaV = deltaV.normalized * maxDelta;
            }
            // 4) Apply it
            cell.rb.velocity += deltaV;

            // 5) Clamp speed to maxSpeed
            if (cell.rb.velocity.magnitude > cell.maxSpeed)
            {
                cell.rb.velocity = cell.rb.velocity.normalized * cell.maxSpeed;
            }
        }
    }
}
