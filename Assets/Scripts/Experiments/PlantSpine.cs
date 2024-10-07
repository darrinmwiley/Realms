using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantSpine : MonoBehaviour
{
    public GameObject root; // Root of the plant
    public Vector3 initialDirection = Vector3.up; // Direction of the plant's growth (upward)
    public float directionVariance = 15f; // Variance in direction for randomness
    public float flexibility = 15f;
    public float strength = 50f;
    public float magnitude = 2f;

    // Lists to track growth times and start times for each Growth segment
    public List<float> growTimes = new List<float>();
    public List<float> growStartTimes = new List<float>();

    private List<Growth> growths = new List<Growth>();

    void Start()
    {
        // Initialize the first Growth
        Vector3 initialGrowthDirection = initialDirection + GetRandomVariance();
        Growth initialGrowth = new Growth(root, initialGrowthDirection, true, flexibility, strength, magnitude);

        // Set the initial grow time and start time
        growths.Add(initialGrowth);
        growTimes.Add(3f); // You can set a default grow time here
        growStartTimes.Add(Time.time); // Record when the growth starts
    }

    void Update()
    {
        // Check if we have a growth chain
        if (growths.Count > 0)
        {
            // Get the topmost growth
            Growth topGrowth = growths[growths.Count - 1];
            int topGrowthIndex = growths.Count - 1;

            // Calculate growth progress based on the time elapsed since growStartTime
            float growthProgress = (Time.time - growStartTimes[topGrowthIndex]) / growTimes[topGrowthIndex];
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
        Growth newGrowth = new Growth(previousGrowJoint, newGrowthDirection, true, flexibility, strength, magnitude);
        
        // Set the new growth to start growing and add its time properties
        growths.Add(newGrowth);
        growTimes.Add(3f); // Assign the grow time for the new growth
        growStartTimes.Add(Time.time); // Record when the new growth starts
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
