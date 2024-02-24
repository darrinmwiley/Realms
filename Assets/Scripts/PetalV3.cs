using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO bake curvatures into instance vars and add realtime render option back
//from there iterate on curvature - need to unity rose vs lily with some fancy way of representing the center

public class PetalV3 : MonoBehaviour
{
    public bool regenerateMode;
    public Material mat;
    //maybe add multiplier for this as well
    public AnimationCurve curvature;
    public AnimationCurve width;
    public float lengthMultiplier;
    public float widthMultiplier;
    public int lengthSamples;

    public int widthSamples;

    public AnimationCurve horizontalCurvature;
    public float horizontalCurvatureMultiplier;

    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;

    void Start()
    {
        if(meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
        if(meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        if(mat != null)
            SetMaterial(mat);
        RegenerateMesh();
    }

    void Update(){
        if(regenerateMode){
            RegenerateMesh();
        }
    }

    public void SetMaterial(Material mat)
    {
        meshRenderer.sharedMaterial = mat;
    }

    public void RegenerateMesh(){
        meshFilter.mesh = GenerateMesh();
    }

    public Mesh GenerateMesh()
    {
        List<Line> lines = new List<Line>();
        for(int i = 0;i<lengthSamples;i++)
        {
            float lengthTime = i / (lengthSamples - 1f);
            List<Vector3> points = new List<Vector3>();
            for(int j = 0;j<widthSamples;j++)
            {
                float widthTime = j / (widthSamples - 1f);
                float x = lengthTime * lengthMultiplier;
                float y = curvature.Evaluate(lengthTime) + horizontalCurvature.Evaluate(widthTime) * horizontalCurvatureMultiplier;
                float startZ = -widthMultiplier * width.Evaluate(lengthTime);
                float endZ = widthMultiplier * width.Evaluate(lengthTime);
                float z = Mathf.Lerp(startZ, endZ, widthTime);
                points.Add(new Vector3(x,y,z));
            }
            lines.Add(new Line(){points = points});
        }
        Mesh front = new Face(){pointsPerLine = widthSamples, lines = lines}.MakeMesh();
        MeshUtils.Flip(front);
        Mesh back = new Face(){pointsPerLine = widthSamples, lines = lines}.MakeMesh();
        return MeshUtils.Combine(front, back);
    }
}
