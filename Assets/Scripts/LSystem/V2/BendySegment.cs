using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

//position: <time, offset>
//update: time

public class BendySegment : LSystemV2<Vector2, float>
{
    public Spline spline;
    public float radius = .1f;

    int numSegments = 6;

    public List<GameObject> capsules;

    public BendySegment(Spline spline, int numSegments){
        this.numSegments = numSegments;
        gameObject.name = "Bendy Segment";
        capsules = new List<GameObject>();
        for(int i = 0;i<numSegments;i++)
        {
            capsules.Add(GameObject.CreatePrimitive(PrimitiveType.Capsule));
            capsules[i].transform.parent = gameObject.transform;
            capsules[i].transform.localScale = new Vector3(.1f, .1f, .1f);
            if(i != 0){
                ConfigureJoint(capsules[i-1], capsules[i], new Vector3(0,1,0), new Vector3(0,-1,0));
            }
        }
        this.spline = spline;
    }

    public override void Update(float time) {
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        float dt = 1f / (numSegments);
        for (int i = 0; i < numSegments; i++) {
            Vector3 start = spline.Evaluate(i * dt * time);
            Vector3 end = spline.Evaluate((i + 1) * dt * time);
            if(time == 1)
            {
                Debug.Log(i+" "+start+" "+end);
            }

            // Assuming you have an array or list of capsules
            capsules[i].transform.localPosition = start;

            // Calculate the direction from start to end
            Vector3 direction = end - start;

            // Align the capsule's up direction with the direction from start to end
            capsules[i].transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction);

            // Adjusting the scale to match the length of the segment
            float length = direction.magnitude / 2f;
            Vector3 scale = capsules[i].transform.localScale;
            scale.y = length; // Assuming the capsule's length is along the y-axis
            capsules[i].transform.localScale = scale;
        }
    }

    public void ConfigureJoint(GameObject parent, GameObject child, Vector3 parentAnchorLocation, Vector3 childAnchorLocation)
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
        ballJoint.transform.up = parent.transform.up;
        ballJoint.name = "ball joint";
        ballJoint.GetComponent<Collider>().enabled = false;
        ballJoint.transform.localScale = new Vector3(.01f, .01f, .01f);
        ConfigurableJoint joint = ballJoint.GetComponent<ConfigurableJoint>();
        if(joint == null)
            joint = ballJoint.AddComponent<ConfigurableJoint>();
        joint.targetRotation = Quaternion.Euler(new Vector3(0,0,0));
        ballJoint.transform.rotation = Quaternion.Euler(0,0,0);
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


    public override Vector3 GetPositionLocal(Vector2 timeAndOffset){
        float time = timeAndOffset.x;
        float offset = timeAndOffset.y;
        Vector3 ret = spline.Evaluate(time * offset);
        return ret;
    }

    public override Vector3 GetDirectionLocal(Vector2 timeAndOffset){
        float epsilon = .1f;
        return (GetPositionLocal(timeAndOffset) - GetPositionLocal(timeAndOffset - new Vector2(0,epsilon))).normalized;
    }

    public override Vector3 GetDirectionAbsolute(Vector2 timeAndOffset)
    {
        float epsilon = .1f;
        Vector3 ans = (GetPositionAbsolute(timeAndOffset) - GetPositionAbsolute(timeAndOffset - new Vector2(0,epsilon))).normalized;
        return ans;
    }

    public Mesh MakeMesh(float time)
    {
        return null;
    }

    public float InterpolateOuterRadius(float outerRadiusMin, float outerRadiusMax, float y, float innerRadius, int numSegments)
    {
        if(y < -.3f)
        {
            float percent = (-.3f - y) / .2f;
            return Mathf.Lerp(outerRadiusMin, innerRadius, percent);
        }else if(y > .4f)
        {
            float percent = (.5f - y) / .1f;
            return Mathf.Lerp(innerRadius, outerRadiusMin, percent);
        }
        y = ((y - -.3f) / .7f)-.5f;
        y = (y+.5f)*numSegments%1-.5f;
        float ans = 4 * (outerRadiusMin - outerRadiusMax) * y * y + outerRadiusMax;
        return ans;
    }
}