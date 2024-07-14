using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CapsuleChain : MonoBehaviour
{
    public int numSegments = 10;
    private GameObject[] capsules;
    public Spline spline; // Assuming you have a Spline class that you can use to evaluate positions along the spline
    public float xzFlexibility;

    void Start()
    {
        spline = MakeSpline(1);
        CreateCapsuleChain();
    }

    public Spline MakeSpline(float length)
    {
        float noise = .3f;
        int numPoints = 10;
        List<Vector3> controlPoints = new List<Vector3>();
        controlPoints.Add(new Vector3(0, 0, 0));
        controlPoints.Add(new Vector3(0, numSegments, 0));
        /*float dh = length / (numPoints - 1f);
        for(int i = 1;i<numPoints;i++)
        {
            float xDeviation = Random.Range(-noise, noise);
            float zDeviation = Random.Range(-noise, noise);
            controlPoints.Add(new Vector3(controlPoints[i - 1].x + xDeviation * dh, i * dh, controlPoints[i-1].z + zDeviation * dh));
        }*/
        return new CatmullRomSpline(controlPoints, 50);
    }

    void CreateCapsuleChain()
    {
        capsules = new GameObject[numSegments];
        float dt = 1f / (numSegments);

        for (int i = 0; i < numSegments; i++)
        {
            capsules[i] = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsules[i].GetComponent<Collider>().enabled = false;
            capsules[i].transform.parent = transform;

            Rigidbody rb = capsules[i].AddComponent<Rigidbody>();
            if(i == 0)
            {
                rb.isKinematic = true;
            }
            rb.mass = 1f;
            rb.drag = 0.5f;

            // Calculate the start and end positions for the capsule
            Vector3 start = spline.Evaluate(i * dt);
            Vector3 end = spline.Evaluate((i + 1) * dt);
            Debug.Log(i+" "+start+" "+end);

            // Calculate the direction from start to end
            Vector3 direction = end - start;

            // Position the capsule at the midpoint
            capsules[i].transform.localPosition = start + direction / 2;

            // Align the capsule's up direction with the direction from start to end
            capsules[i].transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction);

            // Adjusting the scale to match half the length of the segment
            float length = direction.magnitude / 2;
            Vector3 scale = capsules[i].transform.localScale;
            scale.y = length; // Assuming the capsule's length is along the y-axis
            capsules[i].transform.localScale = scale;

            // Add a ConfigurableJoint to connect the capsules
            if (i > 0)
            {
                ConfigurableJoint joint = capsules[i].AddComponent<ConfigurableJoint>();
                joint.connectedBody = capsules[i - 1].GetComponent<Rigidbody>();
                joint.autoConfigureConnectedAnchor = false;
                joint.anchor = new Vector3(0, -1, 0);
                joint.connectedAnchor = new Vector3(0, 1, 0);

                joint.xMotion = ConfigurableJointMotion.Locked;
                joint.yMotion = ConfigurableJointMotion.Locked;
                joint.zMotion = ConfigurableJointMotion.Locked;
                joint.angularXMotion = ConfigurableJointMotion.Limited;
                joint.angularYMotion = ConfigurableJointMotion.Locked;
                joint.angularZMotion = ConfigurableJointMotion.Limited;

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
        }
    }

    void Update()
    {
        // No need to update the capsule chain since it is initialized in Start
    }
}
