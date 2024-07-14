using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BendySegmentMono : LSystemMonoV2 {
    public Material mat;
    public float growTime;

    private float totalGrowTime;
    private float growStartTime;
    public bool growing;
    public bool alwaysUpdate;

    private float time;

    public BendySegment lSystem;

    public int numSegments = 5;
    
    Spline spline;

    public Spline MakeSpline(float length)
    {
        float noise = .3f;
        int numPoints = 10;
        List<Vector3> controlPoints = new List<Vector3>();
        controlPoints.Add(new Vector3(0,0,0));
        controlPoints.Add(new Vector3(0,1,0));
        /*float dh = length / (numPoints - 1f);
        for(int i = 1;i<numPoints;i++)
        {
            float xDeviation = Random.Range(-noise, noise);
            float zDeviation = Random.Range(-noise, noise);
            controlPoints.Add(new Vector3(controlPoints[i - 1].x + xDeviation * dh, i * dh, controlPoints[i-1].z + zDeviation * dh));
        }*/
        return new CatmullRomSpline(controlPoints,50);
    }

    public override void ConfigureLSystem()
    {
        spline = MakeSpline(1);
        lSystem = new BendySegment(spline, numSegments);
        lSystem.gameObject.transform.parent = transform;
        lSystem.mono = this;
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
