using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SegmentControlPointsTester : MonoBehaviour
{
    SegmentControlPoints segment;
    public Material mat;
    private float growStartTime;
    public int growTime;

    //TODO it should be possible to define these in either local or world space
    public Transform[] controlPointTransforms; // Array of transforms to define control points
    List<Vector3> controlPoints = new List<Vector3>();

    // Start is called before the first frame update
    void Start()
    {
        // Convert the positions of the Transform array to control points
        foreach (Transform point in controlPointTransforms)
        {
            controlPoints.Add(point.position);
        }

        // Create a new SegmentControlPoints object using the control points
        segment = new SegmentControlPoints(gameObject, controlPoints, 0, true, mat);
        growStartTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        float growth = (Time.time - growStartTime) / growTime;
        if (growth > 1)
            growth = 1;
        segment.SetGrowth(growth);
    }
}
