using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleMono : MonoBehaviour
{
    public bool showNetForce;
    public bool showAllForces;

    public Particle particle;
    private List<LineRenderer> forceLineRenderers = new List<LineRenderer>();
    private LineRenderer netForceLineRenderer;

    void Start()
    {
        // Initialize particle if necessary
        if (particle == null)
        {
            particle = new Particle(transform.position, 1f); // Example radius
        }

        // Initialize net force line renderer
        netForceLineRenderer = CreateLineRenderer(Color.red);
    }

    public void UpdateRenderers()
    {
        // Sync the particle position with the GameObject's position
        //particle.position = transform.position;

        //Update force line renderers
        if (showAllForces)
        {
            UpdateForceLineRenderers();
        }

        if (showNetForce)
        {
            UpdateNetForceLineRenderer();
        }
    }

    public void UpdateForceLineRenderers()
    {
        // Clear existing line renderers
        foreach (var lineRenderer in forceLineRenderers)
        {
            Destroy(lineRenderer.gameObject);
        }
        forceLineRenderers.Clear();

        // Create new line renderers for each force
        foreach (Vector3 force in particle.forces)
        {
            var lineRenderer = CreateLineRenderer(Color.blue);
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, transform.position + force);
            forceLineRenderers.Add(lineRenderer);
        }   
    }

    private void UpdateNetForceLineRenderer()
    {
        Vector3 netForce = Vector3.zero;
        foreach (Vector3 force in particle.forces)
        {
            netForce += force;
        }
        netForceLineRenderer.SetPosition(0, transform.position);
        netForceLineRenderer.SetPosition(1, transform.position + netForce);
    }

    private LineRenderer CreateLineRenderer(Color color)
    {
        GameObject lineObj = new GameObject("ForceLine");
        LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.positionCount = 2;
        return lineRenderer;
    }
}