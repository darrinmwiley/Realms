using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO bake curvatures into instance vars and add realtime render option back
//from there iterate on curvature - need to unity rose vs lily with some fancy way of representing the center

public class Petal : MonoBehaviour
{
    public Material mat;

    //would like to detect changes in the following, and regenerate mesh when one changes
    public AnimationCurve petalWidthCurve;
    public float petalWidthMultiplier = 1;
    public AnimationCurve petalThicknessCurve;
    public float petalThicknessMultiplier = .15f;
    public float petalHeight = 1;
    public int verticalSamples = 10;
    public int horizontalSamples = 10;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetMaterial(Material mat)
    {
        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if(meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = mat;
    }

    public void SetDefaultCurves(){
        petalWidthCurve = new AnimationCurve(
                new Keyframe(0f, 1f),   // Start at time 0 with value 1.
                new Keyframe(1f, 0f)    // End at time 1 with value 0.
        ); 
        petalThicknessCurve = new AnimationCurve(
                new Keyframe(0f, 1f),   // Start at time 0 with value 1.
                new Keyframe(1f, 0f)    // End at time 1 with value 0.
        ); 
    }

    public void RegenerateMesh(){
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        if(meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = GenerateMesh();
    }

    public Mesh GenerateMesh()
    {
        float curvature = 0;
        float verticalCurvature = 0;

        List<Line> lines = new List<Line>();
        List<Line> backLines = new List<Line>();
        List<Line> leftLines = new List<Line>();
        List<Line> rightLines = new List<Line>();
        for(int i = 0;i<verticalSamples;i++)
        {
            float time = 1f / (verticalSamples - 1) * i;
            float width = petalWidthMultiplier * petalWidthCurve.Evaluate(time);
            float thickness = petalThicknessMultiplier * petalThicknessCurve.Evaluate(time);

            float bevelWidth = Mathf.Min(width / 4, thickness / 4);
            float bevelTotalTime = bevelWidth / width;

            List<Vector3> frontPoints = new List<Vector3>();
            List<Vector3> backPoints = new List<Vector3>();
            List<Vector3> leftPoints = new List<Vector3>();
            List<Vector3> rightPoints = new List<Vector3>();
            for(int j = 0;j<horizontalSamples;j++)
            {
                float horizontalTime = j / (horizontalSamples - 1f);
                float curvatureDz = curvature * thickness * (Mathf.Sin(horizontalTime * Mathf.PI));
                float verticalCurvatureDz = verticalCurvature * Mathf.Sin(time * Mathf.PI / 2);
                if(horizontalTime < bevelTotalTime || (1 - horizontalTime) < bevelTotalTime)
                {
                    float bevelTime = Mathf.Min(horizontalTime, 1-horizontalTime) / bevelTotalTime;
                    float dz = 1 - Mathf.Sqrt(1 - (1 - bevelTime)*(1 - bevelTime));
                    frontPoints.Add(new Vector3(horizontalTime * width - width / 2, time * petalHeight - verticalCurvatureDz, -thickness / 2 + dz*bevelWidth + curvatureDz + verticalCurvatureDz));
                    backPoints.Add(new Vector3(horizontalTime * width - width / 2, time * petalHeight - verticalCurvatureDz, thickness / 2 - dz*bevelWidth + curvatureDz + verticalCurvatureDz));
                }else{
                    frontPoints.Add(new Vector3(horizontalTime * width - width / 2, time * petalHeight - verticalCurvatureDz, -thickness / 2 + curvatureDz + verticalCurvatureDz));
                    backPoints.Add(new Vector3(horizontalTime * width - width / 2, time * petalHeight - verticalCurvatureDz, thickness / 2 + curvatureDz + verticalCurvatureDz));
                }
                float remainingThickness = thickness - 2 * bevelWidth;
                //leftPoints.Add(new Vector3(-width / 2, time * petalHeight, -thickness / 2 + bevelWidth + remainingThickness * horizontalTime));
                //rightPoints.Add(new Vector3(width / 2, time * petalHeight, -thickness / 2 + bevelWidth + remainingThickness * horizontalTime));
                
            }

            lines.Add(new Line(){
                points = frontPoints
            });

            backLines.Add(new Line(){
                points = backPoints
            });

            /*leftLines.Add(new Line(){
                points = leftPoints
            });
            rightLines.Add(new Line(){
                points = rightPoints
            });*/
            
        }

        Mesh front = new Face(){pointsPerLine = horizontalSamples, lines = lines}.MakeMesh();
        MeshUtils.PrintMeshDebugInfo(front);
        Mesh back = new Face(){pointsPerLine = horizontalSamples,lines = backLines}.MakeMesh();
       // Mesh left = new Face(){pointsPerLine = horizontalSamples,lines = leftLines}.MakeMesh();
       // Mesh right = new Face(){pointsPerLine = horizontalSamples,lines = rightLines}.MakeMesh();
        
        MeshUtils.Flip(back);
        //MeshUtils.Flip(left);

        return MeshUtils.Combine(front, back/*, left, right*/);
    }
}
