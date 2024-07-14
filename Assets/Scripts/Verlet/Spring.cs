using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spring
{
    public Particle particle;
    public Vector3 anchorPoint;
    public float restLength;
    public float stiffness;
    public float damping;
    public bool shouldRender;
    private GameObject lineObject;
    private LineRenderer lineRenderer;

    public Spring(Particle particle, Vector3 anchorPoint, float restLength, float stiffness, float damping, bool shouldRender = false)
    {
        this.particle = particle;
        this.anchorPoint = anchorPoint;
        this.restLength = restLength;
        this.stiffness = stiffness;
        this.damping = damping;
        this.shouldRender = shouldRender;

        if (shouldRender)
        {
            InitializeLineRenderer();
        }
    }

    public Spring(Particle particle, Vector3 anchorPoint, float stiffness, float damping, bool shouldRender = false)
    {
        this.particle = particle;
        this.anchorPoint = anchorPoint;
        this.restLength = Vector3.Distance(particle.position, anchorPoint);
        this.stiffness = stiffness;
        this.damping = damping;
        this.shouldRender = shouldRender;

        if (shouldRender)
        {
            InitializeLineRenderer();
        }
    }

    private void InitializeLineRenderer()
    {
        lineObject = new GameObject("Spring Line");
        lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.positionCount = 2;
    }

    public void ApplyForce()
    {
        Vector3 delta = particle.position - anchorPoint;
        float distance = delta.magnitude;
        float displacement = restLength - distance;

        Vector3 force = stiffness * displacement * delta.normalized;

        // Damping force
        Vector3 relativeVelocity = (particle.position - particle.previous) / Time.fixedDeltaTime;
        force += -damping * relativeVelocity;

        particle.AddForce(force / particle.rb.mass);

        if (shouldRender)
        {
            RenderLine();
        }
    }

    private void RenderLine()
    {
        lineRenderer.SetPosition(0, particle.position);
        lineRenderer.SetPosition(1, anchorPoint);

        float happiness = Mathf.Clamp01(Mathf.Abs((particle.position - anchorPoint).magnitude - restLength) / restLength);
        Color springColor = Color.Lerp(Color.blue, Color.red, happiness);
        lineRenderer.startColor = springColor;
        lineRenderer.endColor = springColor;
    }
}
