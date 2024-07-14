using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO: SEGMENTS SHOULD HAVE AN ABSOLUTE WIDTH
//TODO: SPLIT LONG SOCKET SEGMENT INTO MULTIPLE
//todo make this a prefab after unifying mesh

public class Node2{
    public Vector3 location;
    public bool used;
}


public class Segment : MonoBehaviour
{
    public float growTime;
    public float growStartTime = -1;
    public Vector3 start;
    public Vector3 end;
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    public bool isJointed;
    private bool addedJoint;

    public List<Segment> children = new List<Segment>();
    public Segment parent;
    //socket position in parent local space
    Vector3 socketPosition;

    public List<List<Node2>> rings = new List<List<Node2>>();
    public int distanceFromBridge = -1;

    public bool IsGrown(){
        return growStartTime != -1 && Time.time - growStartTime > growTime;
    }

    public void StartGrowth(){
        Resize(.01f);
        growStartTime = Time.time;
        meshRenderer.enabled = true;
    }

    public void Init(Material material, Vector3 startPoint){
        meshFilter = gameObject.GetComponent<MeshFilter>();
        if(meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if(meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = material;
        Mesh mesh = MakeMesh(.02f, .06f, 50);
        gameObject.AddComponent<MeshCollider>();
        meshFilter.mesh = mesh;
        meshRenderer.enabled = false;
        if(parent != null)
        {
            transform.parent = parent.transform;
            if(parent.distanceFromBridge != -1)
            {
                distanceFromBridge = parent.distanceFromBridge + 1;
            }
        }
    }

    void Update()
    {
        if(!IsGrown() && growStartTime != -1)
        {
            float scale = (Time.time - growStartTime) / growTime;
            Resize(scale);
        }
    }

    public Vector3 LocalTip(){
        return new Vector3(0,(end - start).magnitude, 0);
    }

    public Vector3 GetTip(){
        return transform.TransformPoint(LocalTip());
    }

    //idea: predefine a DF curve, resize recalculates the mesh for the first x % of the curve
    public void Resize(float scale)
    {
        transform.localScale = new Vector3(scale, scale, scale);
    }

    public void AddJoint(bool shouldParentBeKinematic = false, bool shouldSelfBeKinematic = false)
    {
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        if(rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.drag = 10.0f;
        rb.angularDrag = 10.0f;
        rb.mass = .1f;
        rb.isKinematic = shouldSelfBeKinematic;
        Segment prev = parent;
        Rigidbody rb2 = prev.gameObject.GetComponent<Rigidbody>();
        if(rb2 == null)
        {
            rb2 = prev.gameObject.AddComponent<Rigidbody>();
        }
        rb2.drag = 10f;
        rb2.angularDrag = 10f;
        rb2.mass = .1f;
        rb2.isKinematic = shouldParentBeKinematic;
        ConfigureJoint(this, parent, parent.transform.InverseTransformPoint(transform.TransformPoint(0,0,0)));
    }

    
    public float InterpolateOuterRadius(float outerRadiusMin, float outerRadiusMax, float y, float innerRadius)
    {
        float origY = y;
        y = (y+.5f)*3%1-.5f;
        float ans = 4 * (outerRadiusMin - outerRadiusMax) * y * y + outerRadiusMax;
        if(origY < -.3f)
        {
            float percent = (-.3f - origY) / .2f;
            return Mathf.Lerp(ans, innerRadius, percent);
        }else if(origY > .3f)
        {
            float percent = (.5f - origY) / .2f;
            return Mathf.Lerp(innerRadius, ans, percent);
        }else{
            return ans;
        }
    }

    public float InterpolateOuterRadius2(float percentage)
    {
        return .25f * Mathf.Sin(4*percentage * Mathf.PI * 2) + .75f + .1f*Mathf.Sin(20 * percentage * Mathf.PI * 2);
    }

    //parent anchor given in local coords
    GameObject ConfigureJoint(Segment child, Segment parent, Vector3 parentAnchor)
    {
        Debug.Log(parentAnchor);
        GameObject joint = new GameObject("Joint");
        joint.transform.up = (end - start).normalized;
        Rigidbody rigidbody = joint.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;
        joint.transform.parent = parent.transform;
        joint.transform.localPosition = parentAnchor;
        joint.transform.localRotation = Quaternion.Euler(0,0,0);
        joint.transform.parent = null;
        //we want to make parent -> joint a fixed joint
        FollowSegmentLocalPosition follow = joint.AddComponent<FollowSegmentLocalPosition>();
        follow.segment = parent;
        follow.localPosition = parentAnchor;
        //and joint -> child a rotator
        ConfigurableJoint rotator = joint.AddComponent<ConfigurableJoint>();
        Rigidbody childRb = child.gameObject.GetComponent<Rigidbody>();
        if(childRb == null)
            childRb = child.gameObject.AddComponent<Rigidbody>();
        rotator.connectedBody = childRb;
        rotator.autoConfigureConnectedAnchor = false;
        rotator.anchor = new Vector3(0, 0, 0);
        rotator.connectedAnchor = new Vector3(0,0,0);
        rotator.axis = new Vector3(0, 1, 0);
        rotator.secondaryAxis = new Vector3(1, 0, 0);
        rotator.xMotion = ConfigurableJointMotion.Locked;
        rotator.yMotion = ConfigurableJointMotion.Locked;
        rotator.zMotion = ConfigurableJointMotion.Locked;
        rotator.angularXMotion = ConfigurableJointMotion.Locked;
        rotator.angularYMotion = ConfigurableJointMotion.Limited;
        rotator.angularZMotion = ConfigurableJointMotion.Limited;

        // Set angular Y and Z limits
        SoftJointLimit jointLimit = new SoftJointLimit();
        jointLimit.limit = 60; // 5 degrees limit
        rotator.angularYLimit = jointLimit;
        rotator.angularZLimit = jointLimit;

        // Set rotation drive mode YZ
        JointDrive drive = new JointDrive();
        drive.positionSpring = .3f; // Small spring force
        //drive.positionDamper = 100; // Small damper
        drive.maximumForce = 100;
        rotator.rotationDriveMode = RotationDriveMode.Slerp;
        rotator.slerpDrive = drive;

        return joint;
    }

    /*ConfigurableJoint ConfigureJoint(Segment child, Segment parent)
    {
        // For all capsules except the bottom one
        GameObject obj = child.gameObject;
        GameObject connected = parent.gameObject;
        ConfigurableJoint joint = obj.AddComponent<ConfigurableJoint>();
        joint.connectedBody = connected.GetComponent<Rigidbody>();

        // Set primary and secondary axes
        joint.axis = new Vector3(0, 1, 0); // Primary axis, e.g., local X-axis
        joint.secondaryAxis = new Vector3(1, 0, 0); // Secondary axis, e.g., local Y-axis


        // Lock XYZ motion
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;

        // Lock angular X, but limit angular Y and Z
        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Limited;
        joint.angularZMotion = ConfigurableJointMotion.Limited;

        // Set angular Y and Z limits
        SoftJointLimit jointLimit = new SoftJointLimit();
        jointLimit.limit = 20; // 5 degrees limit
        joint.angularYLimit = jointLimit;
        joint.angularZLimit = jointLimit;

        // Set very high spring force for angular Y and Z limits using SoftJointLimitSpring
        SoftJointLimitSpring limitSpring = new SoftJointLimitSpring();
        limitSpring.spring = 10000; // Very high spring force
        limitSpring.damper = 1000; // High damper
        joint.angularYZLimitSpring = limitSpring;

        // Set rotation drive mode YZ
        JointDrive drive = new JointDrive();
        drive.positionSpring = 100; // Small spring force
        drive.positionDamper = 100; // Small damper
        drive.maximumForce = 100;
        joint.rotationDriveMode = RotationDriveMode.Slerp;

        joint.slerpDrive = drive;

        // Set Y and Z target rotation to all zeros
        joint.targetRotation = Quaternion.identity;

        // Set joint anchors
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = new Vector3(0, 0, 0);
        joint.connectedAnchor = new Vector3(0, (parent.end - parent.start).magnitude, 0);

        // Set projection mode to None for linear deviations and Limit to angular deviations
        joint.projectionMode = JointProjectionMode.PositionAndRotation;
        
        // Set the projection distance for X and Z axes to zero (no linear deviation allowed)
        joint.projectionAngle = 0f;
        
        // Set the projection angle limit to zero (no angular deviation allowed) for Y-axis (vertical)
        joint.projectionAngle = 0f;

        return joint;
    }*/

    //samples must be at least 2
    //radius and whatnot are given in absolutes
    public Mesh MakeMesh(float innerRadius, float outerRadius, int samples)
    {
        float height = (end - start).magnitude;
        Vector3[] vertices = new Vector3[6 * samples];
        float dy = 1f / (samples - 1);

        for(int s = 0;s<samples;s++)
        {
            bool localMaximum = false;
            float r = outerRadius * InterpolateOuterRadius(.9f, 1.1f, -.5f + dy * s, innerRadius);
            if(s != 0 && s != samples - 1)
            {
                float prevR = outerRadius * InterpolateOuterRadius(.9f, 1.1f, -.5f + dy * (s-1), innerRadius);
                float nextR = outerRadius * InterpolateOuterRadius(.9f, 1.1f, -.5f + dy * (s+1), innerRadius);
                if(prevR < r && r > nextR)
                {
                    localMaximum = true;
                }
            }
            List<Node2> ring = new List<Node2>();
            for(int i = 0;i<6;i++)
            {
                float sin = Mathf.Sin(Mathf.PI / 3 * i);
                float cos = Mathf.Cos(Mathf.PI / 3 * i);
                if((i & 1) == 0)
                {   
                    Vector3 vertex = new Vector3(r * cos, dy * s * height, r * sin);
                    if(localMaximum)
                    {
                        ring.Add(new Node2(){
                            location = transform.TransformPoint(vertex),
                            used = false,
                        });
                        GameObject node = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        node.name = i+"";
                        node.transform.parent = transform;
                        node.transform.localPosition = vertex;
                        node.transform.localScale = new Vector3(.01f,.01f, .01f);
                    }
                    vertices[s * 6 + i] = vertex;
                }else{
                    vertices[s * 6 + i] = new Vector3(innerRadius * cos, dy * s * height, innerRadius * sin);
                }
            }
            if(localMaximum)
                rings.Add(ring);
        }

        Mesh mesh = new Mesh();

        //lets start with 12 faces
        int[] triangles = new int[36 * (samples - 1)];
        for(int s = 0;s<samples-1;s++)
        {
            for(int i = 0;i<6;i++)
            {
                triangles[s * 36 + i * 6] = i + s * 6;
                triangles[s * 36 + i * 6+1] = (i+1) % 6 + 6 + s * 6;
                triangles[s * 36 + i * 6+2] = (i+1) % 6 + s * 6;
                triangles[s * 36 + i * 6+3] = i + s * 6;
                triangles[s * 36 + i * 6+4] = i + 6 + s * 6;
                triangles[s * 36 + i * 6+5] = (i+1) % 6 + 6 + s * 6;
            }
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
}
