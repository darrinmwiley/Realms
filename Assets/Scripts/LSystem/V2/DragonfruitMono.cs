using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragonfruitMono : LSystemMonoV2 {
    public Material mat;

    private float growStartTime;
    public bool growing;
    private bool wasGrowingLastUpdate;

    public Dragonfruit lSystem;
    public AnimationCurve thicknessGrowthCurve;

    public GameObject plane;

    public override void ConfigureLSystem()
    {
        lSystem = new Dragonfruit(mat, thicknessGrowthCurve, plane);
        lSystem.gameObject.transform.parent = transform;
        transform.position = plane.transform.TransformPoint(0,0,-5);
        lSystem.gameObject.transform.localPosition = new Vector3(0,0,0);
        lSystem.mono = this;
    }

    void Update()
    {
        if(growing){
            if(!wasGrowingLastUpdate)
            {
                growStartTime = Time.time;
            }
            lSystem.Update(Time.time - growStartTime);
        }
        wasGrowingLastUpdate = growing;
    }
}
