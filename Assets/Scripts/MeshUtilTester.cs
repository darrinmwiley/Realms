using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshUtilTester : MonoBehaviour
{

    public Material mat;

    public AnimationCurve petalWidthCurve;
    public float petalWidthMultiplier;
    public AnimationCurve petalThicknessCurve;
    public float petalThicknessMultiplier;
    public float petalHeight;

    int samples = 25;

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
        GameObject obj = new GameObject();
        obj.AddComponent<MeshFilter>().mesh = GenerateMesh();
        obj.AddComponent<MeshRenderer>().material = mat;
    }

    public Mesh GenerateMesh(){
        List<Line> lines = new List<Line>();
        List<Line> backLines = new List<Line>();
        List<Line> leftLines = new List<Line>();
        List<Line> rightLines = new List<Line>();
        for(int i = 0;i<samples;i++)
        {
            float time = 1f / (samples - 1) * i;
            float width = petalWidthMultiplier * petalWidthCurve.Evaluate(time);
            float thickness = petalThicknessMultiplier * petalThicknessCurve.Evaluate(time);
            lines.Add(new Line(){
                start = new Vector3(-width / 2, time * petalHeight, -thickness / 2),
                end = new Vector3(width / 2, time * petalHeight, -thickness / 2)
            });
            backLines.Add(new Line(){
                start = new Vector3(-width / 2, time * petalHeight, thickness / 2),
                end = new Vector3(width / 2, time * petalHeight, thickness / 2)
            });
            leftLines.Add(new Line(){
                start = new Vector3(-width / 2, time * petalHeight, -thickness / 2),
                end = new Vector3(-width / 2, time * petalHeight, thickness / 2)
            });
            rightLines.Add(new Line(){
                start = new Vector3(width / 2, time * petalHeight, -thickness / 2),
                end = new Vector3(width / 2, time * petalHeight, thickness / 2)
            });
            
        }

        Mesh front = new Face(){lines = lines}.MakeMesh();
        Mesh back = new Face(){lines = backLines}.MakeMesh();
        Mesh left = new Face(){lines = leftLines}.MakeMesh();
        Mesh right = new Face(){lines = rightLines}.MakeMesh();
        
        MeshUtils.Flip(back);
        MeshUtils.Flip(left);

        return CombineMeshes(front, back, left, right);
    }

    

    // Update is called once per frame
    void Update()
    {

        
    }
}
