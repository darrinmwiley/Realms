using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Particle
{
    static int maxID = 0;
    public Vector3 position;
    public Vector3 previous;
    public Vector3 acceleration;
    public float radius;
    public int id;
    public GameObject gameObject;
    SphereCollider collider;
    MeshRenderer meshRenderer;
    public Rigidbody rb;

    public List<Vector3> forces = new List<Vector3>();

    public Particle(Vector3 pos, float radius = 0)
    {
        position = pos;
        previous = pos;
        acceleration = Vector2.zero;
        this.radius = radius;
        
        // Create a primitive sphere
        gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        gameObject.AddComponent<ParticleMono>().particle = this;;
        gameObject.name = "particle";

        // Get the MeshRenderer component
        meshRenderer = gameObject.GetComponent<MeshRenderer>();

        rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.mass = 1f; // Set a default mass

        // Set the initial position and scale
        gameObject.transform.position = position;
        gameObject.transform.localScale = new Vector3(radius * 2, radius * 2, radius * 2);
        // Get the SphereCollider component and set the radius
        collider = gameObject.GetComponent<SphereCollider>();
        collider.radius = radius / 2f;

        id = maxID++;
    }

    public void AddForce(Vector3 f)
    {
        forces.Add(f);
        acceleration += f;
    }

    public void ClearForces(){
        acceleration = Vector3.zero;
        forces.Clear();
    }

    public void SetRadius(float r)
    {
        this.radius = r;
        collider.radius = r;
        gameObject.transform.localScale = new Vector3(r * 2, r * 2, r * 2); // Adjust the scale to match the radius
    }

    public void SetPosition(Vector3 location)
    {
        position = location;
        gameObject.transform.position = location;
    }

    public void SetMaterial(Material material)
    {
        meshRenderer.material = material;
    }
}
