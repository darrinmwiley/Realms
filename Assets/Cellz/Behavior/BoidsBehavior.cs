using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A single BoidsBehavior instance manages an entire flock of cells.
/// All cells share the same behavior reference, so we store them in flockCells.
/// - Once a cell grows beyond boidMaxSize, it calls cell.Split() by itself.
/// - The splitting is done in Cell.cs, which eventually calls Field.OnCellSplit().
/// - We'll configure the new children ourselves in OnCellAdded(...).
/// </summary>
public class BoidsBehavior : ICellBehavior
{
    // A list of all cells belonging to this flock
    private List<Cell> flockCells = new List<Cell>();

    // Maximum flock size (if you don't want more than 30 from splits)
    private const int MAX_FLOCK_SIZE = 8;

    // The maximum radius before a cell tries to split itself
    private float boidMaxSize = 1.2f;

    // Boids parameters
    private float neighborRadius     = 5f; // how far we look for neighbors
    private float separationDistance = 1f; // desired separation distance
    private float separationWeight   = 1.5f;
    private float alignmentWeight    = 1.0f;
    private float cohesionWeight     = 1.0f;

    /// <summary>
    /// Call this for each cell you want in the flock.
    /// This sets c.behavior to this, and adds it to our list.
    /// </summary>
    public void RegisterCell(Cell c)
    {
        flockCells.Add(c);
        c.behavior = this;
    }

    /// <summary>
    /// Remove a cell from the flock.
    /// Called after a cell is destroyed or changes to a different behavior.
    /// </summary>
    public void UnregisterCell(Cell c)
    {
        flockCells.Remove(c);
    }

    public override void OnCellDestroyed(Cell c)
    {
        UnregisterCell(c);
    }

    /// <summary>
    /// The main boids logic. Called once per physics tick for each Cell in the flock.
    /// </summary>
    public override void PerformBehavior(float deltaTime, Cell cell, Field field)
    {
        // 1) If we've grown too large, try splitting, up to MAX_FLOCK_SIZE
        if (cell.outerRadius >= boidMaxSize && flockCells.Count < MAX_FLOCK_SIZE)
        {
            // The actual splitting code is inside Cell, so we just call it.
            Cell[] children = cell.Split();
            foreach (Cell child in children)
            {
                RegisterCell(child); // Add new children to the flock
                child.behavior = this; // Set their behavior to this one
            }
            return; // This cell is effectively done for this frame.
        }

        // 2) Gather neighbors from our flockCells (not from Field)
        List<Cell> neighbors = new List<Cell>();
        Vector2 myPos = cell.transform.position;

        foreach (Cell other in flockCells)
        {
            if (other == cell) continue; // skip self
            float dist = Vector2.Distance(myPos, other.transform.position);
            if (dist < neighborRadius)
            {
                neighbors.Add(other);
            }
        }

        // If no neighbors, no boids steering
        if (neighbors.Count == 0) return;

        // 3) Compute separation / alignment / cohesion
        Vector2 separation = ComputeSeparation(cell, neighbors);
        Vector2 alignment  = ComputeAlignment(cell, neighbors);
        Vector2 cohesion   = ComputeCohesion(cell, neighbors);

        Vector2 steering = separation * separationWeight
                         + alignment  * alignmentWeight
                         + cohesion   * cohesionWeight;

        // 4) Velocity-based approach: we want currentVelocity + steering,
        //    respecting maxAcceleration and maxSpeed
        Vector2 currentVel = cell.rb.velocity;
        Vector2 desiredVel = currentVel + steering;
        Vector2 deltaV     = desiredVel - currentVel;

        float maxDelta = cell.maxAcceleration * deltaTime;
        if (deltaV.magnitude > maxDelta)
        {
            deltaV = deltaV.normalized * maxDelta;
        }

        cell.rb.velocity += deltaV;

        // Clamp final speed
        if (cell.rb.velocity.magnitude > cell.maxSpeed)
        {
            cell.rb.velocity = cell.rb.velocity.normalized * cell.maxSpeed;
        }
    }

    private Vector2 ComputeSeparation(Cell me, List<Cell> neighbors)
    {
        Vector2 force = Vector2.zero;
        Vector2 myPos = me.transform.position;
        foreach (Cell n in neighbors)
        {
            Vector2 delta = (Vector2)n.transform.position - myPos;
            float dist = delta.magnitude;
            if (dist < separationDistance && dist > 0.0001f)
            {
                // Invert direction, scale by 1/dist
                force -= delta.normalized / dist;
            }
        }
        return force;
    }

    private Vector2 ComputeAlignment(Cell me, List<Cell> neighbors)
    {
        Vector2 avgVelocity = Vector2.zero;
        if (neighbors.Count == 0) return avgVelocity;

        foreach (Cell n in neighbors)
        {
            avgVelocity += n.rb.velocity;
        }
        avgVelocity /= neighbors.Count;
        return (avgVelocity - me.rb.velocity) * 0.05f; // small factor
    }

    private Vector2 ComputeCohesion(Cell me, List<Cell> neighbors)
    {
        Vector2 avgPos = Vector2.zero;
        if (neighbors.Count == 0) return avgPos;

        foreach (Cell n in neighbors)
        {
            avgPos += (Vector2)n.transform.position;
        }
        avgPos /= neighbors.Count;

        Vector2 toCenter = avgPos - (Vector2)me.transform.position;
        return toCenter * 0.01f;
    }
}
