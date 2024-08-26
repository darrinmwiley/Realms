using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//growplane is becoming trellis. ok for now, eventually separate it back out
//TODO fix socket location bug
public class GrowPlane : MonoBehaviour
{
    public GameObject planeObject;
    public Material green;

    List<Segment2> Segment2s = new List<Segment2>();
    Segment2 trellisTop;
    public int numSegment2s;

    public TargetPlane[] left, front, right;

    public bool newAdded = false;
    public int bridgeJoints = 0;
    
    void Start(){
        CreateSpline(numSegment2s);
        trellisTop = Segment2s[Segment2s.Count - 1];
        Segment2s[0].StartGrowth();
    }

    void Update(){
        //todo: after we reach the height of the trellis, 
        //we can pick any socket and add a jointed Segment2 to it
        for(int i = 0;i<Segment2s.Count - 1;i++)
        {
            Segment2 seg = Segment2s[i];
            Segment2 next = Segment2s[i+1];
            if(seg.IsGrown() && next.growStartTime == -1)
            {
                next.StartGrowth();
            }
        }
        Segment2 last = Segment2s[Segment2s.Count - 1];
        //TODO clean up going from the top of the growplane to the targetplane
        if(trellisTop.IsGrown() && bridgeJoints != 9)
        {
            if(AddBridgeJoint(trellisTop))
                bridgeJoints++;
        }
        for(int i = 0;i<Segment2s.Count;i++)
        {
            Segment2 seg = Segment2s[i];
            if(seg.IsGrown() && seg.distanceFromBridge != -1 && seg.distanceFromBridge < 3 && seg.children.Count == 0)
            {
                float length = Random.Range(.2f, .4f) * (seg.distanceFromBridge + 1);
                AddJointedSegment2(seg.GetTip(),seg.GetTip() + length * seg.transform.up,seg, seg.distanceFromBridge == 0);
            }
        }
        //if(newAdded && jointsAdded != 4 && Segment2s[Segment2s.Count - 1].IsGrown())
        //{
        //    AddJointedSegment2();
        //    jointsAdded++;
        //}
    }

    //we'll separate Segment2s into 3 categories: 
    //plane: vertical Segment2s climbing up the trellis 
    //bridge: from the top of the trellis to the edge of the trellis
    //umbrella: jointed, hanging off the trellis after the bridge

    bool AddBridgeJoint(Segment2 parent)
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
        Node2 node = parent.rings[randomRingIndex][randomSocketIndex];
        if(node.used)
            return false;
        node.used = true;
        targetPlane.used = true;
        Vector3 start = parent.rings[randomRingIndex][randomSocketIndex].location;
        Vector3 end = targetPlane.RandomPointOnPlane();
        Segment2 added = AddJointedSegment2(start, end, parent, true, true);
        added.distanceFromBridge = 0;
        return true;
    }

    //for adding with a node
    public Segment2 AddJointedSegment2(Vector3 start, Vector3 end, Segment2 parent, bool shouldParentBeKinematic = false, bool shouldSelfBeKinematic = false)
    {
        Segment2 next = AddCapsule(start, end, parent);
        next.isJointed = true;
        next.AddJoint(shouldParentBeKinematic, shouldSelfBeKinematic);
        next.StartGrowth();
        return next;
    } 

    //this one is for adding it with the tip
    public Segment2 AddJointedSegment2(Segment2 parent)
    {
        float jointLength = .5f;
        Segment2 last = Segment2s[Segment2s.Count - 1];

        Vector3 start = last.GetTip();
        Vector3 end = start + jointLength * last.transform.up;

        Segment2 next = AddCapsule(start, end, last);
        next.isJointed = true;
        next.AddJoint();
        next.StartGrowth();
        return next;
    }

    //todo calculate average Segment2 length independently of growplane here to use elsewhere
    public void CreateSpline(int numSegment2s)
    {
        List<float> heights = new List<float>();
        float avgSegment2Height = 1f / (numSegment2s);
        for(int i = 1;i<numSegment2s;i++)
        {
            heights.Add(avgSegment2Height * i + Random.Range(-0.25f, 0.25f) * avgSegment2Height);
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

    public Segment2 AddCapsule(Vector3 startPoint, Vector3 endPoint, Segment2 parent)
    {
        // Create a capsule
        GameObject Segment2Obj = new GameObject("Segment2");
        Segment2Obj.transform.position = (startPoint); // Position it in the middle between the two points

        // Scale the capsule
        float height = (startPoint - endPoint).magnitude; // Calculate the distance between the points

        // Rotate the capsule to align with the points
        Segment2Obj.transform.up = endPoint - startPoint;
        Segment2 Segment2 = Segment2Obj.AddComponent<Segment2>();
        Segment2.growTime = 4;
        Segment2.start = startPoint;
        Segment2.end = endPoint;
        if(parent != null){
            Segment2.parent = parent;
            parent.children.Add(Segment2);
        }
        Segment2.Init(green, startPoint);
        Segment2s.Add(Segment2);
        return Segment2;
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

            AddCapsule(startPoint, endPoint,i==0?null:Segment2s[i - 1]);

        }
    }
}
