using UnityEngine;
using System.Collections.Generic;

public class BoidsBehavior : ICellBehavior
{
    private List<Cell> flockCells = new List<Cell>();

    private const int MAX_FLOCK_SIZE = 16;

    // -------------------------------------------------
    // Boids parameters
    private float separationFactor = 2f;  
    private float separationWeight = 1.5f; 
    private float alignmentWeight  = 1.0f;
    private float cohesionWeight   = 1.0f;

    // -------------------------------------------------
    // Goal / Pause logic
    private Vector2 flockGoal = Vector2.zero; 
    private float   goalWeight = 0.8f;     
    private float   goalRadius = 20f;     
    private float   goalThreshold = 2f;   
    private float   pauseDuration = 2f;   

    private enum GoalState { Moving, Pausing }
    private GoalState goalState = GoalState.Moving;
    private float pauseEndTime = 0f;

    // -------------------------------------------------
    // Predator avoidance (debug flag)
    [Tooltip("If enabled, cells will try to avoid 'large' neighbors from the entire Field.")]
    public bool enablePredatorAvoidance = true;
    private float predatorWeight     = 1.5f;
    private float predatorSizeFactor = 1.5f; 
    private float predatorRangeFactor = 5f;

    // -------------------------------------------------
    // TIME-BASED SPLITTING
    private Dictionary<Cell, float> scheduledSplitTimes = new Dictionary<Cell, float>();
    // Original scheduling window for the first time (0..10s)
    private float splitScheduleWindow = 10f;
    // If we can't split because the flock is at capacity, we push out the schedule by e.g. 1..3s
    private Vector2 rescheduleRange = new Vector2(15f, 30f);

    public void RegisterCell(Cell c)
    {
        flockCells.Add(c);
        c.behavior = this;
    }

    public void UnregisterCell(Cell c)
    {
        flockCells.Remove(c);
        if (scheduledSplitTimes.ContainsKey(c))
        {
            scheduledSplitTimes.Remove(c);
        }
    }

    public override void OnCellDestroyed(Cell c)
    {
        UnregisterCell(c);
    }

    public override void PerformBehavior(float deltaTime, Cell cell, Field field)
    {
        // 0) Check if this cell is inside a predator => EATEN
        if (CheckAndHandleEaten(cell))
        {
            return; // cell is destroyed
        }

        // 1) Check if we should schedule or trigger a time-based split
        HandleTimeBasedSplitting(cell);

        // 2) Leader updates goal state
        if (IsLeader(cell))
        {
            UpdateGoalState();
        }

        // 3) Boids forces
        Vector2 separation = ComputeSeparation(cell);
        Vector2 alignment  = ComputeAlignment(cell);
        Vector2 cohesion   = ComputeCohesion(cell);

        Vector2 goalForce  = (goalState == GoalState.Moving) ? ComputeGoalForce(cell) : Vector2.zero;
        Vector2 predatorPush = Vector2.zero;
        if (enablePredatorAvoidance)
        {
            predatorPush = ComputePredatorAvoidance(cell) * predatorWeight;
        }

        Vector2 steering = separation * separationWeight
                         + alignment  * alignmentWeight
                         + cohesion   * cohesionWeight
                         + goalForce  * goalWeight
                         + predatorPush;

        // 4) Apply steering within max accel / speed
        Vector2 currentVel = cell.rb.velocity;
        Vector2 desiredVel = currentVel + steering;
        Vector2 deltaV     = desiredVel - currentVel;

        float maxDelta = cell.maxAcceleration * deltaTime;
        if (deltaV.magnitude > maxDelta)
        {
            deltaV = deltaV.normalized * maxDelta;
        }

        cell.rb.velocity += deltaV;
        if (cell.rb.velocity.magnitude > cell.maxSpeed)
        {
            cell.rb.velocity = cell.rb.velocity.normalized * cell.maxSpeed;
        }
    }

