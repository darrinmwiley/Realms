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
    Quaternion initialChildRotation;
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
        this.initialChildPositionParentSpace = parent.InverseTransformPoint(child.position);
        this.initialChildRotation= child.rotation;
        this.initialDistance = Vector3.Distance(parent.position, child.position);
        this.maxAngle = maxAngle;
        this.shouldRender = shouldRender;

        if (shouldRender)
        {
            InitializeLineRenderer();
            //InitializeGoalPositionSphere();
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

        //goalPositionSphere.transform.position = GetGoalWorldSpace();

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
            //goalPositionSphere.transform.position = GetGoalWorldSpace();
        }

        toChild = (child.position - parent.position).normalized;
        toGoal = (GetGoalWorldSpace() - parent.position).normalized;
        angle = Vector3.Angle(toChild, toGoal);
        child.rotation = initialChildRotation * parent.rotation * Quaternion.AngleAxis(angle, Vector3.Cross(toGoal, toChild));

        if (shouldRender)
        {
            RenderLine();
        }
    }

    public Vector3 GetGoalWorldSpace(){
        return parent.TransformPoint(initialChildPositionParentSpace);
    }

    private void RenderLine()
    {
        lineRenderer.SetPosition(0, parent.position);
        lineRenderer.SetPosition(1, child.position);
    }
}
