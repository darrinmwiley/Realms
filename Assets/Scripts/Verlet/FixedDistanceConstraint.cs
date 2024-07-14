using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixedDistanceConstraint
{
    Particle p1;
    Particle p2;
    float distance;
    public bool shouldRender = false;
    private GameObject lineObject;
    private LineRenderer lineRenderer;

    public FixedDistanceConstraint(Particle p1, Particle p2, bool shouldRender = false)
    {
        this.p1 = p1;
        this.p2 = p2;
        this.distance = (p1.position - p2.position).magnitude;
        this.shouldRender = shouldRender;

        if (shouldRender)
        {
            InitializeLineRenderer();
        }
    }

    private void InitializeLineRenderer()
    {
        lineObject = new GameObject("Fixed Distance Line");
        lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.positionCount = 2;
        lineRenderer.startColor = Color.black;
        lineRenderer.endColor = Color.black;
    }

    public void ApplyConstraint()
    {
        Vector3 delta = p1.position - p2.position;
        float currentDistance = delta.magnitude;
        float difference = (currentDistance - distance) / currentDistance;
        Vector3 correction = delta * difference * 0.5f;

        p1.position -= correction;
        p2.position += correction;

        if (shouldRender)
        {
            RenderLine();
        }
    }

    private void RenderLine()
    {
        lineRenderer.SetPosition(0, p1.position);
        lineRenderer.SetPosition(1, p2.position);
    }
}
