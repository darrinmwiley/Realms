using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class CylinderLSystem : LSystem
{
    public float height = 1;
    public float baseRadius = 1;
    public float tipRadius = 1;
    public int verticalSamples = 10;
    public int horizontalSamples = 10;

    //public AnimationCurve thicknessGrowthCurve;

    public override Vector3 GetRelativePosition(float time)
    {
        return new Vector3(0, height * time, 0);
    }

    public override Mesh MakeSelfMesh(float time)
    {
        List<Line> lines = new List<Line>();
        for(int i = 0;i<verticalSamples;i++)
        {
            float verticalTime = i / (verticalSamples - 1f);
            float y = verticalTime * time * height;
            float radius = Mathf.Lerp(baseRadius, tipRadius, verticalTime);// * thicknessGrowthCurve.Evaluate(time);
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
        return mesh;
    }
}