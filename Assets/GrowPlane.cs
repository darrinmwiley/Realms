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
    public int numSegments;

    public TargetPlane left, front, right;

    public bool newAdded = false;
    public bool addedJoint = false;
    bool lol = false;
    
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
        //TODO clean up going from the top of the growplane to the targetplane
        if(last.IsGrown() && !newAdded)
        {
            int randomSocketIndex = Random.Range(0,last.nodeLocations.Count);
            TargetPlane targetPlane = new TargetPlane[]{front, right, left}[randomSocketIndex % 3];
            Vector3 start = last.nodeLocations[randomSocketIndex];
            Vector3 end = targetPlane.RandomPointOnPlane();
            AddCapsule(start, end, segments[segments.Count - 1]);
            newAdded = true;
        }
        if(newAdded && !addedJoint)
        {
            AddJointedSegment();
            addedJoint = true;
        }
        if(addedJoint == true)
        {
            if(last.IsGrown()&& !lol)
            {
                AddJointToLastTwoSegmentsLol();
                lol = true;
            }
        }
    }

    public void AddJointedSegment()
    {
        float jointLength = 1;
        Segment last = segments[segments.Count - 1];

        Vector3 start = last.end;
        Vector3 end = start + jointLength * last.body.transform.up;

        Segment next = AddCapsule(start, end, last);
    }

    public void AddJointToLastTwoSegmentsLol()
    {
        Debug.Log("lol!");
        Segment last = segments[segments.Count - 1];
        Rigidbody rb = last.body.GetComponent<Rigidbody>();
        if(rb == null)
        {
            rb = last.body.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.drag = 3.0f;
        rb.angularDrag = 3.0f;
        rb.mass = 0.01f;
        Segment prev = segments[segments.Count - 2];
        Debug.Log(prev == last);
        Rigidbody rb2 = prev.body.GetComponent<Rigidbody>();
        if(rb2 == null)
        {
            rb2 = prev.body.AddComponent<Rigidbody>();
        }
        rb2.isKinematic = true;
        rb2.drag = 3.0f;
        rb2.angularDrag = 3.0f;
        rb2.mass = 0.01f;
        ConfigureJoint(last.body, prev.body);
    }

    ConfigurableJoint ConfigureJoint(GameObject obj, GameObject connected)
    {
        // For all capsules except the bottom one
        ConfigurableJoint joint = obj.AddComponent<ConfigurableJoint>();
        joint.connectedBody = connected.GetComponent<Rigidbody>();

        // Set primary and secondary axes
        joint.axis = new Vector3(0, 1, 0); // Primary axis, e.g., local X-axis
        joint.secondaryAxis = new Vector3(1, 0, 0); // Secondary axis, e.g., local Y-axis


        // Lock XYZ motion
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;

        // Lock angular X, but limit angular Y and Z
        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Limited;
        joint.angularZMotion = ConfigurableJointMotion.Limited;

        // Set angular Y and Z limits
        SoftJointLimit jointLimit = new SoftJointLimit();
        jointLimit.limit = 5; // 5 degrees limit
        joint.angularYLimit = jointLimit;
        joint.angularZLimit = jointLimit;

        // Set very high spring force for angular Y and Z limits using SoftJointLimitSpring
        SoftJointLimitSpring limitSpring = new SoftJointLimitSpring();
        limitSpring.spring = 10000; // Very high spring force
        limitSpring.damper = 1000; // High damper
        joint.angularYZLimitSpring = limitSpring;

        // Set rotation drive mode YZ
        JointDrive drive = new JointDrive();
        drive.positionSpring = 5; // Small spring force
        drive.positionDamper = 10; // Small damper
        joint.rotationDriveMode = RotationDriveMode.Slerp;

        joint.slerpDrive = drive;

        // Set Y and Z target rotation to all zeros
        joint.targetRotation = Quaternion.identity;

        // Enable collision
        joint.enableCollision = true;

        // Set joint anchors
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = new Vector3(0, -1, 0);
        joint.connectedAnchor = new Vector3(0, 1, 0);

        // Set projection mode to None for linear deviations and Limit to angular deviations
        joint.projectionMode = JointProjectionMode.PositionAndRotation;
        
        // Set the projection distance for X and Z axes to zero (no linear deviation allowed)
        joint.projectionAngle = 0f;
        
        // Set the projection angle limit to zero (no angular deviation allowed) for Y-axis (vertical)
        joint.projectionAngle = 0f;

        joint.configuredInWorldSpace = true;

        return joint;
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
        return next;
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
