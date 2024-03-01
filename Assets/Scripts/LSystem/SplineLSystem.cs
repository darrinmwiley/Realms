using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class SplineLSystem : LSystem
{
    public float height = 1;
    public float horizontalVariance = 1;
    public float width = 1;
    public float baseRadius = 1;
    public float tipRadius = 1;
    public int verticalSamples = 10;
    public int horizontalSamples = 10;

    public AnimationCurve x;
    public AnimationCurve y;
    public AnimationCurve z;

    public AnimationCurve thickness;

    public override Vector3 GetRelativePosition(float time)
    {
        return new Vector3(x.Evaluate(time) * horizontalVariance, y.Evaluate(time) * height, z.Evaluate(time) * horizontalVariance);
    }

    public override Mesh MakeSelfMesh(float time)
    {
        List<Line> lines = new List<Line>();
        for(int i = 0;i<verticalSamples;i++)
        {
            float verticalTime = i / (verticalSamples - 1f);
            Vector3 center = GetRelativePosition(verticalTime * time);
            float radius = thickness.Evaluate(verticalTime * time) * time * width;
            List<Vector3> points = new List<Vector3>();
            for(int j = 0;j<horizontalSamples;j++)
            {
                float horizontalTime = j / (horizontalSamples - 1f);
                float theta = Mathf.PI * 2 * horizontalTime;
                float x = Mathf.Sin(theta) * radius;
                float z = Mathf.Cos(theta) * radius;
                points.Add(center + new Vector3(x,0,z));
            }
            lines.Add(new Line(){points = points});
        }
        Mesh mesh = new Face(){lines = lines, pointsPerLine = horizontalSamples}.MakeMesh();
        MeshUtils.Flip(mesh);
        return mesh;
    }
}