using System.Collections.Generic;
using UnityEngine;

public class PlantSkeleton : MonoBehaviour
{
    GameObject baseObj;
    List<Segment> segments = new List<Segment>();
    public Material green;

    public int numSegments = 2;

    public class Segment{
        public GameObject body;
        public ConfigurableJoint joint;
        public Segment previous;
        public Vector3 initialScale;
        public Vector3 finalScale;
        public float growTime;
        public float creationTime;

        public GameObject rendering;

        public void UpdateMesh(Material green){
            if(rendering == null)
            {
                rendering = new GameObject("rendering");
                rendering.AddComponent<MeshFilter>();
                rendering.AddComponent<MeshRenderer>().material = green;
                rendering.transform.parent = body.transform;
                rendering.transform.localPosition = new Vector3(0,0,0);
                rendering.transform.localScale = new Vector3(1,2,1);
                rendering.transform.localRotation = Quaternion.Euler(0,0,0);
            }
            Mesh mesh = MakeMeshForSegment(.3f, 1, 50, this);
            rendering.GetComponent<MeshFilter>().mesh = mesh;
        }

        public float InterpolateOuterRadius2(float percentage)
        {
            return .25f * Mathf.Sin(4*percentage * Mathf.PI * 2) + .75f + .1f*Mathf.Sin(20 * percentage * Mathf.PI * 2);
        }

        public float InterpolateOuterRadius(float outerRadiusMin, float outerRadiusMax, float y)
        {

            y = (y+.5f)*3%1-.5f;
            return 4 * (outerRadiusMin - outerRadiusMax) * y * y + outerRadiusMax;
        }

