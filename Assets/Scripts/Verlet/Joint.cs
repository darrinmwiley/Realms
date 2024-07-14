using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Joint
{
    public Particle parent;
    public Particle child;
    public Vector3 initialChildPosition;
    public float initialDistance;
    public Vector3 initialChildPositionParentSpace;
    public float maxAngle;
    public bool shouldRender = false;
    private GameObject lineObject;
    private LineRenderer lineRenderer;
    private GameObject goalPositionSphere;

    public Joint(Particle parent, Particle child, float maxAngle, bool shouldRender = false)
    {
        this.parent = parent;
        this.child = child;
        this.initialChildPosition = child.position;
        this.initialChildPositionParentSpace = parent.gameObject.transform.InverseTransformPoint(child.position);
        Debug.Log(initialChildPositionParentSpace);
        //this.initialChildRotationParentSpace = Quaternion.Inverse(parentObject.rotation) * childWorldRotation;
        this.initialDistance = Vector3.Distance(parent.position, child.position);
        this.maxAngle = maxAngle;
        this.shouldRender = shouldRender;

        if (shouldRender)
        {
            InitializeLineRenderer();
            InitializeGoalPositionSphere();
        }
    }

    private void InitializeLineRenderer()
    {
        lineObject = new GameObject("Joint Line");
        lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.positionCount = 2;
        lineRenderer.startColor = Color.black;
        lineRenderer.endColor = Color.black;
    }

    private void InitializeGoalPositionSphere()
    {
        goalPositionSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        goalPositionSphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f); // Small sphere
        goalPositionSphere.GetComponent<Renderer>().material.color = Color.green; // Color it green for visibility
        goalPositionSphere.name = "Goal Position Sphere";
    }

    public void ApplyConstraint()
    {
        //make sure the child is correct distance 

        Vector3 delta = parent.position - child.position;
        float currentDistance = delta.magnitude;
        float difference = (currentDistance - initialDistance) / currentDistance;
        Vector3 correction = delta * difference;

        //option - try and make parent authoritative
        //parent.position -= correction;
        child.position += correction;

        goalPositionSphere.transform.position = GetGoalWorldSpace();

        //todo: if angle between child -> parent -> goal is > maxAngle, then rotate child back on that plane by the diff.
        Vector3 toChild = (child.position - parent.position).normalized;
        Vector3 toGoal = (GetGoalWorldSpace() - parent.position).normalized;
        float angle = Vector3.Angle(toChild, toGoal);
        if (angle > maxAngle)
        {
            Vector3 axisOfRotation = Vector3.Cross(toChild, toGoal);
            Quaternion rotationCorrection = Quaternion.AngleAxis(angle - maxAngle, axisOfRotation);
            Vector3 correctedPosition = rotationCorrection * (child.position - parent.position) + parent.position;
            child.position = correctedPosition;
            goalPositionSphere.transform.position = GetGoalWorldSpace();
        }

        toChild = (child.position - parent.position).normalized;
        toGoal = (GetGoalWorldSpace() - parent.position).normalized;
        angle = Vector3.Angle(toChild, toGoal);
        child.gameObject.transform.rotation = parent.gameObject.transform.rotation * Quaternion.AngleAxis(angle, Vector3.Cross(toGoal, toChild));

        if (shouldRender)
        {
            RenderLine();
        }
    }

    public Vector3 GetGoalWorldSpace(){
        return parent.gameObject.transform.TransformPoint(initialChildPositionParentSpace);
    }

    private Quaternion getQuaternionBetween(Vector3 p1, Vector3 p2, Vector3 axisOfRotation)
    {
        // Normalize the axis of rotation
        Vector3 axis = axisOfRotation.normalized;

        // Calculate the vector from p1 to p2
        Vector3 v1 = p1.normalized;
        Vector3 v2 = p2.normalized;

        // Calculate the angle between the vectors
        float dotProduct = Vector3.Dot(v1, v2);
        float angle = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;

        // Create the quaternion representing the rotation
        Quaternion rotation = Quaternion.AngleAxis(angle, axis);

        return rotation;
    }

    private void RenderLine()
    {
        lineRenderer.SetPosition(0, parent.position);
        lineRenderer.SetPosition(1, child.position);
    }
}
