using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BallJoint : MonoBehaviour
{
    GameObject root;
    public GameObject ball;

    public bool immovable = true;

    // next: integrate this with vertex and segment code, except rework the jointing to fix the angle delta between each root and ball
    //       and have another joint that wants to point straight with some give 
    public Vector3 targetPosition;
    public bool parentSpace;

    // Joint settings
    public float strength = 100f;
    public float damper = 10f;

    void Start()
    {
        // Create the root and ball spheres
        root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "root";
        root.transform.parent = transform;
        root.transform.localPosition = Vector3.zero;

        ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.transform.parent = root.transform;
        ball.name = "ball";
    }

    public void AddGrowthJoint(){
        GameObject growJointObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        growJointObj.transform.parent = ball.transform;
        growJointObj.transform.localPosition = Vector3.zero;
        RotateGameObjectTowards(growJointObj, new Vector3(0,1,0), true);
        GrowthJoint growJoint = growJointObj.AddComponent<GrowthJoint>();
        growJoint.parentBallJoint = this;
    }

    public void ConfigureJoint()
    {
        // Ensure `root` has a fixed ArticulationBody (if immovable)
        ArticulationBody rootArticulation = root.GetComponent<ArticulationBody>();
        if (rootArticulation == null)
        {
            rootArticulation = root.AddComponent<ArticulationBody>();
            rootArticulation.jointType = ArticulationJointType.FixedJoint;
            rootArticulation.immovable = immovable;
        }

        // Add ArticulationBody to `ball` and set initial rotation difference
        ArticulationBody ballArticulation = ball.GetComponent<ArticulationBody>();
        if (ballArticulation == null)
        {
            ballArticulation = ball.AddComponent<ArticulationBody>();
            ballArticulation.jointType = ArticulationJointType.FixedJoint;
        }

        // Set initial anchor rotation to maintain relative rotation
        //Quaternion initialRotation = Quaternion.Inverse(root.transform.rotation) * ball.transform.rotation;
        //ballArticulation.anchorRotation = initialRotation;
    }

    // Method to rotate the object so its Z-axis points at the target instantly
    // Method to rotate the object so its Z-axis points at the target instantly
    public void RotateGameObjectTowards(GameObject obj, Vector3 targetPosition, bool parentSpace)
    {
        Vector3 directionToTarget;

        if (parentSpace)
        {
            // Convert targetPosition from parent's local space to world space
            Vector3 targetPositionWorld = obj.transform.parent.TransformPoint(targetPosition);

            // Calculate the direction from the object's position to the target position in world space
            directionToTarget = (targetPositionWorld - obj.transform.position).normalized;
        }
        else
        {
            // Calculate the direction from the object's position to the target position in world space
            directionToTarget = (targetPosition - obj.transform.position).normalized;
        }

        // Use Quaternion.LookRotation to align the forward direction (Z-axis) to the target
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget) * Quaternion.Euler(90f, 0f, 0f);;

        // Set the new rotation instantly
        obj.transform.rotation = targetRotation;
    }

    void Update()
    {
        RotateGameObjectTowards(ball, targetPosition, parentSpace);
        //ball.transform.localRotation = Quaternion.LookRotation(targetPositionLocal, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
    }

}
