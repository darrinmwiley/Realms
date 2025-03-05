using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser : MonoBehaviour
{
    public GameObject ship;
    public GameObject dualLaser;

    public Transform transform;
    public float laserSpeed = 20f; // Set the speed of the laser
    public string laserLayer = "Laser"; // Define the layer to avoid collisions

    void Start()
    {
        // Ensure that the layer is properly set in the Unity editor collision matrix
        if (LayerMask.NameToLayer(laserLayer) == -1)
        {
            Debug.LogWarning($"Layer '{laserLayer}' not found. Please create it in Unity's Layers settings.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Check if the space bar is pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ShootLaser();
        }
    }

    void ShootLaser()
    {
        // Instantiate the laser at the ship's position and rotation
        GameObject laserInstance = Instantiate(dualLaser, ship.transform.position, ship.transform.rotation);

        // Set the laser instance layer to avoid collisions
        laserInstance.layer = LayerMask.NameToLayer(laserLayer);

        // Ensure the laser position and rotation match the ship's orientation
        laserInstance.transform.position = transform.position;
        laserInstance.transform.rotation = transform.rotation;

        // Add a Rigidbody component if it does not already have one
        Rigidbody rb = laserInstance.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = laserInstance.AddComponent<Rigidbody>();
        }

        // Set Rigidbody properties
        rb.useGravity = false;       // Prevents gravity from affecting the laser
        rb.isKinematic = false;      // Allows setting velocity

        // Set the laser's velocity in the forward direction of the ship
        rb.velocity = ship.transform.up * laserSpeed;
    }
}
