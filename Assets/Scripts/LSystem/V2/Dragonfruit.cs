using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

//position: <time, offset>
//update: time
public class Dragonfruit : LSystemV2<Vector2, float>
{
    public Material mat;
    public AnimationCurve thicknessGrowthCurve;

    //side of trellis plane
    public GameObject plane;
    //endpoints of each segment in the plane's local coordinate space
    List<Vector3> segmentEndpoints;
    //how many segments does it take to top out the trellis;
    private int numClimbingSegments = 4;

    int maxSegments = 10;

    List<DragonfruitSegment> segments = new List<DragonfruitSegment>();

    public Dragonfruit(Material mat, AnimationCurve thicknessGrowthCurve, GameObject plane){
        this.plane = plane;
        segmentEndpoints = CreateSegmentEndpoints(numClimbingSegments);
        gameObject.name = "Dragonfruit";
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "root";
        root.transform.parent = gameObject.transform;
        root.transform.localScale = new Vector3(.01f, .01f, .01f);
        root.GetComponent<Collider>().enabled = false;
        Rigidbody rootRb = root.AddComponent<Rigidbody>();
        rootRb.isKinematic = true;
        this.thicknessGrowthCurve = thicknessGrowthCurve;
        this.mat = mat;
        DragonfruitSegment initialSegment = CreateFixedSegment(plane.transform.TransformPoint(segmentEndpoints[0]), plane.transform.TransformPoint(segmentEndpoints[1]));
        //initialSegment.gameObject.transform.parent = gameObject.transform;
        initialSegment.gameObject.name = "segment 0";
        segments.Add(initialSegment);
        //todo: add a second segment to see if the joint looks realistic when attached to non-kinematic
        //ConfigureJoint(root, initialSegment.gameObject, new Vector3(0,0,0), new Vector3(0,0,0), Vector3.up);
    }

    public List<Vector3> CreateSegmentEndpoints(int numSegments)
    {
        List<float> heights = new List<float>();
        float avgSegmentHeight = 1f / (numSegments);
        for(int i = 1;i<numSegments;i++)
        {
            heights.Add(avgSegmentHeight * i + UnityEngine.Random.Range(-0.25f, 0.25f) * avgSegmentHeight);
        }
        List<Vector3> locations = new List<Vector3>();
        locations.Add(new Vector3(0,0,-5));
        for(int i = 0;i<heights.Count;i++)
        {
            float x = UnityEngine.Random.Range(-5f,5f);
            float z = heights[i] * 10 - 5;
            locations.Add(new Vector3(x,0,z));
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localScale = new Vector3(.01f, .01f, .01f);
            sphere.transform.parent = plane.transform;
            sphere.transform.localPosition = new Vector3(x,0,z);
        }
        locations.Add(new Vector3(0,0,5));
        return locations;
    }

    public DragonfruitSegment CreateFixedSegment(Vector3 start, Vector3 end)
    {
        List<Vector3> points = new List<Vector3>(){new Vector3(0,0,0), end - start};
        Spline spline = new CatmullRomSpline(points,50);
        DragonfruitSegment seg = new DragonfruitSegment(spline, mat, /*innerRadius = */ .02f, /*outerRadius = */ .06f, /*samples = */ 100, thicknessGrowthCurve);
        seg.gameObject.transform.position = start;
        seg.gameObject.AddComponent<Rigidbody>().isKinematic = true;
        return seg;
    }

    public void AddSegment()
    {
        DragonfruitSegment prevSegment = segments[segments.Count - 1];
        if(segments.Count < numClimbingSegments)
        {
            DragonfruitSegment nextSeg = CreateFixedSegment(plane.transform.TransformPoint(segmentEndpoints[segments.Count]), plane.transform.TransformPoint(segmentEndpoints[segments.Count + 1]));
            prevSegment.AddChild(nextSeg);
            segments.Add(nextSeg);
        }else{
            DragonfruitSegment nextSegment = CreateSegment();
            prevSegment.AddChild(nextSegment);
            Node node = prevSegment.nodes[4];
            Vector3 positionAbsolute = node.location;
            Vector3 direction = (node.orthagonalDirection + node.segmentDirection) / 2;
            nextSegment.gameObject.transform.position = positionAbsolute;
            Vector3 positionOnParent = prevSegment.gameObject.transform.InverseTransformPoint(positionAbsolute);
            //nextSegment.gameObject.transform.parent = gameObject.transform; 
            ConfigureJoint(prevSegment.gameObject, nextSegment.gameObject, positionOnParent, new Vector3(0,0,0), direction);
            segments.Add(nextSegment);
        }
        for(int i = 0;i<segments.Count -1 ;i++)
        {
            Rigidbody rb = segments[i].gameObject.GetComponent<Rigidbody>();
            if(rb != null)
                rb.mass = segments.Count - i;
        }
    }

