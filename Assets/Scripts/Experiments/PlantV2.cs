using UnityEngine;
using System.Collections.Generic;

// todo: separate direction from magnitude
// todo: investigate anomalies when moving during growth
// todo: come up with relative growth direction scheme

public class PlantV2 : MonoBehaviour
{
    public Material mat; // Material for the segments

    public GameObject root; // Root GameObject for the plant
    private SegmentV2 spine;
    private SegmentV2 child;
    private float childTime;

    public float growthTime = 50f; // Total growth time for the plant

    void Start()
    {
        // Ensure the root has an ArticulationBody
        if (root == null)
        {
            root = new GameObject("Plant Root");
            root.transform.position = transform.TransformPoint(Vector3.zero);
            root.transform.parent = transform;
        }

        ArticulationBody rootArticulation = root.GetComponent<ArticulationBody>();
        if (rootArticulation == null)
        {
            rootArticulation = root.AddComponent<ArticulationBody>();
            rootArticulation.jointType = ArticulationJointType.FixedJoint; // Set it as a fixed joint
        }

        // Create the first segment of the spine
        spine = new SegmentV2(root, Vector3.up, MakeSpline(5), 3, 5, mat);
    }

    public Spline MakeSpline(float length)
    {
        float noise = .3f;
        int numPoints = 10;
        List<Vector3> controlPoints = new List<Vector3>();
        controlPoints.Add(new Vector3(0, 0, 0));

        //controlPoints.Add(new Vector3(0,1,0));

        float dh = length / (numPoints - 1f);
        for (int i = 1; i < numPoints; i++)
        {
            float xDeviation = Random.Range(-noise, noise);
            float zDeviation = Random.Range(-noise, noise);
            controlPoints.Add(new Vector3(controlPoints[i - 1].x + xDeviation * dh, i * dh, controlPoints[i - 1].z + zDeviation * dh));
        }

        return new CatmullRomSpline(controlPoints, 1);
    }

    void FixedUpdate()
    {
        float growthAmt = Time.deltaTime / growthTime;
        float newGrowthPercentage = Mathf.Min(1, spine.GetGrowth() + growthAmt);

        // Grow the spine segment
        spine.SetGrowth(newGrowthPercentage);

        // If the spine is fully grown and the child segment doesn't exist, create a child
        if (newGrowthPercentage == 1 && child == null)
        {
            Spline newSpline = MakeSpline(4);
            child = spine.AddChild(.8f, new Vector3(1, 1, 0), newSpline);
            childTime = Time.time;
        }

        // Grow the child segment if it exists
        if (child != null)
        {
            newGrowthPercentage = Mathf.Min(1, child.GetGrowth() + growthAmt);
            child.SetGrowth(newGrowthPercentage);
        }
    }
}
