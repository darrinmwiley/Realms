using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SegmentControlPoints
{
    public List<Vector3> controlPoints; // List of control points in world space
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
    private float flexibility = 0;

    // Constructor that takes a list of control points in world space
    public SegmentControlPoints(GameObject parent, List<Vector3> controlPoints, float flexibility, Material mat)
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

        // Ensure the parent has an ArticulationBody
        ArticulationBody parentArticulationBody = parent.GetComponent<ArticulationBody>();
        if (parentArticulationBody == null)
        {
            parentArticulationBody = parent.AddComponent<ArticulationBody>();
            parentArticulationBody.jointType = ArticulationJointType.FixedJoint;
        }

        // Create the root growth attached to the parent
        Vector3 direction = (controlPoints[0] - parent.transform.position);
        rootGrowth = new Growth(parent, direction.normalized, true, flexibility, 100f, direction.magnitude);
        gameObject.transform.parent = rootGrowth.bendJoint.transform;
        gameObject.transform.localPosition = Vector3.zero;
        RotateGameObjectTowards(gameObject, Vector3.up, true);
        growths.Add(rootGrowth);
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
            //UpdateMesh(MakeSplineForMesh(), growthPercentage);
            return;
        }

        if (currentAfterGrowth > currentVertex)
        {
            Vector3 nextPointWorldSpace = controlPoints[currentAfterGrowth]; 
            Vector3 currentPointWorldSpace = controlPoints[currentAfterGrowth - 1];
            Vector3 delta = nextPointWorldSpace - currentPointWorldSpace;
            Debug.Log(delta);
            Growth newGrowth = new Growth(growths[growths.Count - 1].growJoint, delta.normalized, false, flexibility, 50f, delta.magnitude);
            growths[growths.Count - 1].SetGrowth(1);
            growths.Add(newGrowth);
        }

        float nextGrowthPercentage = (growthPercentage - (distanceBetweenPoints * currentAfterGrowth)) / distanceBetweenPoints;
        growths[growths.Count - 1].SetGrowth(nextGrowthPercentage);

        growth = growthPercentage;
        //UpdateMesh(MakeSplineForMesh(), growthPercentage);

        for (int i = 0; i < growths.Count; i++)
        {
            Growth growthSegment = growths[i];
            float time = i / (growths.Count - 1f);
            // Assuming a way to adjust the collider size, e.g., with a custom sphere or capsule collider on the growJoint
            growthSegment.growJoint.GetComponent<SphereCollider>().radius = .25f;
        }
    }

}