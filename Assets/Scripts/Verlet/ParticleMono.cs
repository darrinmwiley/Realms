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
    private SphereCollider sphereCollider;
    private Renderer sphereRenderer;

    void Start()
    {
        // Initialize particle if necessary
        if (particle == null)
        {
            particle = new Particle(transform.position, Quaternion.identity, 1f); // Example radius
        }

        // Initialize net force line renderer
        netForceLineRenderer = CreateLineRenderer(Color.red);

        // Initialize and configure the spherical collider
        sphereCollider = gameObject.AddComponent<SphereCollider>();
        sphereCollider.radius = particle.radius;
        sphereCollider.isTrigger = true; // Make it a trigger collider if necessary

        // Initialize and configure the renderer
        sphereRenderer = gameObject.AddComponent<MeshRenderer>();
        gameObject.AddComponent<MeshFilter>().mesh = CreateSphereMesh(particle.radius);
    }

    void Update()
    {
        UpdateRenderers();
        gameObject.transform.position = particle.position;
        gameObject.transform.rotation = particle.rotation;
    }

    public void UpdateRenderers()
    {
        // Sync the particle position and rotation with the GameObject's position and rotation
        transform.position = particle.position;
        transform.rotation = particle.rotation;

        // Update force line renderers
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

    private Mesh CreateSphereMesh(float radius)
    {
        // Create a simple sphere mesh
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Mesh mesh = sphere.GetComponent<MeshFilter>().mesh;
        Destroy(sphere); // Destroy the temporary sphere
        return mesh;
    }
}
