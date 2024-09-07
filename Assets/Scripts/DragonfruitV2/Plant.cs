using UnityEngine;
using System.Collections.Generic;

//todo: separate direction from magnitude
//todo: investigate anomalies when moving during growth
//todo: come up with relative growth direction scheme

public class Plant : MonoBehaviour
{
    //public int numSegments = 10; // Controls how many segments to split the spline into
    //public float height = 40;

    public Spline spline; // Reference to the spline object
    public GameObject spherePrefab; // Prefab for the spheres

    private Vertex root;
    private Segment spine;
    private Segment child;
    private float childTime;

    public float growthTime = 50f;

    public Material mat;

    void Start(){
        root = new Vertex(transform.TransformPoint(Vector3.zero));
        root.gameObject.transform.parent = transform;
        root.gameObject.name = "Plant Root";
        spine = new Segment(root, Vector3.up, MakeSpline(5), 5, mat);
    }

    public Spline MakeSpline(float length)
    {
        float noise = .3f;
        int numPoints = 10;
        List<Vector3> controlPoints = new List<Vector3>();
        controlPoints.Add(new Vector3(0,0,0));
        //controlPoints.Add(new Vector3(0,10,0));
        float dh = length / (numPoints - 1f);
        for(int i = 1;i<numPoints;i++)
        {
            float xDeviation = Random.Range(-noise, noise);
            float zDeviation = Random.Range(-noise, noise);
            controlPoints.Add(new Vector3(controlPoints[i - 1].x + xDeviation * dh, i * dh, controlPoints[i-1].z + zDeviation * dh));
        }
        return new CatmullRomSpline(controlPoints,1);
    }


    void Update()
    {
        float growthAmt = Time.deltaTime / growthTime;
        float newGrowthPercentage = Mathf.Min(1, spine.GetGrowth() + growthAmt);
        spine.SetGrowth(newGrowthPercentage);
        if(newGrowthPercentage == 1 && child == null)
        {
            Spline spline = MakeSpline(4);
            child = spine.AddChild(.8f, new Vector3(1,0,0), spline);
            childTime = Time.time;
        }
        if(child != null)
        {
            newGrowthPercentage = Mathf.Min(1, child.GetGrowth() + growthAmt);
            child.SetGrowth(newGrowthPercentage);
        }
    }
}
