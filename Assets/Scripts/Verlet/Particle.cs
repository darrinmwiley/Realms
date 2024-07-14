using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Particle
{
    static int maxID = 0;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 previous;
    public Vector3 acceleration;
    public float radius;
    public int id;
    //public GameObject gameObject;
    SphereCollider collider;
    MeshRenderer meshRenderer;
    public Rigidbody rb;

    public List<Vector3> forces = new List<Vector3>();

    public Particle(Vector3 pos, Quaternion rotation, float radius = 0)
    {
        this.rotation = rotation;
        position = pos;
        previous = pos;
        acceleration = Vector2.zero;
        this.radius = radius;
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
    }

    public void SetPosition(Vector3 location)
    {
        position = location;
        //gameObject.transform.position = location;
    }

    public void SetMaterial(Material material)
    {
        meshRenderer.material = material;
    }

    public Vector3 TransformPoint(Vector3 point)
    {
        return position + rotation * point;
    }

    public Vector3 InverseTransformPoint(Vector3 point)
    {
        return Quaternion.Inverse(rotation) * (point - position);
    }
}
