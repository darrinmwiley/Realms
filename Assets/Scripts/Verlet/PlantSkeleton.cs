using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantSkeleton : MonoBehaviour
{
    List<Vert> verts;

    public void Start(){
        verts = new List<Vert>();
        int numVerts = 8;
        for(int i = 0;i<numVerts;i++)
        {
            Vert parent = null;
            Vector3 location = Vector3.zero;
            if(i != 0){
                parent = verts[i - 1];

                // TODO: pick a "direction" angle that is mostly upward but up to 15 degrees in x or z as well,
                // and then add that normalized direction instead of pure up
                float maxAngle = 15f;
                float angleX = 0;//Random.Range(-maxAngle, maxAngle);
                float angleZ = 0;//Random.Range(-maxAngle, maxAngle);

                // Create a rotation from the angles
                Quaternion rotation = Quaternion.Euler(angleX, 0, angleZ);

                // Apply the rotation to the upward direction
                Vector3 direction = rotation * Vector3.up;

                // Ensure the direction is normalized
                direction.Normalize();

                // Set the location with the calculated direction
                location = verts[i - 1].particle.position + direction;
            }
            AddVert(location, parent);
        }
        /*int numBranches = 5;
        int branchLength = 5;
        for(int i = 0;i<numBranches;i++)
        {
            //for each branch, pick
        }*/
        VerletSimulator.instance.AddFixedPositionConstraint(verts[0].particle, verts[0].particle.position);
    }

    public Vert AddVert(Vector3 location, Vert parent)
    {
        Vert newVert = new Vert(location, parent);
        verts.Add(newVert);
        return newVert;
    }

    public class Vert{
        static int maxId = 0;
        public Particle particle;
        public Vert parent;
        public List<Vert> children;

        public Vert(Vector3 position, Vert parent)
        {
            Quaternion rotation = Quaternion.identity;
            if(parent != null){
                Vector3 direction = (position - parent.particle.position).normalized;
                //rotation = Quaternion.LookRotation(direction);
            }
            particle = VerletSimulator.instance.AddParticle(position, rotation, .1f);
            this.parent = parent;
            this.children = new List<Vert>();
            if(parent != null)
            {
                parent.children.Add(this);
                VerletSimulator.instance.AddJointConstraint(parent.particle, particle);
            }
        }
    }

    
}
