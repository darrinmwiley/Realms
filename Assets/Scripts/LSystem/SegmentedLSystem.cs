using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SegmentedLSystem : SplineLSystem
{
    public int numSegments;

    private List<GameObject> segments;

    //joints[i] is the joint between segment i and segment i + 1
    private List<ConfigurableJoint> joints;

    //first, just have them fixed
    //second, add fixed joints and only have the bottom be fixed
    //third, make the joints rotating instead and have a "stiffness" parameter that changes freedom and drive

    //might want to employ the builder pattern for all LSystems so we can take advantage of a constructor.

    //want to add more segments as we go

    //the general idea is: 

    //add a very small segment with the correct orientation
    //grow it until it reaches the target
    //add a new very small segment at the end of the previous
    //repeat

    public SegmentedLSystem(
        float startTime, 
        float startOffset, 
        float growTime, 
        Vector3 localRotation, 
        Vector3 localPosition, 
        float scale, 
        LSystem parent,
        Spline spline,
        AnimationCurve thicknessCurve,
        int verticalSamples,
        int horizontalSamples,
        int numSegments)
        : base(startTime, startOffset, growTime, localRotation, localPosition, scale, parent, spline, thicknessCurve, verticalSamples, horizontalSamples){
        this.numSegments = numSegments;
        segments = new List<GameObject>();
        joints = new List<ConfigurableJoint>();
        gameObject.name = "Segmented L-System";
        for(int i = 0;i<numSegments;i++)
        {
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            segment.AddComponent<Rigidbody>();
            segments.Add(segment);
            segment.name = "segment "+i;
            segment.transform.parent = gameObject.transform;
        }
        UpdateSkeleton(0);
    }

    public void UpdateSkeleton(float time)
    {
        float timePerSegment = 1f / numSegments;
        int completeSegments = (int)(time / timePerSegment);
        float remainder = time % timePerSegment;
        for(int i = 0;i<completeSegments;i++)
        {
            UpdateSegment(i,1, time);
        }
        UpdateSegment(completeSegments, remainder / timePerSegment, time);
        for(int i = completeSegments + 1;i<numSegments;i++)
        {
            UpdateSegment(i,0, time);
        }
    }

    public void UpdateSegment(int segment, float time, float realTime)
    {
        //Debug.Log("updating segment "+segment+" "+time);
        if(segment >= numSegments)
            return;
        float timePerSegment = 1f / numSegments;
        if(time == 0)
        {
            segments[segment].SetActive(false);
        }else
            segments[segment].SetActive(true);
        Vector3 startPosition = spline.Evaluate(segment * timePerSegment);
        float midpoint = (segment + .5f) * timePerSegment;
        float verticalTime = (segment+0f) / numSegments;
        float radius = realTime * thicknessCurve.Evaluate(verticalTime);
        Vector3 targetPosition = spline.Evaluate((segment + 1) * (timePerSegment));
        //rotate by absolute rotation around spline.Evaluate(0), then shift by absolute position
        Vector3 endPosition = Vector3.Lerp(startPosition, targetPosition, time);
        //alter the cylinder primitive position and scale such that it starts and ends at these positions

        Vector3 midPoint = (startPosition + endPosition) / 2f;

        // Calculate the distance between the start and end as the scale for our capsule
        float length = Vector3.Distance(startPosition, endPosition);

        // Set the capsule's position to the midpoint
        segments[segment].transform.localPosition = midPoint;

        // Assuming the capsule's initial scale is set properly for a unit length,
        // Scale the capsule's Y-axis by the length. Adjust X and Z for the capsule's thickness as needed.
        segments[segment].transform.localScale = new Vector3(radius, length / 2f, radius); // Unity's capsule height scales from the center, so we divide by 2

        // Calculate the direction from start to end in local space
        Vector3 localDirection = segments[segment].transform.parent.InverseTransformDirection(endPosition - startPosition);

        // Calculate the direction from start to end
        Vector3 direction = endPosition - startPosition;

        // Calculate the rotation to align the capsule with the direction vector
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);

        segments[segment].transform.localRotation = rotation;

        if(time == 1)
        {
            //Debug.Log("about to configure joint for segment "+segment);
            if(segment == 0)
            {
                Debug.Log("setting kinematic");
                segments[segment].GetComponent<Rigidbody>().isKinematic = true;
            }
            else{
                ConfigurableJoint joint;
                
                if(joints.Count <= segment || joints[segment] == null){
                    joint = segments[segment-1].AddComponent<ConfigurableJoint>();
                    joints.Add(joint);
                }
                else{
                    joint = joints[segment];
                }
                ConfigureJoint(segments[segment], segments[segment - 1],joint, new Vector3(0,1,0));
            }
        }

    }
    //how do I connect one L system to another via joint

    //segments "harden" over time and start retaining their position
    //in formative times they will move towards stimulus (light, moisture, nutrient, surface / stability, )

    //TODO: brainstorm on how to connect sub segments to a specific parent segment

    //parent anchor given in local coords
    ConfigurableJoint ConfigureJoint(GameObject child, GameObject parent, ConfigurableJoint joint, Vector3 anchorLocalPosition)
    {
        Rigidbody rigidbody = parent.GetComponent<Rigidbody>();
        if(rigidbody == null)
            rigidbody = parent.AddComponent<Rigidbody>();

        Rigidbody childRb = child.GetComponent<Rigidbody>();
        if(childRb == null){
            childRb = child.AddComponent<Rigidbody>();
        }
        childRb.drag = 1f;
        childRb.mass = .001f;
        joint.connectedBody = childRb;
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = new Vector3(0, 1, 0);
        joint.connectedAnchor = new Vector3(0,-1,0);
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
        jointLimit.limit = 60; // 5 degrees limit
        joint.angularYLimit = jointLimit;
        joint.angularZLimit = jointLimit;

        // Set rotation drive mode YZ
        JointDrive drive = new JointDrive();
        drive.positionSpring = 400f; // Small spring force
        drive.positionDamper = 400f; // Small damper
        drive.maximumForce = 100;
        joint.rotationDriveMode = RotationDriveMode.Slerp;
        joint.slerpDrive = drive;

        return joint;
    }

    public override Mesh MakeSelfMesh(float time)
    {
        UpdateSkeleton(time);
        return null;
        /*
        List<Line> lines = new List<Line>();
        for(int i = 0;i<verticalSamples;i++)
        {
            float verticalTime = i / (verticalSamples - 1f);
            Vector3 center = GetRelativePosition(time, verticalTime);
            float radius = thicknessCurve.Evaluate(time * verticalTime);
            List<Vector3> points = new List<Vector3>();
            for(int j = 0;j<horizontalSamples;j++)
            {
                float horizontalTime = j / (horizontalSamples - 1f);
                float theta = Mathf.PI * 2 * horizontalTime;
                float x = Mathf.Sin(theta) * radius;
                float z = Mathf.Cos(theta) * radius;
                points.Add(center + new Vector3(x,0,z));
            }
            lines.Add(new Line(){points = points});
        }
        Mesh mesh = new Face(){lines = lines, pointsPerLine = horizontalSamples}.MakeMesh();
        MeshUtils.Flip(mesh);
        return mesh;*/
    }
}