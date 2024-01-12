using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    public Transform player; // The player's transform
    public float distance = 10f; // Distance from the player
    public float height = 5f; // Height above the player

    private float currentAngle = 45f;
    private Camera mainCamera;

    void Start()
    {
        // Get the main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found in the scene.");
        }
    }

    void Update()
    {
        if (player == null || mainCamera == null) return;

        // Rotate camera with 'Q' or 'R'
        if (Input.GetKeyDown(KeyCode.Q))
        {
            RotateCamera(45); // Rotate counterclockwise
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            RotateCamera(-45); // Rotate clockwise
        }

        // Update camera position and rotation
        UpdateCameraPosition();
    }

    private void RotateCamera(float angle)
    {
        currentAngle += angle;
        currentAngle %= 360; // Keep the angle within 0-360 degrees
    }

    private void UpdateCameraPosition()
    {
        // Calculate the new position
        Vector3 offset = Quaternion.Euler(0, currentAngle, 0) * new Vector3(0, height, -distance);
        mainCamera.transform.position = player.position + offset;

        // Look at the player
        mainCamera.transform.LookAt(player.position);
    }
}
