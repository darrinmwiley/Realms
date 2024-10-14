using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SegmentControlPoints
{
    public List<Vector3> controlPoints; // List of control points in world space
    public int numVertices;
    public Growth rootGrowth;
    public GameObject parent;
    public GameObject gameObject;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public Material mat;
    public Mesh mesh;

    public List<Growth> growths = new List<Growth>();

    private float growth;
    private float flexibility = 0;

    private bool nodesAdded;

    private bool parentSpace;

    private bool showMesh = false;

    // Constructor that takes a list of control points in world space
    public SegmentControlPoints(GameObject parent, List<Vector3> controlPoints, float flexibility, bool parentSpace, Material mat)
    {
        Debug.Log(parent.transform.position+" "+string.Join(",", controlPoints));
        this.mat = mat;
        this.gameObject = new GameObject("SegmentControlPoints");
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = mat;
        this.parent = parent;
        this.numVertices = controlPoints.Count;
        this.controlPoints = controlPoints;
        this.flexibility = flexibility;
        this.parentSpace = parentSpace;

        // Ensure the parent has an ArticulationBody
        ArticulationBody parentArticulationBody = parent.GetComponent<ArticulationBody>();
        if (parentArticulationBody == null)
        {
            parentArticulationBody = parent.AddComponent<ArticulationBody>();
            parentArticulationBody.jointType = ArticulationJointType.FixedJoint;
        }

        // Create the root growth attached to the parent
        Vector3 direction = (controlPoints[0] - parent.transform.position);
        Debug.Log("direction: "+direction+" "+direction.magnitude);
        rootGrowth = new Growth(parent, direction.normalized, parentSpace, flexibility, 100f, direction.magnitude);
        gameObject.transform.parent = rootGrowth.bendJoint.transform;
        gameObject.transform.localPosition = Vector3.zero;
        RotateGameObjectTowards(gameObject, Vector3.up, true);
        growths.Add(rootGrowth);
        gameObject.GetComponent<MeshRenderer>().enabled = showMesh;
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
        int currentVertex = (int)(growth / (1f / numVertices));
        int currentAfterGrowth = (int)(growthPercentage / (1f / numVertices));
        float distanceBetweenPoints = 1f / numVertices;

        if (currentAfterGrowth >= numVertices)
        {
            UpdateMesh(MakeSplineForMesh(), growthPercentage);
            return;
        }

        if (currentAfterGrowth > currentVertex)
        {
            Vector3 nextPointWorldSpace = controlPoints[currentAfterGrowth]; 
            Vector3 currentPointWorldSpace = controlPoints[currentAfterGrowth - 1];
            Vector3 delta = nextPointWorldSpace - currentPointWorldSpace;
            Growth newGrowth = new Growth(growths[growths.Count - 1].growJoint, delta.normalized, parentSpace, flexibility, 50f, delta.magnitude);
            growths[growths.Count - 1].SetGrowth(1);
            growths.Add(newGrowth);
        }

        float nextGrowthPercentage = (growthPercentage - (distanceBetweenPoints * currentAfterGrowth)) / distanceBetweenPoints;
        growths[growths.Count - 1].SetGrowth(nextGrowthPercentage);

        growth = growthPercentage;
        UpdateMesh(MakeSplineForMesh(), growthPercentage);

        for (int i = 0; i < growths.Count; i++)
        {
            Growth growthSegment = growths[i];
            float time = i / (growths.Count - 1f);
            // Assuming a way to adjust the collider size, e.g., with a custom sphere or capsule collider on the growJoint
            growthSegment.growJoint.GetComponent<SphereCollider>().radius = .25f;
        }
    }
    
    public float InterpolateOuterRadius(float outerRadiusMin, float outerRadiusMax, float y, float innerRadius, int numSegments)
    {
        if (y < -.3f)
        {
            float percent = (-.3f - y) / .2f;
            return Mathf.Lerp(outerRadiusMin, innerRadius, percent);
        }
        else if (y > .4f)
        {
            float percent = (.5f - y) / .1f;
            return Mathf.Lerp(innerRadius, outerRadiusMin, percent);
        }
        y = ((y - -.3f) / .7f) - .5f;
        y = (y + .5f) * numSegments % 1 - .5f;
        float result = 4 * (outerRadiusMin - outerRadiusMax) * y * y + outerRadiusMax;
        return result;
    }

    public Spline MakeSplineForMesh()
    {
        List<Vector3> controlPoints = new List<Vector3>();
        controlPoints.Add(Vector3.zero);

        // Collect the local positions of all growths
        foreach (Growth growth in growths)
        {
            controlPoints.Add(rootGrowth.bendJoint.transform.InverseTransformPoint(growth.growJoint.transform.position));
        }

        // Create a new spline from the control points
        return new CatmullRomSpline(controlPoints, 1);
    }

    /*public SegmentControlPoints AddChildEnd(){
        growth lastGrowth = growths[growths.Count - 1];

        //TODO: add a set of control points going generally upwards with some level of X Z variance as well
        List<Vector3> childControlPoints = new List<Vector3>();
    }

    public SegmentControlPoints AddChildSide(){

    }*/

    //adds a child to end in current state
    public SegmentControlPoints AddChild(float percent, Vector3 direction, int numVertices, float flexibility, bool parentSpace)
    {
        // Step 1: Get the Growth closest to the provided percentage
        int closestGrowthIndex = Mathf.RoundToInt(percent * (growths.Count - 1));
        closestGrowthIndex = growths.Count - 1;
        Growth closestGrowth = growths[closestGrowthIndex];

        // Step 2: Calculate control points for the new child segment
        List<Vector3> childControlPoints = GenerateControlPoints(closestGrowth.growJoint.transform.position, direction, numVertices);

        // Step 3: Create a new SegmentControlPoints attached to the growJoint of the closest growth
        SegmentControlPoints newChildSegment = new SegmentControlPoints(
            closestGrowth.growJoint,   // Parent object becomes the growJoint of the closest growth
            childControlPoints,        // The generated control points
            flexibility,               // Flexibility inherited from parent segment
            parentSpace,
            mat                        // Material inherited from parent segment
        );

        return newChildSegment;
    }

    private List<Vector3> GenerateControlPoints(Vector3 startPosition, Vector3 direction, int numVertices)
    {
        List<Vector3> controlPoints = new List<Vector3>();

        // Get the magnitude of the direction vector, which will be the total length of the segment
        float totalLength = direction.magnitude;

        // Normalize the direction to get a unit vector
        Vector3 normalizedDirection = direction.normalized;

        // Calculate the distance between control points, evenly distributing along the total length
        float distanceBetweenPoints = totalLength / (numVertices - 1);  // We subtract 1 because there are n-1 spaces between n points

        // Generate control points along the direction
        for (int i = 1; i < numVertices; i++)
        {
            Vector3 point = startPosition + normalizedDirection * distanceBetweenPoints * i;
            controlPoints.Add(point);
        }

        return controlPoints;
    }

    // Mesh update logic based on spline
    public void UpdateMesh(Spline spline, float time)
    {
        float innerRadius = .1f;
        float outerRadius = .35f;
        int samples = 100;
        AnimationCurve thicknessGrowthCurve = new AnimationCurve();
        thicknessGrowthCurve.AddKey(0, 0);
        thicknessGrowthCurve.AddKey(1, 1);
        int numSegments = 5;

        Vector3[] vertices = new Vector3[6 * samples];
        float dy = 1f / (samples - 1);

        for (int s = 0; s < samples; s++)
        {
            float r = outerRadius * InterpolateOuterRadius(.9f, 1.1f, -.5f + dy * s, innerRadius, numSegments);
            Vector3 currentPos = spline.Evaluate((float)s / (samples - 1));
            Vector3 prevPos = s > 0 ? spline.Evaluate((float)(s - 1) / (samples - 1)) : currentPos - Vector3.up;
            Vector3 direction = (currentPos - prevPos).normalized;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction).normalized;

            for (int i = 0; i < 6; i++)
            {
                float verticalTime = s / (samples - 1f);
                float sin = Mathf.Sin(Mathf.PI / 3 * i);
                float cos = Mathf.Cos(Mathf.PI / 3 * i);
                float taperTime = .3f;
                float taper;

                if (verticalTime == 1)
                {
                    taper = 0;
                }
                else
                {
                    float taperEndTime = Mathf.Min(1, verticalTime + taperTime);
                    taper = thicknessGrowthCurve.Evaluate(Mathf.Min(1, Mathf.Max(0, (time - verticalTime) / (taperEndTime - verticalTime))));
                }
                Vector3 baseOffset = new Vector3(taper * r * cos, 0, taper * r * sin);
                if(i % 2 == 1){
                    baseOffset = new Vector3(taper * innerRadius * cos, 0, taper * innerRadius * sin);
                }
                Vector3 offset = rotation * baseOffset;
                Vector3 vertex = currentPos + offset;
                vertices[s * 6 + i] = vertex;
            }
        }

        int[] triangles = new int[36 * (samples - 1)];

        for (int s = 0; s < samples - 1; s++)
        {
            for (int i = 0; i < 6; i++)
            {
                triangles[s * 36 + i * 6] = i + s * 6;
                triangles[s * 36 + i * 6 + 1] = (i + 1) % 6 + 6 + s * 6;
                triangles[s * 36 + i * 6 + 2] = (i + 1) % 6 + s * 6;
                triangles[s * 36 + i * 6 + 3] = i + s * 6;
                triangles[s * 36 + i * 6 + 4] = i + 6 + s * 6;
                triangles[s * 36 + i * 6 + 5] = (i + 1) % 6 + 6 + s * 6;
            }
        }

        if (mesh == null)
        {
            mesh = new Mesh();
            meshFilter.mesh = mesh;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

}