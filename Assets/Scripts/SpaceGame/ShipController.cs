using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ShipController : MonoBehaviour
{
    public float speed = 5f;              // Base speed of the ship
    public float acceleration = 2f;       // Rate of acceleration
    public float deceleration = 2f;       // Rate of deceleration
    public float brakingSpeed = 5f;       // Rate of braking when right-clicking
    public float topSpeed = 10f;          // Maximum speed the ship can reach
    public float rotationSpeed = 200f;    // Speed at which the ship rotates toward the mouse
    public float collisionLockDuration = 1f; // Duration to lock out controls after collision

    private float currentSpeed = 0f;      // Tracks the current speed of the ship
    private Rigidbody2D rb;               // Reference to the Rigidbody2D component
    private float lockoutTimer = 0f;      // Timer to track the lockout duration
    private bool isLockedOut = false;     // Whether the ship is in lockout mode

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); // Initialize the Rigidbody2D reference
        rb.gravityScale = 0;              // Ensure no gravity affects the ship
    }

    void FixedUpdate()
    {
        //if (!isLockedOut)
        //{
            HandleMovement();
        //}
        //else
        //{
        //    // Count down the lockout timer
        //    lockoutTimer -= Time.fixedDeltaTime;
        //    if (lockoutTimer <= 0f)
         //   {
       //         isLockedOut = false; // Re-enable controls when timer reaches 0
        //    }
       // }
    }

    void HandleMovement()
    {
        // Handle rotation first to ensure ship rotates toward the mouse before moving
        HandleRotation();

        // Accelerate, brake, or decelerate based on input
        if (Input.GetMouseButton(1)) // Right mouse button for braking
        {
            // Apply a braking force opposite to the current velocity
            Vector2 brakingForce = -rb.velocity.normalized * brakingSpeed;
            rb.AddForce(brakingForce);
        }
        else if (Input.GetMouseButton(0)) // Left mouse button for accelerating
        {
            // Accelerate forward up to top speed
            Vector2 forwardForce = transform.up * acceleration;
            rb.AddForce(forwardForce);
            if(rb.velocity.magnitude > topSpeed)
            {
                rb.velocity = rb.velocity.normalized * topSpeed;
            }
        }
        else
        {
            // Decelerate naturally if no input is pressed
            Vector2 decelerationForce = -rb.velocity.normalized * deceleration;
            rb.AddForce(decelerationForce);
        }

        // Clamp speed to prevent it from exceeding the top speed
        if (rb.velocity.magnitude > topSpeed)
        {
            rb.velocity = rb.velocity.normalized * topSpeed;
        }
    }

    void HandleRotation()
    {
        // Get the mouse position in world space
        Vector3 mousePosition = Input.mousePosition;
        mousePosition = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, Camera.main.nearClipPlane));

        // Calculate the direction from the ship to the mouse
        Vector2 direction = (mousePosition - transform.position).normalized;

        // Calculate the target rotation angle
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        // Smoothly rotate the ship towards the target angle using MoveRotation
        float newAngle = Mathf.LerpAngle(rb.rotation, targetAngle, rotationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(newAngle);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the object collided with has the "Asteroid" tag
        if (collision.gameObject.CompareTag("Asteroid"))
        {
            // Start the lockout
            isLockedOut = true;
            lockoutTimer = collisionLockDuration;
            currentSpeed = 0f; // Reset speed during lockout
            rb.velocity = Vector2.zero; // Stop movement
        }
    }
}
