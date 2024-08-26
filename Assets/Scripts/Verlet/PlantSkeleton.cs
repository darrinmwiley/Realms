using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantSkeleton : MonoBehaviour
{
    List<Vert> verts;

    public void Start()
    {
        verts = new List<Vert>();
        int numVerts = 50;
        for (int i = 0; i < numVerts; i++)
        {
            Vert parent = null;
            Vector3 location = Vector3.zero;
            if (i != 0)
            {
                parent = verts[i - 1];

                // Pick a "direction" angle that is mostly upward but up to 15 degrees in x or z as well
                float maxAngle = 5f;
                float angleX = Random.Range(-maxAngle, maxAngle);
                float angleZ = Random.Range(-maxAngle, maxAngle);

                // Create a rotation from the angles
                Quaternion rotation = Quaternion.Euler(angleX, 0, angleZ);

                // Apply the rotation to the upward direction
                Vector3 direction = parent.particle.rotation * rotation * Vector3.up;

                // Ensure the direction is normalized
                direction.Normalize();

                // Set the location with the calculated direction
                location = verts[i - 1].particle.position + direction;
            }
            AddVert(location, parent);
        }

        int numBranches = 0;
        int branchLength = 15;
        for (int i = 0; i < numBranches; i++)
        {
            // Pick a random Vert to branch off from
            Vert branchStart = verts[Random.Range(30, numVerts - 1)];
            Vert parent = branchStart;
            for (int j = 0; j < branchLength; j++)
            {
                Vector3 location = parent.particle.position;

                // Generate a random angle for the branch direction
                float angleY = 0;
                float angleX = Random.Range(-15f, 15f);
                float angleZ = Random.Range(-15f, 15f);
                if(j == 0){
                    angleY = Random.Range(-180f, 180f);
                    angleZ = Random.Range(45f, 90f);
                }
                // Create a rotation from the angles
                Quaternion rotation = Quaternion.Euler(angleX, angleY, angleZ);

                // Apply the rotation to the parent's up direction
                Vector3 direction = parent.particle.rotation * rotation * Vector3.up;

                // Ensure the direction is normalized
                direction.Normalize();

                // Set the location with the calculated direction
                location += direction;

                // Add the new Vert
                parent = AddVert(location, parent);
            }
        }

        VerletSimulator.instance.AddFixedPositionConstraint(verts[0].particle, verts[0].particle.position);
    }

    public Vert AddVert(Vector3 location, Vert parent)
    {
        Vert newVert = new Vert(location, parent);
        verts.Add(newVert);
        return newVert;
    }

    public class Vert
    {
        static int maxId = 0;
        public Particle particle;
        public Vert parent;
        public List<Vert> children;

        public Vert(Vector3 position, Vert parent)
        {
            Quaternion rotation = Quaternion.identity;
            if (parent != null)
            {
                Vector3 direction = (position - parent.particle.position).normalized;

                // Create a quaternion that aligns the parent's up direction with the calculated direction
                Vector3 parentUp = parent.particle.rotation * Vector3.up;
                rotation = Quaternion.FromToRotation(parentUp, direction);
            }
            particle = VerletSimulator.instance.AddParticle(position, rotation, .1f);
            this.parent = parent;
            this.children = new List<Vert>();
            if (parent != null)
            {
                parent.children.Add(this);
                VerletSimulator.instance.AddJointConstraint(parent.particle, particle);
            }
        }
    }
}
