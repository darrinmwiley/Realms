using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SegmentControlPointsTester : MonoBehaviour
{

    SegmentControlPoints segment;
    public Material mat;
    private float growStartTime;
    public int growTime;
    List<Vector3> controlPoints = new List<Vector3>();
    // Start is called before the first frame update
    void Start()
    {
        controlPoints.Add(new Vector3(0,2,2));
        controlPoints.Add(new Vector3(0,4,0));
        segment = new SegmentControlPoints(gameObject, controlPoints, 0, mat);
        growStartTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        float growth = (Time.time - growStartTime) / growTime;
        if(growth > 1)
            growth = 1;
        segment.SetGrowth(growth);
    }
}
