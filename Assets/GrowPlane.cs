using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//growplane is becoming trellis. ok for now, eventually separate it back out
//TODO fix socket location bug
public class GrowPlane : MonoBehaviour
{
    public GameObject planeObject;
    public Material green;

    List<Segment> segments = new List<Segment>();
    Segment trellisTop;
    public int numSegments;

    public TargetPlane[] left, front, right;

    public bool newAdded = false;
    public int bridgeJoints = 0;
    
    void Start(){
        CreateSpline(numSegments);
        trellisTop = segments[segments.Count - 1];
        segments[0].StartGrowth();
    }

    void Update(){
        //todo: after we reach the height of the trellis, 
        //we can pick any socket and add a jointed segment to it
        for(int i = 0;i<segments.Count - 1;i++)
        {
            Segment seg = segments[i];
            Segment next = segments[i+1];
            if(seg.IsGrown() && next.growStartTime == -1)
            {
                next.StartGrowth();
            }
        }
        Segment last = segments[segments.Count - 1];
        //TODO clean up going from the top of the growplane to the targetplane
        if(trellisTop.IsGrown() && bridgeJoints != 9)
        {
            if(AddBridgeJoint(trellisTop))
                bridgeJoints++;
        }
        for(int i = 0;i<segments.Count;i++)
        {
            Segment seg = segments[i];
            if(seg.IsGrown() && seg.distanceFromBridge != -1 && seg.distanceFromBridge < 3 && seg.children.Count == 0)
            {
                float length = Random.Range(.2f, .4f) * (seg.distanceFromBridge + 1);
                AddJointedSegment(seg.GetTip(),seg.GetTip() + length * seg.transform.up,seg, seg.distanceFromBridge == 0);
            }
        }
        //if(newAdded && jointsAdded != 4 && segments[segments.Count - 1].IsGrown())
        //{
        //    AddJointedSegment();
        //    jointsAdded++;
        //}
    }

    //we'll separate segments into 3 categories: 
    //plane: vertical segments climbing up the trellis 
    //bridge: from the top of the trellis to the edge of the trellis
    //umbrella: jointed, hanging off the trellis after the bridge

    bool AddBridgeJoint(Segment parent)
    {
        int randomRingIndex = Random.Range(0,parent.rings.Count);
        int randomSocketIndex = Random.Range(0,3);
        int randomTargetPlaneIndex = Random.Range(0,3);

        TargetPlane[,] targetPlanes = new TargetPlane[3,3];
        for(int i = 0;i<3;i++)
        {
            targetPlanes[0,i] = front[i];
            targetPlanes[1,i] = right[i];
            targetPlanes[2,i] = left[i];
        }
        TargetPlane targetPlane = targetPlanes[randomSocketIndex,randomTargetPlaneIndex];
        if(targetPlane.used)
            return false;
        Node node = parent.rings[randomRingIndex][randomSocketIndex];
        if(node.used)
            return false;
        node.used = true;
        targetPlane.used = true;
        Vector3 start = parent.rings[randomRingIndex][randomSocketIndex].location;
        Vector3 end = targetPlane.RandomPointOnPlane();
        Segment added = AddJointedSegment(start, end, parent, true, true);
        added.distanceFromBridge = 0;
        return true;
    }

    //for adding with a node
    public Segment AddJointedSegment(Vector3 start, Vector3 end, Segment parent, bool shouldParentBeKinematic = false, bool shouldSelfBeKinematic = false)
    {
        Segment next = AddCapsule(start, end, parent);
        next.isJointed = true;
        next.AddJoint(shouldParentBeKinematic, shouldSelfBeKinematic);
        next.StartGrowth();
        return next;
    } 

    //this one is for adding it with the tip
    public Segment AddJointedSegment(Segment parent)
    {
        float jointLength = .5f;
        Segment last = segments[segments.Count - 1];

        Vector3 start = last.GetTip();
        Vector3 end = start + jointLength * last.transform.up;

        Segment next = AddCapsule(start, end, last);
        next.isJointed = true;
        next.AddJoint();
        next.StartGrowth();
        return next;
    }

    //todo calculate average segment length independently of growplane here to use elsewhere
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

    public Segment AddCapsule(Vector3 startPoint, Vector3 endPoint, Segment parent)
    {
        // Create a capsule
        GameObject segmentObj = new GameObject("segment");
        segmentObj.transform.position = (startPoint); // Position it in the middle between the two points

        // Scale the capsule
        float height = (startPoint - endPoint).magnitude; // Calculate the distance between the points

        // Rotate the capsule to align with the points
        segmentObj.transform.up = endPoint - startPoint;
        Segment segment = segmentObj.AddComponent<Segment>();
        segment.growTime = 4;
        segment.start = startPoint;
        segment.end = endPoint;
        if(parent != null){
            segment.parent = parent;
            parent.children.Add(segment);
        }
        segment.Init(green, startPoint);
        segments.Add(segment);
        return segment;
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
