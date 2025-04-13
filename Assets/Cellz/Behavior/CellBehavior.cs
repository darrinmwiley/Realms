using UnityEngine;

/// <summary>
/// Common interface that all cell behaviors implement.
/// Each behavior decides how to move/steer the cell each tick.
/// </summary>
public abstract class ICellBehavior
{
    /// <param name="deltaTime">Time step (usually Time.fixedDeltaTime).</param>
    /// <param name="cell">The Cell we are controlling.</param>
    /// <param name="field">Reference to the overall Field for context if needed.</param>
    public abstract void PerformBehavior(float deltaTime, Cell cell, Field field);

    public virtual void OnCellDestroyed(Cell c){}
}