    /// <summary>
    /// If this cell is inside any predator's outer radius, we destroy it and return true.
    /// Otherwise return false.
    /// </summary>
    private bool CheckAndHandleEaten(Cell me)
    {
        if (me == null || me.field == null) return false;

        List<Cell> allCells = me.field.GetAllCells();
        foreach (Cell other in allCells)
        {
            if (other == me) continue;
            // Is 'other' a predator?
            if (other.outerRadius >= me.outerRadius * predatorSizeFactor)
            {
                float dist = Vector2.Distance(other.transform.position, me.transform.position);
                if (dist < other.outerRadius)
                {
                    // EATEN
                    me.DestroySelf();
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// If a cell is fully grown, schedule a random time in [now..now+10].
    /// If that time arrives & the flock is under capacity => split.
    /// Otherwise we reschedule for [now..now+3].
    /// </summary>
    private void HandleTimeBasedSplitting(Cell cell)
    {
        bool isFullyGrown = (cell.outerRadius >= cell.maximumSize);

        if (!isFullyGrown)
        {
            // Cancel any existing schedule
            if (scheduledSplitTimes.ContainsKey(cell))
            {
                scheduledSplitTimes.Remove(cell);
            }
            return;
        }

        // If fully grown, see if we need to create or check a schedule
        if (!scheduledSplitTimes.ContainsKey(cell))
        {
            float offset = Random.Range(0f, splitScheduleWindow);
            scheduledSplitTimes[cell] = Time.time + offset;
        }
        else
        {
            float scheduledTime = scheduledSplitTimes[cell];
            if (Time.time >= scheduledTime)
            {
                // Time to attempt a split
                if (flockCells.Count < MAX_FLOCK_SIZE)
                {
                    // Actually split
                    Cell[] children = cell.Split();
                    if (children != null)
                    {
                        foreach (Cell child in children)
                        {
                            RegisterCell(child);
                        }
                    }
                }
                else
                {
                    // Reschedule since the flock is full
                    float extra = Random.Range(rescheduleRange.x, rescheduleRange.y);
                    scheduledSplitTimes[cell] = Time.time + extra;
                }
            }
        }
    }

    // -------------------------------------------------
    // Boids sub-behaviors

    private bool IsLeader(Cell c)
    {
        return (flockCells.Count > 0 && flockCells[0] == c);
    }

    private void UpdateGoalState()
    {
        switch (goalState)
        {
            case GoalState.Moving:
            {
                Vector2 center = ComputeFlockCenter();
                float dist = Vector2.Distance(center, flockGoal);
                if (dist <= goalThreshold)
                {
                    pauseEndTime = Time.time + pauseDuration;
                    goalState = GoalState.Pausing;
                }
                break;
            }
            case GoalState.Pausing:
            {
                if (Time.time >= pauseEndTime)
                {
                    PickNewFlockGoal();
                    goalState = GoalState.Moving;
                }
                break;
            }
        }
    }

    private void PickNewFlockGoal()
    {
        Vector2 dir = Random.insideUnitCircle.normalized;
        float dist  = Random.Range(goalRadius * 0.3f, goalRadius);
        flockGoal   = dir * dist;
    }

    private Vector2 ComputeFlockCenter()
    {
        if (flockCells.Count == 0) 
            return Vector2.zero;

        Vector2 sum = Vector2.zero;
        foreach (Cell c in flockCells)
        {
            sum += (Vector2)c.transform.position;
        }
        return sum / flockCells.Count;
    }

    private Vector2 ComputeSeparation(Cell me)
    {
        Vector2 force = Vector2.zero;
        Vector2 myPos = me.transform.position;

        foreach (Cell neighbor in flockCells)
        {
            if (neighbor == me) continue;
            Vector2 delta = neighbor.transform.position - (Vector3)myPos;
            float dist    = delta.magnitude;

            float combinedRadii = (me.outerRadius + neighbor.outerRadius) * separationFactor;
            if (dist < combinedRadii && dist > 0.0001f)
            {
                float overlap = (combinedRadii - dist) / combinedRadii;
                force -= delta.normalized * overlap;
            }
        }
        return force;
    }

    private Vector2 ComputeAlignment(Cell me)
    {
        if (flockCells.Count <= 1) return Vector2.zero;

        Vector2 avgVelocity = Vector2.zero;
        foreach (Cell neighbor in flockCells)
        {
            avgVelocity += neighbor.rb.velocity;
        }
        avgVelocity /= flockCells.Count;

        return (avgVelocity - me.rb.velocity) * 0.05f;
    }

    private Vector2 ComputeCohesion(Cell me)
    {
        if (flockCells.Count <= 1) return Vector2.zero;

        Vector2 avgPos = Vector2.zero;
        foreach (Cell neighbor in flockCells)
        {
            avgPos += (Vector2)neighbor.transform.position;
        }
        avgPos /= flockCells.Count;

        Vector2 toCenter = avgPos - (Vector2)me.transform.position;
        return toCenter * 0.01f;
    }

    private Vector2 ComputePredatorAvoidance(Cell me)
    {
        Vector2 force = Vector2.zero;
        Vector2 myPos = me.transform.position;

        if (me.field == null) return force;

        List<Cell> allCells = me.field.GetAllCells(); 
        foreach (Cell other in allCells)
        {
            if (other == me) continue;

            if (other.outerRadius >= me.outerRadius * predatorSizeFactor)
            {
                Vector2 delta = other.transform.position - (Vector3)myPos;
                float dist = delta.magnitude;

                float threatRange = (other.outerRadius + me.outerRadius) * predatorRangeFactor;
                if (dist < threatRange && dist > 0.0001f)
                {
                    float overlap = (threatRange - dist) / threatRange;
                    force -= delta.normalized * overlap;
                }
            }
        }
        return force;
    }

    private Vector2 ComputeGoalForce(Cell me)
    {
        Vector2 toGoal = flockGoal - (Vector2)me.transform.position;
        return toGoal.normalized * 0.5f;
    }
}
