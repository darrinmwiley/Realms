using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshUtilTester : MonoBehaviour
{

    public Material mat;

    //would like to detect changes in the following, and regenerate mesh when one changes
    public AnimationCurve petalWidthCurve;
    public float petalWidthMultiplier;
    public AnimationCurve petalThicknessCurve;
    public float petalThicknessMultiplier;
    public float petalHeight;
    public int verticalSamples = 10;
    public int horizontalSamples = 10;

    public GameObject petal;

    public float lastRefreshTime = 0;

    // Start is called before the first frame update
    void Start()
    {
        petalWidthCurve = new AnimationCurve(
                new Keyframe(0f, 1f),   // Start at time 0 with value 1.
                new Keyframe(1f, 0f)    // End at time 1 with value 0.
        ); 
        petalThicknessCurve = new AnimationCurve(
                new Keyframe(0f, 1f),   // Start at time 0 with value 1.
                new Keyframe(1f, 0f)    // End at time 1 with value 0.
        ); 
        petal = new GameObject();
        petal.AddComponent<MeshFilter>().mesh = GenerateMesh();
        petal.AddComponent<MeshRenderer>().material = mat;
    }

    void Update(){
        if(Time.time - lastRefreshTime > .5)
        {
            petal.GetComponent<MeshFilter>().mesh = GenerateMesh();
        }
    }

    public Mesh GenerateMesh(){
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
                if(horizontalTime < bevelTotalTime || (1 - horizontalTime) < bevelTotalTime)
                {
                    float bevelTime = Mathf.Min(horizontalTime, 1-horizontalTime) / bevelTotalTime;
                    float dz = 1 - Mathf.Sqrt(1 - (1 - bevelTime)*(1 - bevelTime));
                    frontPoints.Add(new Vector3(horizontalTime * width - width / 2, time * petalHeight, -thickness / 2 + dz*bevelWidth));
                    backPoints.Add(new Vector3(horizontalTime * width - width / 2, time * petalHeight, thickness / 2 - dz*bevelWidth));
                }else{
                    frontPoints.Add(new Vector3(horizontalTime * width - width / 2, time * petalHeight, -thickness / 2));
                    backPoints.Add(new Vector3(horizontalTime * width - width / 2, time * petalHeight, thickness / 2));
                }
                float remainingThickness = thickness - 2 * bevelWidth;
                leftPoints.Add(new Vector3(-width / 2, time * petalHeight, -thickness / 2 + bevelWidth + remainingThickness * horizontalTime));
                rightPoints.Add(new Vector3(width / 2, time * petalHeight, -thickness / 2 + bevelWidth + remainingThickness * horizontalTime));
                
            }

            lines.Add(new Line(){
                points = frontPoints
            });

            backLines.Add(new Line(){
                points = backPoints
            });

            leftLines.Add(new Line(){
                points = leftPoints
            });
            rightLines.Add(new Line(){
                points = rightPoints
            });
            
        }

        Mesh front = new Face(){pointsPerLine = horizontalSamples, lines = lines}.MakeMesh();
        MeshUtils.PrintMeshDebugInfo(front);
        Mesh back = new Face(){pointsPerLine = horizontalSamples,lines = backLines}.MakeMesh();
        Mesh left = new Face(){pointsPerLine = horizontalSamples,lines = leftLines}.MakeMesh();
        Mesh right = new Face(){pointsPerLine = horizontalSamples,lines = rightLines}.MakeMesh();
        
        MeshUtils.Flip(back);
        MeshUtils.Flip(left);

        return MeshUtils.Combine(front, back, left, right);
    }
}