        //samples must be at least 2
        public Mesh MakeMeshForSegment(float innerRadius, float outerRadius, int samples, Segment segment)
        {
            Vector3[] vertices = new Vector3[6 * samples];
            float height = 1;
            float dy = height / (samples - 1);
            for(int s = 0;s<samples;s++)
            {
                for(int i = 0;i<6;i++)
                {
                    float sin = Mathf.Sin(Mathf.PI / 3 * i);
                    float cos = Mathf.Cos(Mathf.PI / 3 * i);
                    if((i & 1) == 0)
                    {
                        float r = outerRadius * InterpolateOuterRadius(.9f, 1.1f, -.5f + dy * s);
                        vertices[s * 6 + i] = new Vector3(r * cos, -.5f + dy * s, r * sin);
                    }else{
                        vertices[s * 6 + i] = new Vector3(innerRadius * cos, -.5f + dy * s, innerRadius * sin);
                    }
                }
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

        public Vector3 GetTip(){
            return body.transform.position + body.transform.rotation * Vector3.up * (body.transform.localScale.y);
        }
        public Vector3 GetBase(){
            return body.transform.position - body.transform.rotation * Vector3.up * (body.transform.localScale.y);
        }
        public Vector3 GetSize(){
            float elapsed = Time.time - creationTime;
            float percentage = Mathf.Min(1, elapsed / growTime);
            return Vector3.Lerp(initialScale, finalScale, percentage);
        }
        public bool IsGrown(){
            return creationTime + growTime < Time.time;
        }
    }

    void Start()
    {
        baseObj = new GameObject("base");
        baseObj.AddComponent<Rigidbody>().isKinematic = true;
        AddSegment(new Vector3(1,2,1), new Vector3(1,2,1), -1);
        AddSegment(new Vector3(.2f,.1f,.2f), new Vector3(1,2,1), 10);
    }

    public void ResizeSegment(Segment segment, Vector3 scale)
    {
        Segment previousSegment = segment.previous;
        Rigidbody rb = segment.body.GetComponent<Rigidbody>();
        GameObject body = segment.body;
        Vector3 position = body.transform.position;
        Quaternion rotation = body.transform.rotation;

        float newHeight = scale.y * 2;

        Vector3 newPosition = previousSegment.GetTip() + rotation * Vector3.up * (newHeight / 2f);
        Quaternion saveRotation = segment.body.transform.rotation;
        
        segment.body.transform.localScale = scale;
        
        //we need to reconfigure the joint, so temporarily align this segment with the previous one tip to tip
        segment.body.transform.rotation = previousSegment.body.transform.rotation;
        segment.body.transform.position = previousSegment.GetTip() + previousSegment.body.transform.rotation * Vector3.up * (newHeight / 2f);
        rb.centerOfMass = body.transform.InverseTransformPoint(body.transform.position);
        Destroy(body.GetComponent<ConfigurableJoint>());
        segment.joint = ConfigureJoint(body, previousSegment.body);
        
        segment.body.transform.rotation = saveRotation;
        segment.body.transform.position = newPosition;
    }

    void AddSegment(Vector3 initialScale, Vector3 finalScale, float growTime)
    {
        GameObject previousSegmentObj = baseObj;
        Vector3 tip = baseObj.transform.position;
        Quaternion rotation = Quaternion.Euler(0,0,0);
        if (segments.Count != 0)
        {
            tip = segments[segments.Count - 1].GetTip();
            previousSegmentObj = segments[segments.Count - 1].body;
            rotation = previousSegmentObj.transform.rotation;
        }

        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.GetComponent<Renderer>().enabled = false;

        Vector3 newPosition = tip + rotation * Vector3.up * (initialScale.y / 2f);
        Debug.Log(newPosition);
        capsule.transform.localScale = initialScale;
        capsule.transform.position = newPosition;
        capsule.transform.rotation = rotation;     

        Rigidbody rb = capsule.AddComponent<Rigidbody>();
        rb.drag = 3.0f;
        rb.angularDrag = 3.0f;
        rb.mass = 0.01f;
        ConfigurableJoint joint = ConfigureJoint(capsule, previousSegmentObj);
        if(segments.Count == 0)
        {
            joint.anchor = new Vector3(0, 0, 0);
        }

        Segment prev = null;
        if(segments.Count != 0)
            prev = segments[segments.Count - 1];

        segments.Add(new Segment(){
            body = capsule,
            joint = joint,
            initialScale = initialScale,
            finalScale = finalScale,
            creationTime = Time.time,
            growTime = growTime,
            previous = prev,
        });
    }

    ConfigurableJoint ConfigureJoint(GameObject obj, GameObject connected)
    {
        // For all capsules except the bottom one
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
        jointLimit.limit = 5; // 5 degrees limit
        joint.angularYLimit = jointLimit;
        joint.angularZLimit = jointLimit;

        // Set very high spring force for angular Y and Z limits using SoftJointLimitSpring
        SoftJointLimitSpring limitSpring = new SoftJointLimitSpring();
        limitSpring.spring = 10000; // Very high spring force
        limitSpring.damper = 1000; // High damper
        joint.angularYZLimitSpring = limitSpring;

        // Set rotation drive mode YZ
        JointDrive drive = new JointDrive();
        drive.positionSpring = 5; // Small spring force
        drive.positionDamper = 10; // Small damper
        joint.rotationDriveMode = RotationDriveMode.Slerp;

        joint.slerpDrive = drive;

        // Set Y and Z target rotation to all zeros
        joint.targetRotation = Quaternion.identity;

        // Enable collision
        joint.enableCollision = true;

        // Set joint anchors
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = new Vector3(0, -.5f, 0);
        joint.connectedAnchor = new Vector3(0, 1, 0);

        // Set projection mode to None for linear deviations and Limit to angular deviations
        joint.projectionMode = JointProjectionMode.PositionAndRotation;
        
        // Set the projection distance for X and Z axes to zero (no linear deviation allowed)
        joint.projectionAngle = 0f;
        
        // Set the projection angle limit to zero (no angular deviation allowed) for Y-axis (vertical)
        joint.projectionAngle = 0f;

        joint.configuredInWorldSpace = true;

        return joint;
    }

    bool flag;
    public bool shouldGrow;

    void Update()
    {
        Segment seg = segments[segments.Count - 1];
        if(!seg.IsGrown())
        {
            ResizeSegment(seg, seg.GetSize());
        }else{
            AddSegment(new Vector3(.2f,.1f,.2f), new Vector3(2,5,2), 4);
        }
        foreach(Segment segm in segments)
        {
            segm.UpdateMesh(green);
        }
    }

}