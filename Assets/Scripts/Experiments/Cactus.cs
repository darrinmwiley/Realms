using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cactus : MonoBehaviour
{
    public int baseHeight = 3;
    public int numVertices = 3;

    public float flexibility = 5;
    public AnimationCurve thicknessCurve;
    public float thickness;

    public Material mat;

    public float timeToGrow;
    public bool growing;

    CactusSegment cactusSegment;

    List<CactusSegment> segments = new List<CactusSegment>();

    ArticulationBody rootArticulation;
    // Start is called before the first frame update
    void Start()
    {
        rootArticulation = gameObject.AddComponent<ArticulationBody>();
        rootArticulation.immovable = true;
        cactusSegment = new CactusSegment(gameObject, Vector3.up, Spline.Direction(Vector3.up * baseHeight), thicknessCurve, thickness, numVertices, flexibility, mat);
        segments.Add(cactusSegment);
    }

    void AddRandomSegment(){
        CactusSegment parent = segments[UnityEngine.Random.Range(0,segments.Count)];
        CactusSegment child = parent.AddChild(UnityEngine.Random.Range(0f, 1f), RandomDirection(), Spline.Direction(Vector3.up * baseHeight));
        segments.Add(child);
    }

    public Vector3 RandomDirection()
    {
        // Generate a random angle for XZ plane (horizontal direction)
        float angleXZ = Random.Range(0f, Mathf.PI * 2); // Full 360 degrees in radians

        // Set a random magnitude for X and Z based on the angle
        float x = Mathf.Cos(angleXZ);
        float z = Mathf.Sin(angleXZ);
        
        // Random value for Y, ensuring it's always positive
        float y = Random.Range(0.1f, 1f); // Adjust range as needed for positive values

        // Combine into a direction vector
        Vector3 direction = new Vector3(x, y, z);
        
        // Normalize the direction to ensure it's a unit vector
        return direction.normalized;
    }

    // Update is called once per frame
    void Update()
    {
        if(growing){
            bool fullGrown = true;
            for(int i = 0;i<segments.Count;i++)
            {
                CactusSegment seg = segments[i];
                float newGrowth = Mathf.Min(seg.GetGrowth() + Time.deltaTime / timeToGrow, 1);
                if(newGrowth != 1)
                    fullGrown = false;
                seg.SetGrowth(newGrowth);
                seg.UpdateMesh();
            }
            if(fullGrown)
                AddRandomSegment();   
        }
    }
}
