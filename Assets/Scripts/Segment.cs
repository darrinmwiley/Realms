using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO: SEGMENTS SHOULD HAVE AN ABSOLUTE WIDTH
//TODO: SPLIT LONG SOCKET SEGMENT INTO MULTIPLE

public class Segment : MonoBehaviour
{
    public GameObject body;
    public GameObject rendering;
    public float growTime;
    public float growStartTime = -1;
    public Vector3 start;
    public Vector3 end;

    //front right left front right left etc... TODO organize
    public List<Vector3> nodeLocations = new List<Vector3>();

    public Material green;

    public List<Segment> children = new List<Segment>();
    public Segment parent;

    public bool IsGrown(){
        return growStartTime != -1 && Time.time - growStartTime > growTime;
    }

    public void StartGrowth(){
        growStartTime = Time.time;
    }

    public void Init(Material green){
        green = green;
        rendering = new GameObject("Dragonfruit Mesh");
        rendering.AddComponent<MeshFilter>();
        rendering.AddComponent<MeshRenderer>().material = green;
        rendering.transform.parent = body.transform;
        rendering.transform.localPosition = new Vector3(0,0,0);
        rendering.transform.localScale = new Vector3(1,2,1);
        rendering.transform.localRotation = Quaternion.Euler(0,0,0);
        Mesh mesh = MakeMesh(.3f, 1, 50);
        rendering.active = false;
        rendering.GetComponent<MeshFilter>().mesh = mesh;
    }

    void Start(){
        
    }

    // Update is called once per frame
    void Update()
    {
        if(!IsGrown() && growStartTime != -1)
        {
            float scale = (Time.time - growStartTime) / growTime;
            Resize(scale);
        }
    }

    public void Resize(float scale)
    {
        Vector3 direction = end - start;
        float fullHeight = direction.magnitude;

        // Interpolate the height based on the y component of the scale
        float height = fullHeight * scale;
        
        // Adjust the capsule position so that its bottom is always at the 'bottom' position
        Vector3 capsuleCenter = start + (direction.normalized * (height / 2.0f));
        body.transform.position = capsuleCenter;

        // Align the capsule with the direction vector
        body.transform.up = direction.normalized;

        // Scale the capsule
        // Default capsule height in Unity is 2 units at localScale.y = 1
        float capsuleDefaultHeight = 2.0f;
        float scaleFactor = height / capsuleDefaultHeight;
        Vector3 newScale = new Vector3(.2f*scaleFactor, scaleFactor,.2f*scaleFactor);

        body.transform.localScale = newScale;
    }

    public float InterpolateOuterRadius(float outerRadiusMin, float outerRadiusMax, float y, float innerRadius)
    {
        float origY = y;
        y = (y+.5f)*3%1-.5f;
        float ans = 4 * (outerRadiusMin - outerRadiusMax) * y * y + outerRadiusMax;
        if(origY < -.3f)
        {
            float percent = (-.3f - origY) / .2f;
            return Mathf.Lerp(ans, innerRadius, percent);
        }else if(origY > .3f)
        {
            float percent = (.5f - origY) / .2f;
            return Mathf.Lerp(innerRadius, ans, percent);
        }else{
            return ans;
        }
        
    }

    public float InterpolateOuterRadius2(float percentage)
    {
        return .25f * Mathf.Sin(4*percentage * Mathf.PI * 2) + .75f + .1f*Mathf.Sin(20 * percentage * Mathf.PI * 2);
    }

    //samples must be at least 2
    public Mesh MakeMesh(float innerRadius, float outerRadius, int samples)
    {
        Vector3[] vertices = new Vector3[6 * samples];
        float height = 1;
        float dy = height / (samples - 1);

        for(int s = 0;s<samples;s++)
        {
            for(int i = 0;i<6;i++)
            {
                float sin = Mathf.Sin(Mathf.PI / 3 * i);
                float cos = Mathf.Cos(Mathf.PI / 3 * i);
                if((i & 1) == 0)
                {
                    float r = outerRadius * InterpolateOuterRadius(.9f, 1.1f, -.5f + dy * s, innerRadius);
                    if(s != 0 && s != samples - 1)
                    {
                        float prevR = outerRadius * InterpolateOuterRadius(.9f, 1.1f, -.5f + dy * (s-1), innerRadius);
                        float nextR = outerRadius * InterpolateOuterRadius(.9f, 1.1f, -.5f + dy * (s+1), innerRadius);
                        if(prevR < r && r >= nextR)
                        {
                            Vector3 location = rendering.transform.TransformPoint(new Vector3(r*cos, -.5f + dy * s, r * sin));
                            nodeLocations.Add(location);
                            GameObject node = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            node.name = i+"";
                            node.transform.position = location;
                            node.transform.localScale = new Vector3(.01f,.01f, .01f);
                        }
                    }
                    vertices[s * 6 + i] = new Vector3(r * cos, -.5f + dy * s, r * sin);
                }else{
                    vertices[s * 6 + i] = new Vector3(innerRadius * cos, -.5f + dy * s, innerRadius * sin);
                }
            }
        }

        Mesh mesh = new Mesh();

        //lets start with 12 faces
        int[] triangles = new int[36 * (samples - 1)];
        for(int s = 0;s<samples-1;s++)
        {
            for(int i = 0;i<6;i++)
            {
                triangles[s * 36 + i * 6] = i + s * 6;
                triangles[s * 36 + i * 6+1] = (i+1) % 6 + 6 + s * 6;
                triangles[s * 36 + i * 6+2] = (i+1) % 6 + s * 6;
                triangles[s * 36 + i * 6+3] = i + s * 6;
                triangles[s * 36 + i * 6+4] = i + 6 + s * 6;
                triangles[s * 36 + i * 6+5] = (i+1) % 6 + 6 + s * 6;
            }
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
}
