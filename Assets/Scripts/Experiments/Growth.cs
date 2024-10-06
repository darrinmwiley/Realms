using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//container class used for a combination of joints used to model plant growth
public class Growth
{
    public GameObject anchor;
    public GameObject offsetJoint;
    public GameObject bendJoint;
    public GameObject growJoint;
    public Vector3 direction;
    public bool parentSpace;
    public float flexibility;
    public float strength;
    public float magnitude;
    public float growTime;
    public bool growing;
    public float growStartTime;

    public bool showJoints = false;

    private int plantLayer;
    private LayerMask plantLayerMask;

    private float mass = .01f;

    public Growth(GameObject anchor, Vector3 direction, bool parentSpace, float flexibility, float strength, float magnitude, float growTime)
    {
        this.anchor = anchor;
        this.direction = direction;
        this.parentSpace = parentSpace;
        this.flexibility = flexibility;
        this.strength = strength;
        this.magnitude = magnitude;
        this.growTime = growTime;

        // Get the layer index for the "Plant" layer
        plantLayer = LayerMask.NameToLayer("Plant");
        plantLayerMask = 1 << plantLayer; // Create a LayerMask for the "Plant" layer
        anchor.layer = plantLayer;
    
        ConfigureOffsetJoint();
        ConfigureBendJoint();
        ConfigureGrowJoint();

        if(!showJoints){
            offsetJoint.GetComponent<MeshRenderer>().enabled = false;
            bendJoint.GetComponent<MeshRenderer>().enabled = false;
            growJoint.GetComponent<MeshRenderer>().enabled = false;
        }
    }

    public void ConfigureGrowJoint()
    {
        growJoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        growJoint.name = "Growth";
        growJoint.transform.parent = bendJoint.transform;
        growJoint.transform.localPosition = Vector3.zero;
        RotateGameObjectTowards(growJoint, Vector3.up, true);

        ArticulationBody growthArticulation = growJoint.AddComponent<ArticulationBody>();
        growthArticulation.jointType = ArticulationJointType.FixedJoint;
        growthArticulation.mass = mass;

        // Set layer and exclude collisions
        growJoint.layer = plantLayer;
        growthArticulation.excludeLayers = plantLayerMask;  // Exclude collisions with the "Plant" layer
    }

    public void ConfigureBendJoint()
    {
        bendJoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bendJoint.transform.parent = offsetJoint.transform;
        bendJoint.transform.localPosition = Vector3.zero;
        bendJoint.name = "Bendy Joint";
        RotateGameObjectTowards(bendJoint, Vector3.up, true);

        ArticulationBody bendArticulation = bendJoint.AddComponent<ArticulationBody>();
        bendArticulation.jointType = ArticulationJointType.SphericalJoint;
        bendArticulation.twistLock = ArticulationDofLock.LockedMotion;
        bendArticulation.swingYLock = ArticulationDofLock.LimitedMotion;
        bendArticulation.swingZLock = ArticulationDofLock.LimitedMotion;
        bendArticulation.mass = mass;

        ArticulationDrive swingDrive = new ArticulationDrive
        {
            lowerLimit = -flexibility,
            upperLimit = flexibility,
            stiffness = strength,
            forceLimit = 10000
        };

        bendArticulation.yDrive = swingDrive;
        bendArticulation.zDrive = swingDrive;

        // Set layer and exclude collisions
        bendJoint.layer = plantLayer;
        bendArticulation.excludeLayers = plantLayerMask;  // Exclude collisions with the "Plant" layer
    }

    public void ConfigureOffsetJoint()
    {
        offsetJoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        offsetJoint.transform.parent = anchor.transform;
        offsetJoint.transform.localPosition = Vector3.zero;
        offsetJoint.name = "Offset Joint";
        RotateGameObjectTowards(offsetJoint, direction, parentSpace);

        ArticulationBody anchorArticulation = anchor.GetComponent<ArticulationBody>();
        if (anchorArticulation == null)
        {
            anchorArticulation = anchor.AddComponent<ArticulationBody>();
            anchorArticulation.jointType = ArticulationJointType.FixedJoint;
            anchorArticulation.excludeLayers = plantLayerMask;
        }

        ArticulationBody offsetArticulation = offsetJoint.AddComponent<ArticulationBody>();
        offsetArticulation.jointType = ArticulationJointType.FixedJoint;
        offsetArticulation.mass = mass;

        // Set layer and exclude collisions
        offsetJoint.layer = plantLayer;
        offsetArticulation.excludeLayers = plantLayerMask;  // Exclude collisions with the "Plant" layer
    }

    public void RotateGameObjectTowards(GameObject obj, Vector3 direction, bool parentSpace)
    {
        Vector3 directionToTarget = direction.normalized ;

        if (parentSpace)
        {
            Vector3 directionWorld = obj.transform.parent.TransformPoint(direction);
            directionToTarget = (directionWorld - obj.transform.position).normalized;
        }

        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget) * Quaternion.Euler(90f, 0f, 0f);
        obj.transform.rotation = targetRotation;
    }

    public void SetGrowth(float growthPercentage)
    {
        Vector3 growthOffset = Vector3.up * magnitude * growthPercentage;

        growJoint.transform.localPosition = growthOffset;

        growJoint.GetComponent<ArticulationBody>().anchorPosition = -growthOffset;
    }
}