    //this is for side nodes
    /*public void AddSegment(Node node)
    {
        DragonfruitSegment nextSegment = CreateSegment();
        prevSegment.AddChild(nextSegment);
        Node node = prevSegment.nodes[4];
        Vector3 positionAbsolute = node.location;
        Vector3 direction = (node.orthagonalDirection + node.segmentDirection) / 2;
        nextSegment.gameObject.transform.position = positionAbsolute;
        Vector3 positionOnParent = prevSegment.gameObject.transform.InverseTransformPoint(positionAbsolute);
        //nextSegment.gameObject.transform.parent = gameObject.transform; 
        ConfigureJoint(prevSegment.gameObject, nextSegment.gameObject, positionOnParent, new Vector3(0,0,0), direction);
        segments.Add(nextSegment);
    }*/

    public void ConfigureJoint(GameObject parent, GameObject child, Vector3 parentAnchorLocation, Vector3 childAnchorLocation, Vector3 parentAnchorRotation)
    {
        Rigidbody parentRb = parent.GetComponent<Rigidbody>();
        if(parentRb == null)
            parentRb = parent.AddComponent<Rigidbody>();

        Rigidbody childRb = child.GetComponent<Rigidbody>();
        if(childRb == null){
            childRb = child.AddComponent<Rigidbody>();
        }
        childRb.drag = 5;
        childRb.angularDrag = 5;

        Physics.IgnoreCollision(parent.GetComponent<Collider>(), child.GetComponent<Collider>());

        GameObject ballJoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        Rigidbody jointRb = ballJoint.AddComponent<Rigidbody>();
        jointRb.constraints |= RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        jointRb.isKinematic = false;
        jointRb.mass = 100;
        FollowGameObjectLocalPosition follow = ballJoint.AddComponent<FollowGameObjectLocalPosition>();
        follow.toFollow = parent;
        follow.localPosition = parentAnchorLocation;
        ballJoint.transform.parent = parent.transform;
        ballJoint.transform.localPosition = parentAnchorLocation;
        ballJoint.name = "ball joint";
        ballJoint.GetComponent<Collider>().enabled = false;
        ballJoint.transform.localScale = new Vector3(.01f, .01f, .01f);
        ConfigurableJoint joint = ballJoint.AddComponent<ConfigurableJoint>();
        joint.targetRotation = Quaternion.Euler(new Vector3(0,0,0));
        ballJoint.transform.up = parentAnchorRotation.normalized;
        child.transform.up = parentAnchorRotation;
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = new Vector3(0,0,0);
        joint.connectedBody = childRb;
        joint.connectedAnchor = childAnchorLocation;
        joint.enableCollision = false;

        joint.axis = new Vector3(0, 1, 0);
        joint.secondaryAxis = new Vector3(1, 0, 0);
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;
        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Limited;
        joint.angularZMotion = ConfigurableJointMotion.Limited;

        // Set angular Y and Z limits
        SoftJointLimit jointLimit = new SoftJointLimit();
        jointLimit.limit = 15; // 5 degrees limit
        joint.angularYLimit = jointLimit;
        joint.angularZLimit = jointLimit;

        // Set rotation drive mode YZ
        JointDrive drive = new JointDrive();
        //drive.positionSpring = 20f; // Small spring force
        drive.positionDamper = 20f; // Small damper
        drive.maximumForce = 100;
        joint.rotationDriveMode = RotationDriveMode.Slerp;
        joint.slerpDrive = drive;
    }

    public Spline MakeSpline(float height)
    {
        float noise = .3f;
        int numPoints = 10;
        List<Vector3> controlPoints = new List<Vector3>();
        controlPoints.Add(new Vector3(0,0,0));
        controlPoints.Add(new Vector3(0,height,0));
       /* float dh = height / (numPoints - 1f);
        for(int i = 1;i<numPoints;i++)
        {
            float xDeviation = UnityEngine.Random.Range(-noise, noise);
            float zDeviation = UnityEngine.Random.Range(-noise, noise);
            controlPoints.Add(new Vector3(controlPoints[i - 1].x + xDeviation * dh, i * dh, controlPoints[i-1].z + zDeviation * dh));
        }*/
        return new CatmullRomSpline(controlPoints,(int)(50 * height));
    }

    public DragonfruitSegment CreateSegment(){
        return new DragonfruitSegment(MakeSpline(.5f), mat, /*innerRadius = */ .02f, /*outerRadius = */ .06f, /*samples = */ 100, thicknessGrowthCurve);
    }

    public override void Update(float time){
        //initialSegment.Update(time / 5);
        for(int i = 0;i<segments.Count;i++)
        {
            float t = (time - i * 5) / 5;
            segments[i].Update(t);
        }

        if(time / 5 > segments.Count && segments.Count < maxSegments)
        {
            AddSegment();
        }
    }

    
    /*TODO
    public override Vector3 GetPositionLocal(Vector2 timeAndOffset){}

    public override Vector3 GetDirectionLocal(Vector2 timeAndOffset){
        float epsilon = .1f;
        return (GetPositionLocal(timeAndOffset) - GetPositionLocal(timeAndOffset - new Vector2(0,epsilon))).normalized;
    }*/
}