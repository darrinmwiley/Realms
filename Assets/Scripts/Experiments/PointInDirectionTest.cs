using UnityEngine;

public class PointInDirectionTest : MonoBehaviour
{
    // Public variable to set the target position in the Inspector
    public Vector3 targetPosition;   // The target position that the object's Z-axis should point towards

    void Update()
    {
        // Call the method to rotate the Z-axis to face the target instantly
        RotateTowards(targetPosition);
    }

    // Method to rotate the object so its Z-axis points at the target instantly
    public void RotateTowards(Vector3 targetPosition)
    {
        // Calculate the direction from the object's position to the target position
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;

        // Use Quaternion.LookRotation to align the forward direction (Z-axis) to the target
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, Vector3.up);

        // Set the new rotation instantly
        transform.rotation = targetRotation * Quaternion.Euler(90f, 0f, 0f);;
    }
}
