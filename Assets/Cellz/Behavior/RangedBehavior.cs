// RangedBehavior.cs
using UnityEngine;

/// <summary>
/// A "ranged" behavior that periodically shoots a bullet in a random direction.
/// </summary>
public class RangedBehavior : ICellBehavior
{
    // How often the cell shoots a bullet.
    [SerializeField] private float fireRate = 10f; // Bullets per second

    // Speed of the bullet.
    [SerializeField] private float bulletSpeed = 10.0f;

    // Radius of the bullet.
    [SerializeField] private float bulletRadius = 1f;

    // Color of the bullet.
    [SerializeField] private Color bulletColor = Color.red; // Added bullet color

    // Internal timer to track when to shoot next.
    private float fireTimer = 0f;

    public override void PerformBehavior(float deltaTime, Cell cell, Field field)
    {
        fireTimer -= deltaTime;

        if (fireTimer <= 0f)
        {
            // Reset timer
            fireTimer = fireRate;

            // Determine a random direction
            Vector2 randomDirection = Random.insideUnitCircle.normalized;

            // Calculate bullet spawn position slightly outside the cell's outer radius
            Vector2 spawnPosition = (Vector2)cell.transform.position + randomDirection * (cell.outerRadius + bulletRadius + 0.1f);

            // Create and launch a bullet
            field.AddBullet(spawnPosition, randomDirection * bulletSpeed, bulletRadius, bulletColor); // Pass bulletColor
        }
    }
}