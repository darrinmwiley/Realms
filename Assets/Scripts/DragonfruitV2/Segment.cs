using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Segment{
    public Spline spline;
    public float scale;
    public int numVertices;
    public Vertex parent;
    public Vertex root;
    public GameObject gameObject;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public Material mat;
    public Mesh mesh;
    //todo: add public Vector3 direction;

    public List<Vertex> vertices = new List<Vertex>();
    
    private float growth;

    public Segment(Vertex parent, Vector3 direction, Spline spline, int numVertices, Material mat){
        this.mat = mat;
        this.gameObject = new GameObject("Segment");
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = mat;
        this.parent = parent;
        this.numVertices = numVertices;
        this.spline = spline;
        //todo make this composite of direction and spline first point
        //or alternatively make it in the same place as parent and just rotating
        float distanceBetweenPoints = 1f / numVertices;
        ///root = new Vertex(parent, direction, 0, /*immovable = */ false, /*isFixed = */  true);
        ///root.gameObject.name = "Segment Root";
        gameObject.transform.parent = root.gameObject.transform;
        gameObject.transform.localPosition = Vector3.zero;
        gameObject.transform.localRotation = Quaternion.LookRotation(direction);
        /*root.SetGrowth(1);    
        vertices.Add(root);
        if(direction != Vector3.up)
            root.gameObject.GetComponent<ArticulationBody>().anchorRotation = /*Quaternion.Euler(90,0,0)  Quaternion.LookRotation(direction);
        else{
            root.gameObject.GetComponent<ArticulationBody>().parentAnchorRotation = Quaternion.identity;
            root.gameObject.GetComponent<ArticulationBody>().anchorRotation = Quaternion.identity;
        }
        Vector3 nextPointWorldSpace = root.gameObject.transform.TransformPoint(spline.Evaluate(distanceBetweenPoints));
        Vector3 dir = root.gameObject.transform.InverseTransformPoint(nextPointWorldSpace);
        Vertex next = new Vertex(root, dir, dir.magnitude, false, false);
        Debug.Log("dir: "+dir);
        next.gameObject.transform.rotation = root.gameObject.transform.rotation;
        vertices.Add(next);*/    
    }

    public void SetGrowth(float f)
    {
        int currentVertex = (int)(growth / (1f / numVertices));
        int currentAfterGrowth = (int)(f / (1f / numVertices));
        float distanceBetweenPoints = 1f / numVertices;
        if(currentAfterGrowth >= numVertices){
            UpdateMesh(MakeSplineForMesh(), growth);
            return;
        }
        if(currentAfterGrowth > currentVertex)
        {
            Vector3 nextPointWorldSpace = root.gameObject.transform.TransformPoint(spline.Evaluate((currentAfterGrowth + 1) * distanceBetweenPoints));
            Vector3 currentPointWorldSpace = root.gameObject.transform.TransformPoint(spline.Evaluate((currentAfterGrowth) * distanceBetweenPoints));
            Vector3 delta = spline.Evaluate((currentAfterGrowth + 1) * distanceBetweenPoints) - spline.Evaluate(currentAfterGrowth * distanceBetweenPoints);

            //make a very small sphere at nextPointWorldSpace, no collider. GameObject primitive style
            Vector3 direction = vertices[vertices.Count - 1].gameObject.transform.InverseTransformPoint(nextPointWorldSpace);
            
            Vertex added = new Vertex(vertices[vertices.Count - 1], delta, delta.magnitude, false, false);
            vertices[vertices.Count - 1].SetGrowth(1);
            vertices.Add(added);
        }
        float nextVertexGrowthPercentage = (f - (distanceBetweenPoints * currentAfterGrowth)) / distanceBetweenPoints;
        vertices[vertices.Count - 1].SetGrowth(nextVertexGrowthPercentage);
        growth = f;
        UpdateMesh(MakeSplineForMesh(), growth);
        for(int i = 0;i<vertices.Count;i++){
            Vertex vertex = vertices[i];
            float time = i / (vertices.Count - 1);
            vertex.gameObject.GetComponent<SphereCollider>().radius = GetWidth(time);
        }
    }

    public float GetGrowth()
    {
        return growth;
    }

    //TODO: fix
    public float GetWidth(float time)
    {
        return .25f;
    }

    public float InterpolateOuterRadius(float outerRadiusMin, float outerRadiusMax, float y, float innerRadius, int numSegments)
    {
        if(y < -.3f)
        {
            float percent = (-.3f - y) / .2f;
            return Mathf.Lerp(outerRadiusMin, innerRadius, percent);
        }else if(y > .4f)
        {
            float percent = (.5f - y) / .1f;
            return Mathf.Lerp(innerRadius, outerRadiusMin, percent);
        }
        y = ((y - -.3f) / .7f)-.5f;
        y = (y+.5f)*numSegments%1-.5f;
        float ans = 4 * (outerRadiusMin - outerRadiusMax) * y * y + outerRadiusMax;
        return ans;
    }

    public Spline MakeSplineForMesh()
    {
        List<Vector3> controlPoints = new List<Vector3>();

        // Collect the local positions of all vertices
        foreach (Vertex vertex in vertices)
        {
            controlPoints.Add(root.gameObject.transform.InverseTransformPoint(vertex.gameObject.transform.position));
        }

        // Create a new CatmullRomSpline from the control points
        return new CatmullRomSpline(controlPoints, 1);
    }

    public Segment AddChild(float percent, Vector3 direction, Spline childSpline)
    {
        // Step 1: Get the Vertex closest to the provided percentage
        int closestVertexIndex = Mathf.RoundToInt(percent * (vertices.Count - 1));
        Vertex closestVertex = vertices[closestVertexIndex];

        Segment newChildSegment = new Segment(closestVertex, direction, childSpline, numVertices, mat);
        return newChildSegment;
    }

    //samples must be at least 2
    //radius and whatnot are given in absolutes
    public void UpdateMesh(Spline spline, float time)
    {
        float innerRadius = .1f;
        float outerRadius = .3f;
        int samples = 100;
        AnimationCurve thicknessGrowthCurve = new AnimationCurve();
        thicknessGrowthCurve.AddKey(0,0);
        thicknessGrowthCurve.AddKey(1,1);
        int numSegments = 5;

        Vector3[] vertices = new Vector3[6 * samples];
        float dy = 1f / (samples - 1);

        for (int s = 0; s < samples; s++)
        {
            bool localMinimum = false;
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

                if ((i & 1) == 0)
                {
                    Vector3 baseOffset = new Vector3(taper * r * cos, 0, taper * r * sin);
                    Vector3 offset = rotation * baseOffset;
                    Vector3 vertex = currentPos + offset;
                    vertices[s * 6 + i] = vertex;
                }
                else
                {
                    Vector3 offset = rotation * new Vector3(taper * innerRadius * cos, 0, taper * innerRadius * sin);
                    Vector3 vertex = currentPos + offset;
                    vertices[s * 6 + i] = vertex;
                }
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

        if(mesh == null)
        {
            mesh = new Mesh();
            meshFilter.mesh = mesh;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}