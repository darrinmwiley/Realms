using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeLSystemSpec : MonoBehaviour
{

    //TODO: move "Plant" implementation into LSystemMono, as well as the editor tooling for it
    //TODO: start looking into adding back segmented l systems

    //phase one is trunk. most plans have a "trunk" of sorts, whos goal is just to grow up
    //phase two is branches. most plants also have a sort of "branch" whose purpose is to move outward to secure more sun, above other competitors, and ideally not even competing with itself

    //trunks have a 
    //avg segment size
    //max num segments
    //path selection spec
    //branching spec

    //at beginning of lifetime we roll:

    //num height segments
    //num branches

    public float minBranchHeight = .3f;
    public float maxBranchHeight = .8f;
    //in degrees
    public float branchRotation = 137.5f;
    public int numBranches = 4;
    public float height = 10;
    public float baseWidth = 3;
    //if generations is zero, don't make more sub-branches
    public int generations = 1;

    public Material mat;
    public float growTime;

    private float totalGrowTime;
    private float growStartTime;
    public bool growing;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    private float time;

    public LSystem lSystem;

    public void Start()
    {
        AnimationCurve thicknessCurve = new AnimationCurve();
        thicknessCurve.AddKey(new Keyframe(0,baseWidth));
        thicknessCurve.AddKey(new Keyframe(.8f,baseWidth * .8f));
        thicknessCurve.AddKey(new Keyframe(1,0));
        List<Vector3> controlPoints = new List<Vector3>(){new Vector3(0,0,0), new Vector3(0,height,0)};
        lSystem = new SegmentedLSystemBuilder()
            .SetNumSegments(4)
            .SetSpline(MakeTreeSpline())
            .SetVerticalSamples(50)
            .SetThicknessCurve(thicknessCurve)
            .Build();
        lSystem.SetMaterial(mat);
        if(meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
        if(meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        if(mat != null)
            meshRenderer.sharedMaterial = mat;
        float dt = (maxBranchHeight - minBranchHeight) / (numBranches - 1);
        for(int i = 0;i<numBranches;i++)
        {
            AnimationCurve branchCurve = new AnimationCurve();
            branchCurve.AddKey(new Keyframe(0,baseWidth));
            branchCurve.AddKey(new Keyframe(.8f,baseWidth * .8f));
            branchCurve.AddKey(new Keyframe(1,0));
            float time = Random.Range(minBranchHeight, maxBranchHeight);
            float thetaY = branchRotation * i;
            LSystem sub = new SegmentedLSystemBuilder()
                .SetNumSegments(10)
                .SetSpline(MakeTreeSpline())
                .SetThicknessCurve(branchCurve)
                .SetLocalScale(.5f)
                .SetLocalRotation(new Vector3(60, thetaY, 0))
                .SetStartTime(.9f)
                .SetStartOffset(time)
                .Build();
            lSystem.AddSubSystem(sub);
            sub.SetMaterial(mat);
        }
    }

    public Spline MakeTreeSpline()
    {
        float noise = .3f;
        int numPoints = 10;
        List<Vector3> controlPoints = new List<Vector3>();
        controlPoints.Add(new Vector3(0,0,0));
        float dh = height / (numPoints - 1f);
        for(int i = 1;i<numPoints;i++)
        {
            float xDeviation = Random.Range(-noise, noise);
            float zDeviation = Random.Range(-noise, noise);
            controlPoints.Add(new Vector3(controlPoints[i - 1].x + xDeviation * dh, i * dh, controlPoints[i-1].z + zDeviation * dh));
        }
        return new CatmullRomSpline(controlPoints,50);
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
            UpdateMesh();
        }
    }

    
    void UpdateMesh()
    {
        /*List<Mesh> meshes = lSystem.MakeMeshes(time * lSystem.GetTotalTime());
        while(components.Count < meshes.Count){
            GameObject go = new GameObject();
            go.name = components.Count + "";
            components.Add(go);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }
        for(int i = 0;i<meshes.Count;i++)
            components[i].GetComponent<MeshFilter>().mesh = meshes[i];*/
        //meshFilter.mesh = lSystem.MakeMesh(time * lSystem.GetTotalTime());

        lSystem.UpdateMesh(time * lSystem.GetTotalTime());
    }

    public void SetTime(float time)
    {
        this.time = time;
        UpdateMesh();
    }

    public float GetTime()
    {
        return time;
    }
}