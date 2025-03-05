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
    public GameObject cylinder;
    public Vector3 direction;
    public bool parentSpace;
    public float flexibility;
    public float strength;
    public float magnitude;
    public bool showJoints = false;

    private int plantLayer;
    private LayerMask plantLayerMask;

    private float mass = .1f;
    private float scale = .2f;

    private bool useGravity;

    public Growth(GameObject anchor, Vector3 direction, bool parentSpace, float flexibility, float strength, float magnitude, bool useGravity = true)
    {
        Debug.Log(magnitude);
        this.anchor = anchor;
        this.direction = direction;
        this.parentSpace = parentSpace;
        this.flexibility = flexibility;
        this.strength = strength;
        this.magnitude = magnitude;
        this.useGravity = useGravity;

        // Get the layer index for the "Plant" layer
        plantLayer = LayerMask.NameToLayer("Plant");
        plantLayerMask = 1 << plantLayer; // Create a LayerMask for the "Plant" layer
        anchor.layer = plantLayer;
    
        ConfigureOffsetJoint();
        ConfigureBendJoint();
        ConfigureGrowJoint();
        ConfigureCylinder();

        if(!showJoints){
            offsetJoint.GetComponent<MeshRenderer>().enabled = false;
            bendJoint.GetComponent<MeshRenderer>().enabled = false;
            growJoint.GetComponent<MeshRenderer>().enabled = false;
            cylinder.GetComponent<MeshRenderer>().enabled = false;
        }
        SetGrowth(0);
    }

    public void ConfigureGrowJoint()
    {
        growJoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        growJoint.name = "Growth";
        growJoint.transform.parent = bendJoint.transform;
        growJoint.transform.localPosition = Vector3.zero;
        growJoint.transform.localScale = Vector3.one;
        RotateGameObjectTowards(growJoint, Vector3.up, true);

        ArticulationBody growthArticulation = growJoint.AddComponent<ArticulationBody>();
        growthArticulation.jointType = ArticulationJointType.FixedJoint;
        growthArticulation.mass = mass;
        growthArticulation.useGravity = useGravity;

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
        bendJoint.transform.localScale = Vector3.one;
        RotateGameObjectTowards(bendJoint, Vector3.up, true);

        ArticulationBody bendArticulation = bendJoint.AddComponent<ArticulationBody>();
        bendArticulation.jointType = ArticulationJointType.SphericalJoint;
        bendArticulation.twistLock = ArticulationDofLock.LockedMotion;
        bendArticulation.swingYLock = ArticulationDofLock.LimitedMotion;
        bendArticulation.swingZLock = ArticulationDofLock.LimitedMotion;
        bendArticulation.linearDamping = 5;
        bendArticulation.angularDamping = 5;
        bendArticulation.mass = mass;
        bendArticulation.useGravity = useGravity;

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
        offsetJoint.transform.localScale = Vector3.one;
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
        offsetArticulation.useGravity = useGravity;

        // Set layer and exclude collisions
        offsetJoint.layer = plantLayer;
        offsetArticulation.excludeLayers = plantLayerMask;  // Exclude collisions with the "Plant" layer
    }

    public void ConfigureCylinder()
    {
        cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.transform.parent = bendJoint.transform;
        cylinder.transform.localRotation = Quaternion.Euler(Vector3.zero);
        //RotateGameObjectTowards(cylinder, Vector3.up, true);
        cylinder.transform.localPosition = Vector3.zero;
        cylinder.name = "Cylinder";
        
        
        ArticulationBody cylinderArticulation = cylinder.AddComponent<ArticulationBody>();
        cylinderArticulation.jointType = ArticulationJointType.FixedJoint;
        cylinderArticulation.mass = mass;
        cylinderArticulation.anchorPosition = new Vector3(0,-1, 0);
        cylinderArticulation.useGravity = useGravity;

        // Set layer and exclude collisions
        cylinder.layer = plantLayer;
        //cylinder.GetComponent<Collider>().enabled = false;
        cylinderArticulation.excludeLayers = plantLayerMask;  // Exclude collisions with the "Plant" layer
    }

    public void RotateGameObjectTowards(GameObject obj, Vector3 direction, bool parentSpace)
    {
        Vector3 directionToTarget = direction.normalized;

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

        cylinder.transform.localScale = new Vector3(1,magnitude * growthPercentage / 2, 1);
        cylinder.transform.localPosition = new Vector3(0, magnitude * growthPercentage / 2, 0);
        cylinder.GetComponent<ArticulationBody>().anchorPosition = new Vector3(0,-magnitude * growthPercentage / 2,0);

        growJoint.transform.localPosition = growthOffset;

        growJoint.GetComponent<ArticulationBody>().anchorPosition = -growthOffset;


    }
}
