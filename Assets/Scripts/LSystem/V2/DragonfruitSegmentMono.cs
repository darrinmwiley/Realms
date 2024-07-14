using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragonfruitSegmentMono : LSystemMonoV2 {
    public Material mat;
    public float growTime;

    private float totalGrowTime;
    private float growStartTime;
    public bool growing;
    public bool alwaysUpdate;

    private float time;

    public DragonfruitSegment lSystem;
    public AnimationCurve thicknessCurve;
    public AnimationCurve thicknessGrowthCurve;
    public AnimationCurve taperCurve;

    public float innerRadius = .3f;
    public float outerRadius = 1;

    public int numSegments = 5;
    public int samples = 100;
    
    Spline spline;

    public Spline MakeSpline(float length)
    {
        float noise = .3f;
        int numPoints = 10;
        List<Vector3> controlPoints = new List<Vector3>();
        controlPoints.Add(new Vector3(0,0,0));
        float dh = length / (numPoints - 1f);
        for(int i = 1;i<numPoints;i++)
        {
            float xDeviation = Random.Range(-noise, noise);
            float zDeviation = Random.Range(-noise, noise);
            controlPoints.Add(new Vector3(controlPoints[i - 1].x + xDeviation * dh, i * dh, controlPoints[i-1].z + zDeviation * dh));
        }
        return new CatmullRomSpline(controlPoints,50);
    }

    public override void ConfigureLSystem()
    {
        if(lSystem == null){
            spline = MakeSpline(1);
            lSystem = new DragonfruitSegment(spline, mat, innerRadius, outerRadius, samples, thicknessGrowthCurve);
            lSystem.gameObject.transform.parent = transform;
            lSystem.mono = this;
        }
        else
            lSystem.Configure(spline, mat, innerRadius, outerRadius, samples, thicknessGrowthCurve);
    }

    void Update()
    {
        if(growing){
            time += (Time.deltaTime / growTime);
            if(time >= 1)
            {
                growing = false;
                time = 1;
            }
            lSystem.Update(time);
        }
        if(alwaysUpdate){
            ConfigureLSystem();
            lSystem.Update(time);
        }
    }

    public void SetTime(float time)
    {
        this.time = time;
    }

    public float GetTime()
    {
        return time;
    }
}
