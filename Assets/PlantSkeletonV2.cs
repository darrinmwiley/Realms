using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantSkeletonV2 : MonoBehaviour
{

    //public GameObject sun;
    public float distanceBetweenJoints;
    public float drag;
    public float angulardrag;
    public float mass;

    public GameObject baseObj;
    List<Segment> segments = new List<Segment>();

    class Segment{
        public GameObject body;
        public GameObject jointObj;
        public ConfigurableJoint rotator;
        public ConfigurableJoint fixedJoint;
    }

    void Start(){
        segments.Add(new Segment(){
            body = baseObj
        });
        AddSegment();
        AddSegment();
        AddSegment();
        AddSegment();
    }

    public void AddSegment()
    {
        Segment seg = segments[segments.Count - 1];
        GameObject jointObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        jointObj.transform.position = seg.body.transform.position;
        jointObj.GetComponent<Collider>().enabled = false;
        jointObj.transform.rotation = seg.body.transform.rotation;
        jointObj.transform.parent = seg.body.transform;
        seg.jointObj = jointObj;
        GameObject nextObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nextObj.transform.position = jointObj.transform.position + jointObj.transform.up * distanceBetweenJoints;
        ConfigurableJoint rotator = ConfigureRotator(jointObj, seg.body);
        ConfigurableJoint fixedJoint = ConfigureFixed(jointObj, nextObj);
        seg.rotator = rotator;
        segments.Add(new Segment(){
            body = nextObj,
            fixedJoint = fixedJoint
        });
    }

    public ConfigurableJoint ConfigureRotator(GameObject jointObj, GameObject segmentObj)
    {
        if(jointObj.GetComponent<Rigidbody>() == null)
            jointObj.AddComponent<Rigidbody>();
        Rigidbody jrb = jointObj.GetComponent<Rigidbody>();
        jrb.drag = drag;
        jrb.angularDrag = angulardrag;
        jrb.mass = mass;
        ConfigurableJoint joint = jointObj.AddComponent<ConfigurableJoint>();
        if(segmentObj.GetComponent<Rigidbody>() == null)
            segmentObj.AddComponent<Rigidbody>();
        Rigidbody rb = segmentObj.GetComponent<Rigidbody>();
        joint.connectedBody = rb;
        rb.drag = drag;
        rb.angularDrag = angulardrag;
        rb.mass = mass;
        // Set primary and secondary axes
        joint.axis = new Vector3(0, 1, 0); // Primary axis, e.g., local X-axis
        joint.secondaryAxis = new Vector3(1, 0, 0); // Secondary axis, e.g., local Y-axis
        joint.anchor = new Vector3(0,0,0);

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
        jointLimit.limit = 5; // 1 degrees limit
        joint.angularYLimit = jointLimit;
        joint.angularZLimit = jointLimit;

        // Set rotation drive mode YZ
        JointDrive drive = new JointDrive();
        drive.positionSpring = 50000; // Small spring force
        drive.positionDamper = 5; // Small damper
        joint.rotationDriveMode = RotationDriveMode.Slerp;
        drive.maximumForce = 100000;

        joint.slerpDrive = drive;

        // Set Y and Z target rotation to all zeros
        joint.targetRotation = Quaternion.identity;

        // Set projection mode to None for linear deviations and Limit to angular deviations
        joint.projectionMode = JointProjectionMode.PositionAndRotation;
        
        // Set the projection distance for X and Z axes to zero (no linear deviation allowed)
        joint.projectionAngle = 0f;
        
        // Set the projection angle limit to zero (no angular deviation allowed) for Y-axis (vertical)
        joint.projectionAngle = 0f;

        return joint;
    }


    public ConfigurableJoint ConfigureFixed(GameObject jointObj, GameObject nextObj)
    {
        if(jointObj.GetComponent<Rigidbody>() == null)
            jointObj.AddComponent<Rigidbody>();
        ConfigurableJoint joint = jointObj.AddComponent<ConfigurableJoint>();
        if(nextObj.GetComponent<Rigidbody>() == null)
            nextObj.AddComponent<Rigidbody>();
        Rigidbody rb = nextObj.GetComponent<Rigidbody>();
        rb.drag = drag;
        rb.angularDrag = angulardrag;
        rb.mass = mass;
        joint.connectedBody = nextObj.GetComponent<Rigidbody>();
        // Set primary and secondary axes
        joint.axis = new Vector3(0, 1, 0); // Primary axis, e.g., local X-axis
        joint.secondaryAxis = new Vector3(1, 0, 0); // Secondary axis, e.g., local Y-axis
        joint.anchor = new Vector3(0,0,0);
        // Lock XYZ motion
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;

        // Lock angular X, but limit angular Y and Z
        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;

        // Set Y and Z target rotation to all zeros
        joint.targetRotation = Quaternion.identity;

        // Set projection mode to None for linear deviations and Limit to angular deviations
        joint.projectionMode = JointProjectionMode.PositionAndRotation;
        
        // Set the projection distance for X and Z axes to zero (no linear deviation allowed)
        joint.projectionAngle = 0f;
        
        // Set the projection angle limit to zero (no angular deviation allowed) for Y-axis (vertical)
        joint.projectionAngle = 0f;

        return joint;
    }
}
