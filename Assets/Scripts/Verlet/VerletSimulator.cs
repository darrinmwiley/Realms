using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerletSimulator : MonoBehaviour
{
    public static VerletSimulator instance;
    public int substeps = 3;
    public float drag = 0.02f;
    void Awake() => instance = this;

    public Vector3 gravity = new Vector3(0, -9.8f, 0);

    Dictionary<int, Particle> particles = new Dictionary<int, Particle>();
    List<Spring> springs = new List<Spring>();
    List<FixedDistanceConstraint> fixedDistanceConstraints = new List<FixedDistanceConstraint>();
    List<FixedPositionConstraint> fixedPositionConstraints = new List<FixedPositionConstraint>();
    List<Joint> jointConstraints = new List<Joint>();

    public void RemoveParticle(int id)
    {
        particles.Remove(id);
    }

    public Particle AddParticle(Vector3 position, Quaternion rotation, float radius)
    {
        Particle p = CreateParticle(position, rotation, radius);
        particles.Add(p.id, p);
        return p;
    }

    public Particle CreateParticle(Vector3 position, Quaternion rotation, float radius)
    {
        Particle p = new Particle(position,rotation, radius);
        return p;
    }

    public Spring AddSpring(Particle particle, Vector3 anchorPoint, float stiffness, float damping, bool shouldRender = false)
    {
        Spring spring = new Spring(particle, anchorPoint, stiffness, damping, shouldRender);
        springs.Add(spring);
        return spring;
    }

    public FixedDistanceConstraint AddFixedDistanceConstraint(Particle p1, Particle p2, bool shouldRender = false)
    {
        FixedDistanceConstraint constraint = new FixedDistanceConstraint(p1, p2, shouldRender);
        fixedDistanceConstraints.Add(constraint);
        return constraint;
    }

    public FixedPositionConstraint AddFixedPositionConstraint(Particle particle, Vector3 fixedPosition)
    {
        FixedPositionConstraint constraint = new FixedPositionConstraint(particle, fixedPosition);
        fixedPositionConstraints.Add(constraint);
        return constraint;
    }

    public Joint AddJointConstraint(Particle parent, Particle child, bool shouldRender = true)
    {
        Joint constraint = new Joint(parent, child, 15f, shouldRender);
        jointConstraints.Add(constraint);
        return constraint;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime / substeps;
        for (int i = 0; i < substeps; i++)
        {
            AccumulateForces();
            Integrate(dt);
            SatisfyConstraints();
        }
    }

    void Integrate(float timestep)
    {
        foreach (Particle p in particles.Values)
        {
            Vector3 loc = p.position;
            Vector3 newPrev = loc;
            p.position = loc * (2 - drag) - p.previous * (1 - drag) + p.acceleration * timestep * timestep;
            p.previous = newPrev;
            p.ClearForces();
            p.gameObject.transform.position = p.position;
            p.gameObject.transform.rotation = p.rotation;
        }
    }

    void AccumulateForces()
    {
        foreach (Particle p in particles.Values)
        {
            p.AddForce(gravity);
        }

        foreach (Spring spring in springs)
        {
            spring.ApplyForce();
        }
        /*
        foreach (Joint j in jointConstraints){
            // Calculate the goal position based on the initial relative position
            Vector3 goalPosition = j.GetGoalWorldSpace();
            
            // Calculate the vectors forming the plane
            Vector3 parentToChild = j.child.position - j.parent.position;
            Vector3 parentToGoal = goalPosition - j.parent.position;
            
            // Find the normal of the plane formed by parent, child, and goal positions
            Vector3 planeNormal = Vector3.Cross(parentToChild, parentToGoal).normalized;
            
            // Project the goal position onto the plane
            Vector3 childToGoal = goalPosition - j.child.position;
            Vector3 projectedGoal = Vector3.ProjectOnPlane(childToGoal, planeNormal);
            
            // Apply the force to the child particle along the plane towards the projected goal position
            j.child.AddForce(projectedGoal.normalized * 5f);
            //j.child.gameObject.GetComponent<ParticleMono>().UpdateRenderers();
        }*/
    }

    void SatisfyConstraints()
    {
        // Apply fixed distance constraints
        foreach (FixedDistanceConstraint constraint in fixedDistanceConstraints)
        {
            constraint.ApplyConstraint();
        }

        // Apply fixed position constraints
        foreach (FixedPositionConstraint constraint in fixedPositionConstraints)
        {
            constraint.ApplyConstraint();
        }

        // Apply joint constraints
        foreach (Joint constraint in jointConstraints)
        {
            constraint.ApplyConstraint();
        }
    }
}