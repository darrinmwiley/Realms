using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SplineLSystem : LSystem
{
    //1: unit spline
    //2: scale
    //3) radius / time (even this can be followup)
    private Spline spline;
    //todo make this an animation curve instead

    //how thick each part of the plant is at full growth
    public AnimationCurve thicknessCurve;

    //how the plant "approaches" the full thickness
    public AnimationCurve thicknessMultiplierOverTime;

    public float thicknessMultiplier;

    public int verticalSamples = 10;
    public int horizontalSamples = 10;
    public Transform[] controlPoints;

    public void Start(){
        base.Start();
        List<Vector3> pts = new List<Vector3>();
        foreach(Transform t in controlPoints){
            pts.Add(t.position);
        }
        Debug.Log(pts);
        spline = new CatmullRomSpline(pts, 50);
    }

    public override Vector3 GetRelativePosition(float time)
    {
        return spline.Evaluate(time);
    }

    public override Mesh MakeSelfMesh(float time)
    {
        List<Line> lines = new List<Line>();
        for(int i = 0;i<verticalSamples;i++)
        {
            float verticalTime = i / (verticalSamples - 1f);
            Vector3 center = GetRelativePosition(verticalTime * time);
            float radius = thicknessCurve.Evaluate(verticalTime) * thicknessMultiplierOverTime.Evaluate(time) * thicknessMultiplier;
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