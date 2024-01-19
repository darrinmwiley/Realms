using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragonfruitMesh : MonoBehaviour
{

    public Material mat;

    public float innerRadiusStart = .3f;
    public float outerRadiusStart = 1f;
    public float bulgeFactor = 1.1f;
    public float shrinkFactor = .9f;
    public float heightStart = 1;
    public int maxIterations = 6;


    public void Update()
    {
        foreach(Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        float innerRadius = innerRadiusStart;
        float outerRadius = outerRadiusStart;
        float height = heightStart;
        float y = 0;
        int it = 0;
        while(outerRadius >= innerRadius)
        {
            it++;
            GameObject go = MakeDragonfruitLinkMeshV3(innerRadius, outerRadius, outerRadius*bulgeFactor, outerRadius*shrinkFactor, height, 5);
            go.transform.parent = transform;
            go.GetComponent<MeshRenderer>().material = mat;
            outerRadius *= shrinkFactor;
            go.transform.position = new Vector3(0,y,0);
            y += height;
            height *= shrinkFactor;
            if(it > maxIterations)
                break;
        }
    }

    //samples must be at least 2
    public GameObject MakeDragonfruitLinkMeshV3(float innerRadius, float outerRadiusStart, float outerRadiusBulge, float outerRadiusEnd, float height, int samples)
    {
        Vector3[] vertices = new Vector3[6 * samples];
        float dy = height / (samples - 1);
        for(int s = 0;s<samples;s++)
        {
            for(int i = 0;i<6;i++)
            {
                float sin = Mathf.Sin(Mathf.PI / 3 * i);
                float cos = Mathf.Cos(Mathf.PI / 3 * i);
                if((i & 1) == 0)
                {
                    float r = interpolateRadius(outerRadiusStart, outerRadiusBulge, outerRadiusEnd, height, dy * s);
                    vertices[s * 6 + i] = new Vector3(r * cos, dy * s, r * sin);
                }else{
                    vertices[s * 6 + i] = new Vector3(innerRadius * cos, dy * s, innerRadius * sin);
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

        GameObject go = new GameObject("dragonfruit", typeof(MeshFilter), typeof(MeshRenderer));
        go.GetComponent<MeshFilter>().mesh = mesh;
        return go;
    }

    //samples must be at least 2
    public void MakeDragonfruitLinkMeshV2(float innerRadius, float outerRadiusMin, float outerRadiusMax, float height, int samples)
    {
        Vector3[] vertices = new Vector3[6 * samples];
        float dy = height / (samples - 1);
        for(int s = 0;s<samples;s++)
        {
            for(int i = 0;i<6;i++)
            {
                float sin = Mathf.Sin(Mathf.PI / 3 * i);
                float cos = Mathf.Cos(Mathf.PI / 3 * i);
                if((i & 1) == 0)
                {
                    float r = interpolateRadius(outerRadiusMin, outerRadiusMax, height, dy * s);
                    vertices[s * 6 + i] = new Vector3(r * cos, dy * s, r * sin);
                }else{
                    vertices[s * 6 + i] = new Vector3(innerRadius * cos, dy * s, innerRadius * sin);
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

        GameObject go = new GameObject("dragonfruit", typeof(MeshFilter), typeof(MeshRenderer));
        go.GetComponent<MeshFilter>().mesh = mesh;
    }

    public float interpolateRadius(float outerRadiusMin, float outerRadiusMax, float height, float y)
    {
        y-=height / 2;
        return 4 * (outerRadiusMin - outerRadiusMax) / (height * height) * y * y + outerRadiusMax;
    }

    public float interpolateRadius(float outerRadiusStart, float outerRadiusBulge, float outerRadiusEnd, float height, float y)
    {
        y-=height / 2;
        if(y > 0)
        {
            return 4 * (outerRadiusEnd - outerRadiusBulge) / (height * height) * y * y + outerRadiusBulge;
        }else{
            return 4 * (outerRadiusStart - outerRadiusBulge) / (height * height) * y * y + outerRadiusBulge;
        }
    }

    public void MakeDragonfruitLinkMesh(float innerRadius, float outerRadius, float height)
    {
        Vector3[] vertices = new Vector3[12];
        for(int i = 0;i<6;i++)
        {
            float sin = Mathf.Sin(Mathf.PI / 3 * i);
            float cos = Mathf.Cos(Mathf.PI / 3 * i);
            if((i & 1) == 0)
            {
                vertices[i] = new Vector3(outerRadius * cos, 0, outerRadius * sin);
                vertices[i + 6] = new Vector3(outerRadius * cos, height, outerRadius * sin);
            }else{
                vertices[i] = new Vector3(innerRadius * cos, 0, innerRadius * sin);
                vertices[i + 6] = new Vector3(innerRadius * cos, height, innerRadius * sin);
            }
        }
        Mesh mesh = new Mesh();

        //lets start with 12 faces
        int[] triangles = new int[36];
        for(int i = 0;i<6;i++)
        {
            triangles[i * 6] = i;
            triangles[i * 6+1] = (i+1) % 6 + 6;
            triangles[i * 6+2] = (i+1) % 6;
            triangles[i * 6+3] = i;
            triangles[i * 6+4] = i + 6;
            triangles[i * 6+5] = (i+1) % 6 + 6;
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GameObject go = new GameObject("dragonfruit", typeof(MeshFilter), typeof(MeshRenderer));
        go.GetComponent<MeshFilter>().mesh = mesh;
    }
}
