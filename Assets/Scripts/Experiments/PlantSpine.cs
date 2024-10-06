using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantSpine : MonoBehaviour
{
    public GameObject root; // Root of the plant
    public Vector3 initialDirection = Vector3.up; // Direction of the plant's growth (upward)
    public float directionVariance = 15f; // Variance in direction for randomness
    public float flexibility = 45f;
    public float strength = 10f;
    public float magnitude = 2f;
    public float growTime = 3f;

    private List<Growth> growths = new List<Growth>();

    void Start()
    {
        // Initialize the first Growth
        Vector3 initialGrowthDirection = initialDirection + GetRandomVariance();
        Growth initialGrowth = new Growth(root, initialGrowthDirection, true, flexibility, strength, magnitude, growTime);
        growths.Add(initialGrowth);
    }

    void Update()
    {
        // Check if we have a growth chain
        if (growths.Count > 0)
        {
            // Get the topmost growth
            Growth topGrowth = growths[growths.Count - 1];

            // If the top growth is growing, update it
            float growthProgress = (Time.time - topGrowth.growStartTime) / growTime;
            topGrowth.SetGrowth(growthProgress);

            // If the top growth is fully grown, add a new Growth segment
            if (growthProgress >= 1f)
            {
                // Add a new growth, anchored to the previous grow joint
                AddNewGrowth(topGrowth.growJoint);
            }
        }
    }

    // Method to add a new Growth to the chain
    private void AddNewGrowth(GameObject previousGrowJoint)
    {
        // Calculate the direction for the new Growth (with some variance)
        Vector3 newGrowthDirection = initialDirection + GetRandomVariance();

        // Create a new Growth segment, anchored to the growJoint of the previous growth
        Growth newGrowth = new Growth(previousGrowJoint, newGrowthDirection, true, flexibility, strength, magnitude, growTime);
        newGrowth.growing = true; // Set the new growth to start growing
        newGrowth.growStartTime = Time.time; // Record when the new growth starts

        // Add the new Growth to the list
        growths.Add(newGrowth);
    }

    // Method to introduce randomness in growth direction
    private Vector3 GetRandomVariance()
    {
        // Randomly alter the growth direction within the variance range
        float randomYaw = Random.Range(-directionVariance, directionVariance);
        float randomPitch = Random.Range(-directionVariance, directionVariance);

        Quaternion varianceRotation = Quaternion.Euler(randomPitch, randomYaw, 0);
        return varianceRotation * initialDirection;
    }
}