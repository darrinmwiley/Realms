using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SplineLSystem : LSystem
{
    public Spline spline;
    public AnimationCurve thicknessCurve;

    public int verticalSamples = 10;
    public int horizontalSamples = 10;

    //TODO integrate into rendering logic
    //TODO integrate into builder
    //TODO add back parent child relationship code
    public AnimationCurve growthOverTime;
    public AnimationCurve thicknessOverTime;

    public SplineLSystem(
        float startTime, 
        float startOffset, 
        float growTime, 
        Vector3 localRotation, 
        Vector3 localPosition, 
        float scale, 
        LSystem parent,
        Spline spline,
        AnimationCurve thicknessCurve,
        int verticalSamples,
        int horizontalSamples)
        : base(startTime, startOffset, growTime, localRotation, localPosition, scale, parent){
        mono.thicknessCurve = thicknessCurve;
        this.spline = spline;
        this.thicknessCurve = thicknessCurve;
        this.verticalSamples = verticalSamples;
        this.horizontalSamples = horizontalSamples;
        growthOverTime = new AnimationCurve();
        growthOverTime.AddKey(0,0);
        growthOverTime.AddKey(1,1);
        thicknessOverTime = growthOverTime;
        gameObject.name = "Spline L-System";
    }

    public static SplineLSystem GetDefaultInstance(){
        return (SplineLSystem)(new SplineLSystemBuilder().Build());
    }

    //public override Vector3 GetRelativePosition(float time)
    //{
    //    return spline.Evaluate(time);
    //}

    public override Vector3 GetRelativePosition(float time, float offset){
        Vector3 ret = spline.Evaluate(growthOverTime.Evaluate(time) * offset);
        return ret;
    }

/*
    public override Mesh MakeSelfMesh(float time)
    {
        List<Line> lines = new List<Line>();
        for(int i = 0;i<verticalSamples;i++)
        {
            float verticalTime = i / (verticalSamples - 1f);
            Vector3 center = GetRelativePosition(verticalTime * time);
            float radius = thicknessCurve.Evaluate(time * verticalTime);
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
*/

    public override Mesh MakeSelfMesh(float time)
    {
        List<Line> lines = new List<Line>();
        for(int i = 0;i<verticalSamples;i++)
        {
            float verticalTime = i / (verticalSamples - 1f);
            Vector3 center = GetRelativePosition(time, verticalTime);
            float radius = thicknessCurve.Evaluate(time * verticalTime);
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