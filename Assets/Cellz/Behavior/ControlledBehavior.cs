using UnityEngine;

/// <summary>
/// A behavior that lets the user control the cell with WASD/arrow keys.
/// Movement is velocity-based, respecting cell.maxSpeed and cell.maxAcceleration.
/// </summary>
public class ControlledBehavior : ICellBehavior
{
    public override void PerformBehavior(float deltaTime, Cell cell, Field field)
    {
        // Gather user input
        Vector2 input = Vector2.zero;
        // Use arrow keys or WASD. Feel free to expand as needed:
        if (Input.GetKey(KeyCode.UpArrow))    input += Vector2.up;
        if (Input.GetKey(KeyCode.DownArrow))  input += Vector2.down;
        if (Input.GetKey(KeyCode.LeftArrow))  input += Vector2.left;
        if (Input.GetKey(KeyCode.RightArrow)) input += Vector2.right;

        if (input.sqrMagnitude > 0.001f)
        {
            // 1) Desired velocity is in input direction * maxSpeed
            Vector2 desiredVelocity = input.normalized * cell.maxSpeed;
            // 2) Current velocity
            Vector2 currentVelocity = cell.rb.velocity;
            // 3) The needed change
            Vector2 deltaV = desiredVelocity - currentVelocity;
            // 4) Limit by maxAcceleration * deltaTime
            float maxDelta = cell.maxAcceleration * deltaTime;
            if (deltaV.magnitude > maxDelta)
            {
                deltaV = deltaV.normalized * maxDelta;
            }
            // 5) Apply
            cell.rb.velocity += deltaV;

            // 6) Clamp final speed
            if (cell.rb.velocity.magnitude > cell.maxSpeed)
            {
                cell.rb.velocity = cell.rb.velocity.normalized * cell.maxSpeed;
            }
        }
    }
}
