using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public class Vertex{

        public static float bendiness = 15f; // Controls the max angle of the joints
        public static float stiffness = 50f; // Controls the stiffness of the joint

        public GameObject gameObject;
        public Vertex parent;
        
        // this doesn't have to be a unit vector, 
        // just some relative direction vector in which the next vertex will reach at 100% growth
        public Vector3 direction;
        public float magnitude;
        public float growth;

        //root constructor
        public Vertex(Vector3 location, bool immovable = true){
            growth = 0;
            this.direction = direction;
            gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gameObject.GetComponent<MeshRenderer>().enabled = false;
            gameObject.transform.position = location;
            gameObject.transform.localScale = new Vector3(1f,1f,1f);
            ArticulationBody articulationBody = gameObject.AddComponent<ArticulationBody>();
            articulationBody.matchAnchors = false;
            articulationBody.mass = 0.01f;
            articulationBody.jointType = ArticulationJointType.SphericalJoint;
            //articulationBody.jointType = ArticulationJointType.FixedJoint;
            articulationBody.immovable = immovable;
        }

        public void SetGrowth(float growthPercentage)
        {
            growth = growthPercentage;
            gameObject.GetComponent<ArticulationBody>().anchorPosition = -direction * growthPercentage * magnitude;
            gameObject.transform.localPosition = direction * growthPercentage * magnitude;
        }

        //child constructor
        public Vertex(Vertex parent, Vector3 direction, float magnitude, bool immovable = false, bool isFixed = false){
            growth = 0;
            this.magnitude = magnitude;
            this.direction = direction;
            gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gameObject.GetComponent<MeshRenderer>().enabled = false;
            gameObject.transform.localScale = new Vector3(1f,1f,1f);
            ArticulationBody articulationBody = gameObject.AddComponent<ArticulationBody>();
            articulationBody.matchAnchors = false;
            articulationBody.mass = 0.01f;
            articulationBody.jointType = ArticulationJointType.SphericalJoint;
            if(isFixed){
                articulationBody.jointType = ArticulationJointType.FixedJoint;
            }
            articulationBody.immovable = immovable;
            if (parent != null)
            {
                gameObject.transform.parent = parent.gameObject.transform;

                // Calculate a perpendicular vector to 'direction' using cross product
                Vector3 referenceAxis = Vector3.up;  // Can use Vector3.up, or any other vector not parallel to 'direction'
                if (Mathf.Abs(Vector3.Dot(direction.normalized, Vector3.up)) > 0.99f)
                {
                    // If 'direction' is close to 'up', use another reference axis like Vector3.forward
                    referenceAxis = Vector3.forward;
                }
                Vector3 perpendicular = Vector3.Cross(direction.normalized, referenceAxis).normalized;

                // Set the anchor position at the center of the sphere
                articulationBody.anchorPosition = Vector3.zero;

                // Create a quaternion that aligns the up direction with the 'direction' vector
                // and the forward direction with the calculated perpendicular vector
                articulationBody.anchorRotation = Quaternion.LookRotation(perpendicular, direction);

                // Set the parent's anchor rotation to match the child's anchor rotation
                articulationBody.parentAnchorRotation = articulationBody.anchorRotation;
                
                // Set drive properties for the joint
                ArticulationDrive xDrive = articulationBody.xDrive;
                xDrive.stiffness = stiffness;
                //xDrive.damping = bendiness;
                xDrive.lowerLimit = -bendiness;
                xDrive.upperLimit = bendiness;
                articulationBody.xDrive = xDrive;

                ArticulationDrive yDrive = articulationBody.yDrive;
                yDrive.stiffness = stiffness;
                //yDrive.damping = bendiness;
                yDrive.lowerLimit = -bendiness;
                yDrive.upperLimit = bendiness;
                articulationBody.yDrive = yDrive;

                ArticulationDrive zDrive = articulationBody.zDrive;
                zDrive.stiffness = stiffness;
                //zDrive.damping = bendiness;
                zDrive.lowerLimit = -bendiness;
                zDrive.upperLimit = bendiness;
                articulationBody.zDrive = zDrive;

                // Limit the swing in Y and Z axes
                articulationBody.swingYLock = ArticulationDofLock.LimitedMotion;
                articulationBody.swingZLock = ArticulationDofLock.LimitedMotion;
                articulationBody.twistLock = ArticulationDofLock.LockedMotion;
            }
        }
    }
