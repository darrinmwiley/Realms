using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//growplane is becoming trellis. ok for now, eventually separate it back out
public class GrowPlane : MonoBehaviour
{
    public GameObject planeObject;
    public Material green;

    List<Segment> segments = new List<Segment>();
    public int numSegments;

    public TargetPlane left, front, right;

    public bool newAdded = false;
    
    void Start(){
        CreateSpline(numSegments);
        segments[0].growStartTime = Time.time;
        segments[0].rendering.active = true;
    }

    void Update(){
        for(int i = 0;i<segments.Count - 1;i++)
        {
            Segment seg = segments[i];
            Segment next = segments[i+1];
            if(seg.IsGrown() && next.growStartTime == -1)
            {
                next.StartGrowth();
                next.Resize(.01f);
                next.rendering.active = true;
            }
        }
        Segment last = segments[segments.Count - 1];
        if(last.IsGrown() && !newAdded)
        {
            int randomSocketIndex = Random.Range(0,last.nodeLocations.Count);
            TargetPlane targetPlane = new TargetPlane[]{front, right, left}[randomSocketIndex % 3];
            Vector3 start = last.nodeLocations[randomSocketIndex];
            Vector3 end = targetPlane.RandomPointOnPlane();
            AddCapsule(start, end, segments[segments.Count - 1]);
            newAdded = true;
        }
    }

    public void CreateSpline(int numSegments)
    {
        List<float> heights = new List<float>();
        float avgSegmentHeight = 1f / (numSegments);
        for(int i = 1;i<numSegments;i++)
        {
            heights.Add(avgSegmentHeight * i + Random.Range(-0.25f, 0.25f) * avgSegmentHeight);
        }
        List<Vector3> locations = new List<Vector3>();
        locations.Add(new Vector3(-5,0,0));
        for(int i = 0;i<heights.Count;i++)
        {
            float x = heights[i] * 10 - 5;
            float z = Random.Range(-5f,5f);
            locations.Add(new Vector3(x,0,z));
        }
        locations.Add(new Vector3(5,0,0));
        List<Vector3> transformed = new List<Vector3>();
        foreach(Vector3 v in locations){
            transformed.Add(transform.TransformPoint(v));
        }
        AddCapsules(transformed);
    }

    public void AddCapsule(Vector3 startPoint, Vector3 endPoint, Segment parent)
    {
        // Create a capsule
        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.transform.position = (startPoint + endPoint) / 2; // Position it in the middle between the two points

        // Scale the capsule
        float capsuleHeight = (startPoint - endPoint).magnitude; // Calculate the distance between the points
        capsule.transform.localScale = new Vector3(.03f, capsuleHeight * 0.5f,  .03f); // Scale it

        // Rotate the capsule to align with the points
        capsule.transform.up = endPoint - startPoint;
        capsule.GetComponent<Renderer>().enabled = false;
        Segment next = capsule.AddComponent<Segment>();
        next.body = capsule;
        next.growTime = 4;
        next.start = startPoint;
        next.end = endPoint;
        next.Init(green);
        if(parent != null){
            next.parent = parent;
            parent.children.Add(next);
        }
        segments.Add(next);
    }

    public void AddCapsules(List<Vector3> locations)
    {
        /*List<Vector3> catmull = CatmullRomSpline.CreateSpline(locations, 10);
        foreach(Vector3 pt in catmull)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = pt;
            sphere.transform.localScale = new Vector3(.03f,.03f,.03f);
        }*/
        for (int i = 0; i < locations.Count - 1; i++)
        {
            Vector3 startPoint = locations[i];
            Vector3 endPoint = locations[i + 1];

            AddCapsule(startPoint, endPoint,i==0?null:segments[i - 1]);

        }
    }
}
