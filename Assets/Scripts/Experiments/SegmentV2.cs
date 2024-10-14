using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SegmentV2
{
    public Spline spline;
    public float scale;
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

    private float flexibility;

    //todo: make magnitude calculations based on the spline instead of hardcoding 2
    //todo: make stiffness / flexibility configurable at the segment level
        // may be useful to ditinguish between stiffness / flexibility in the segment, and at the connection
        // configure mass to prevent jarring movements when a new growth is added. Have mass scale with growth
    //next big phase: bring back in the trellis and have the DF grow up it and make umbrella canopy
        // subtask: dragonfruit grow up surface
    //todo factor rotateGameObjectTowards helper to some global location

    //direction: which way to face the root offset in relation to parent
    //spline: should be relative to bend's space
    public SegmentV2(GameObject parent, Vector3 direction, Spline spline, int numVertices, float flexibility, Material mat)
    {
        this.mat = mat;
        this.gameObject = new GameObject("SegmentV2");
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = mat;
        this.parent = parent;
        this.numVertices = numVertices;
        this.spline = spline;
        this.flexibility = flexibility;

        // Ensure the parent has an ArticulationBody
        ArticulationBody parentArticulationBody = parent.GetComponent<ArticulationBody>();
        if (parentArticulationBody == null)
        {
            parentArticulationBody = parent.AddComponent<ArticulationBody>();
            parentArticulationBody.jointType = ArticulationJointType.FixedJoint; // Ensure it's a fixed joint or configure as needed
        }

        // Create the root growth attached to the parent
        rootGrowth = new Growth(parent, direction, true, flexibility, 100f, 2f);
        gameObject.transform.parent = rootGrowth.bendJoint.transform;
        gameObject.transform.localPosition = Vector3.zero;
        RotateGameObjectTowards(gameObject, Vector3.up, true);
        growths.Add(rootGrowth);
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
            Vector3 nextPointWorldSpace = rootGrowth.growJoint.transform.TransformPoint(spline.Evaluate((currentAfterGrowth + 1) * distanceBetweenPoints));
            Vector3 currentPointWorldSpace = rootGrowth.growJoint.transform.TransformPoint(spline.Evaluate((currentAfterGrowth) * distanceBetweenPoints));
            Vector3 delta = spline.Evaluate((currentAfterGrowth + 1) * distanceBetweenPoints) - spline.Evaluate(currentAfterGrowth * distanceBetweenPoints);

            // Create new growth segment
            Vector3 direction = growths[growths.Count - 1].growJoint.transform.InverseTransformPoint(nextPointWorldSpace);
            float magnitude = Vector3.Distance(growths[growths.Count - 1].growJoint.transform.position, nextPointWorldSpace);
            Growth newGrowth = new Growth(growths[growths.Count - 1].growJoint, delta, true, flexibility, 50f, magnitude);
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
            growthSegment.growJoint.GetComponent<SphereCollider>().radius = GetWidth(time);
        }
    }

    public float GetGrowth()
    {
        return growth;
    }

    public float GetWidth(float time)
    {
        return .25f;
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

    public SegmentV2 AddChild(float percent, Vector3 direction, Spline childSpline)
    {
        // Step 1: Get the Growth closest to the provided percentage
        int closestGrowthIndex = Mathf.RoundToInt(percent * (growths.Count - 1));
        Growth closestGrowth = growths[closestGrowthIndex];

        SegmentV2 newChildSegment = new SegmentV2(closestGrowth.growJoint, direction, childSpline, numVertices, flexibility, mat);
        return newChildSegment;
    }

    // Mesh update logic based on spline
    public void UpdateMesh(Spline spline, float time)
    {
        float innerRadius = .1f;
        float outerRadius = .3f;
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
}
