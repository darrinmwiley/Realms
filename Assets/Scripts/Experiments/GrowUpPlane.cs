using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GrowUpPlane : MonoBehaviour
{
    public GameObject root; // Root of the plant
    public GameObject plane; // Reference to the plane for the plant to grow up
    public int numPoints = 10; // Number of points to create along the spline
    public int numSegments = 1; // Number of segments
    public bool testing = true; // If true, create spheres at control points
    public Material segmentMaterial; // Material for the segments
    public float growthTime = 3f; // Time for each segment to grow

    private List<SegmentControlPoints> segments = new List<SegmentControlPoints>();
    private List<float> segmentGrowthStartTimes = new List<float>(); // To track when each segment starts growing
    private bool isGrowing = true;

    public void Start()
    {
        // Get the list of points
        List<Vector3> points = GeneratePointsOnPlane(plane, numPoints);

        // Split points into segments and create SegmentControlPoints objects
        CreateSegmentsFromPoints(points);
    }

    // Method to generate points along the plane
    private List<Vector3> GeneratePointsOnPlane(GameObject plane, int numPoints)
    {
        List<Vector3> controlPoints = new List<Vector3>();

        // The base starting point at x = 0, z = -5
        float lastX = 0f;

        for (int i = 1; i <= numPoints; i++)
        {
            // Adjust x position based on the last point with some variance
            float xDeviation = UnityEngine.Random.Range(-1f, 1f); // Reduced for smoother variance
            float newX = Mathf.Clamp(lastX + xDeviation, -5f, 5f);

            // Increment z position from -5 to 5
            float zPosition = Mathf.Lerp(-5f, 5f, (float)i / numPoints);

            // Construct the new point
            Vector3 newPoint = new Vector3(newX, 0, zPosition);
            lastX = newX; // Update lastX to the new x value

            // Transform the point into the plane's local space
            controlPoints.Add(newPoint);

            // For testing purposes, create tiny spheres at control points
            if (testing)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = plane.transform.TransformPoint(newPoint);
                sphere.transform.localScale = Vector3.one * 0.1f; // Tiny spheres
                sphere.GetComponent<Collider>().enabled = false; // Disable colliders for visual only
            }
        }

        return controlPoints;
    }

    // Method to split points into segments and create SegmentControlPoints objects
    private void CreateSegmentsFromPoints(List<Vector3> points)
    {
        int pointsPerSegment = Mathf.CeilToInt((float)points.Count / numSegments);
        int start = 0;
        int end = Mathf.Min(start + pointsPerSegment, points.Count);

        List<Vector3> segmentPoints = points.GetRange(start, end - start);

        List<Vector3> transformedPoints = new List<Vector3>();
        foreach (Vector3 pt in segmentPoints)
        {
            transformedPoints.Add(plane.transform.TransformPoint(pt));
        }

        // Create a SegmentControlPoints object using the root's space
        SegmentControlPoints segment = new SegmentControlPoints(root, transformedPoints, 5, false, segmentMaterial);

        // Set initial growth to 0
        segment.SetGrowth(0f);

        // Add the segment to the list
        segments.Add(segment);

        // Track the start time for the growth of this segment
        segmentGrowthStartTimes.Add(0f); // -1 means not started yet

        segmentGrowthStartTimes[0] = Time.time;
    }

    // Helper method to create a spline from a list of points
    private Spline CreateSplineFromPoints(List<Vector3> points)
    {
        // Convert the points into local space relative to the root
        List<Vector3> localPoints = new List<Vector3>();

        foreach (var point in points)
        {
            Vector3 localPoint = root.transform.InverseTransformPoint(point);
            localPoints.Add(localPoint);
        }

        // Create the spline from the local points
        return new CatmullRomSpline(localPoints, 1);
    }

    public void Update()
    {
        if (!isGrowing || segments.Count == 0) return;

        for (int i = 0; i < segments.Count; i++)
        {
            // Check if this segment should start growing
            if (segmentGrowthStartTimes[i] >= 0)
            {
                // Calculate growth progress based on the time elapsed since the segment started growing
                float growthProgress = (Time.time - segmentGrowthStartTimes[i]) / growthTime;
                growthProgress = Mathf.Clamp01(growthProgress);

                // Set the growth of the segment
                segments[i].SetGrowth(growthProgress);

                // If the current segment is fully grown, start growing the next segment
                if (growthProgress >= 1f && i + 1 < segments.Count && segmentGrowthStartTimes[i + 1] < 0)
                {
                    segmentGrowthStartTimes[i + 1] = Time.time;
                }

                // Stop the overall growth if the last segment is fully grown
                if (growthProgress >= 1f && i == segments.Count - 1)
                {
                    isGrowing = false;
                    // Call AddSegment to add a new segment when the last one is fully grown
                    AddSegment();
                }
            }
        }
    }

    // Method to add a new segment between 80% and 100% in an upwards-ish direction using AddChild
    private void AddSegment()
    {
        // Get the last segment
        SegmentControlPoints lastSegment = segments[segments.Count - 1];

        // Calculate a random point between 80% and 100% of the last segment's length
        float percentAlongLastSegment = UnityEngine.Random.Range(0.6f, 1.0f);

        // Calculate a direction that is upwards-ish
        Vector3 upwardsDirection = (Vector3.up + UnityEngine.Random.insideUnitSphere * 0.4f).normalized;

        // Add the new child segment using the AddChild method
        SegmentControlPoints newSegment = lastSegment.AddChild(percentAlongLastSegment, upwardsDirection * 4, 4, 5, true);

        // Add the new segment to the list
        segments.Add(newSegment);

        // Start growing the new segment
        segmentGrowthStartTimes.Add(Time.time);
        isGrowing = true;
    }
}
