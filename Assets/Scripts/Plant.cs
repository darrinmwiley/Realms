using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plant : MonoBehaviour
{
    LSystem lSystem;
    public bool regenerateMode;

    public Material mat;
    
    public float growTime;
    public float totalGrowTime;
    float growStartTime;
    bool growthStarted;
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    public float height;
    public float baseRadius;
    public float tipRadius;
    public int verticalSamples;
    public int horizontalSamples;
    public AnimationCurve thicknessGrowthCurve;

    public Material material;
    void Start()
    {
        if(meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
        if(meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        if(mat != null)
            meshRenderer.sharedMaterial = mat;
            lSystem = new LSystem();
    }

    void Update()
    {
        if(regenerateMode && growthStarted){
            float time = GetTime();
            if(time >= totalGrowTime)
            {
                regenerateMode = false;
            }
            meshFilter.mesh = lSystem.MakeMesh(time);
        }
    }

    float GetTime()
    {
        if(growthStarted)
            return Mathf.Min(totalGrowTime,(Time.time - growStartTime) / growTime);
        return 0;
    }

    public void StartGrowth()
    {
        lSystem = new LSystem(){
            height = height,
            baseRadius = baseRadius,
            tipRadius = tipRadius,
            verticalSamples = verticalSamples,
            horizontalSamples = horizontalSamples,
            thicknessGrowthCurve = thicknessGrowthCurve
        };
        LSystem copy = new LSystem(){
            height = height,
            baseRadius = baseRadius,
            tipRadius = tipRadius,
            verticalSamples = verticalSamples,
            horizontalSamples = horizontalSamples,
            thicknessGrowthCurve = thicknessGrowthCurve
        };
        SubLsystem sub = new SubLsystem(){
            startTime = .5f,
            growTime = 1.5f,
            offsetX = 30,
            offsetZ = 60, 
            relativeScale = .5f,
            lSystem = copy
        };
        lSystem.subsystems = new List<SubLsystem>(){sub};
        totalGrowTime = lSystem.getTotalTime();
        growthStarted = true;
        growStartTime = Time.time;
    }
}
