using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallJoint : MonoBehaviour
{
    GameObject root;
    GameObject ball;

    bool immovable;

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

        // Debugging the direction
        Debug.Log(directionToTarget);

        // Use Quaternion.LookRotation to align the forward direction (Z-axis) to the target
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

        // Set the new rotation instantly
        obj.transform.rotation = targetRotation;
    }

    void Update()
    {
        RotateGameObjectTowards(ball, targetPosition, parentSpace);
        //ball.transform.localRotation = Quaternion.LookRotation(targetPositionLocal, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
    }
}
