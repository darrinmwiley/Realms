using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO: separate the idea of "the time this sub-growth starts" and "where this subgrowth starts on the plant"
public class SubLsystem{
    //relative to parent
    public float startTime;
    //relative to parent
    public float growTime;
    public float offsetX;
    public float offsetZ;
    public float relativeScale;
    public LSystem lSystem;
}

public class LSystem
{
    /*ideas: 
    add (pseudorandom?) child L systems at some x,y,z,angle offset
    add randomness for direction of stem - stem is more of a spline than a line
    sub-productions will be a combination of {time, angleOffset, }
    */

    public float height;
    public float baseRadius;
    public float tipRadius;
    public int verticalSamples;
    public int horizontalSamples;
    public AnimationCurve thicknessGrowthCurve;
    public List<SubLsystem> subsystems = new List<SubLsystem>();

    //this * the growth time specified for the root L System is the actual total grow time
    public float getTotalTime()
    {
        if(subsystems.Count == 0)
        {
            return 1;
        }
        float max = 0;
        foreach(SubLsystem sub in subsystems)
        {
            max = Mathf.Max(max, 1 + sub.lSystem.getTotalTime() - sub.startTime);
        }
        return max;
    }

    public void Adjust(Mesh mesh, Vector3 newOrigin, Quaternion newRotation, float scale)
    {
        Vector3[] vertices = mesh.vertices;
        Matrix4x4 matrix = Matrix4x4.TRS(newOrigin, newRotation, Vector3.one * scale);

        for (int i = 0; i < vertices.Length; i++)
        {
            // Apply the matrix transformation to each vertex
            vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
    }

    //MakeMesh(x, y, z, thetax, thetay(0), thetaz, scale -> make the mesh)
    //combine all at end
    //1 corresponds to "this LSystem is done growing", but not necessarily child systems.
    //can dfs to find total grow time
    //grow time of child is grow time of parent - child.time
    public Mesh MakeMesh(float time)
    {
        List<Mesh> meshes = new List<Mesh>();
        //todo: make this a position instead of a height since we may have a non-straight spline
        foreach(SubLsystem sub in subsystems)
        {
            if(time >= sub.startTime)
            {
                float subTime = time - sub.startTime;
                Mesh subMesh = sub.lSystem.MakeMesh(subTime);
                Vector3 origin = new Vector3(0,Mathf.Min(1, time) * height * sub.startTime,0);
                Quaternion rotation = Quaternion.Euler(sub.offsetX, 0, sub.offsetZ);
                Adjust(subMesh, origin, rotation, sub.relativeScale);
                meshes.Add(subMesh);
            }
        }
        Debug.Log(time);
        time = Mathf.Min(time, 1);
        List<Line> lines = new List<Line>();
        for(int i = 0;i<verticalSamples;i++)
        {
            float verticalTime = i / (verticalSamples - 1f);
            float y = verticalTime * time * height;
            float radius = Mathf.Lerp(baseRadius, tipRadius, verticalTime) * thicknessGrowthCurve.Evaluate(time);
            List<Vector3> points = new List<Vector3>();
            for(int j = 0;j<horizontalSamples;j++)
            {
                float horizontalTime = j / (horizontalSamples - 1f);
                float theta = Mathf.PI * 2 * horizontalTime;
                float x = Mathf.Sin(theta) * radius;
                float z = Mathf.Cos(theta) * radius;
                points.Add(new Vector3(x,y,z));
            }
            lines.Add(new Line(){points = points});
        }
        Mesh mesh = new Face(){lines = lines, pointsPerLine = horizontalSamples}.MakeMesh();
        MeshUtils.Flip(mesh);
        meshes.Add(mesh);
        return MeshUtils.Combine(meshes.ToArray());
    }
}