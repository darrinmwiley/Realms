using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

//position: <time, offset>
//update: time

public class Node{
    public Vector3 location;
    public Vector3 orthagonalDirection;
    public Vector3 segmentDirection;
}

public class DragonfruitSegment : LSystemV2<Vector2, float>
{
    private GameObject meshObj;

    public int horizontalSamples = 6;

    public Spline spline;
    public AnimationCurve thicknessGrowthCurve;
    public AnimationCurve growthOverTime;

    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;

    //local position of nodes
    public List<Node> nodes;

    public float innerRadius; 
    public float outerRadius;
    public int samples;

    int numSegments = 6;

    public List<DragonfruitSegment> children;
    public float lastUpdateTime;

    //GameObject tip;

    public DragonfruitSegment(Spline spline, Material mat, float innerRadius, float outerRadius, int samples, AnimationCurve thicknessGrowthCurve){
        //tip = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        //tip.transform.parent = gameObject.transform;
        gameObject.name = "Dragonfruit Segment";
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.convex = true;
        growthOverTime = new AnimationCurve();
        growthOverTime.AddKey(0,0);
        growthOverTime.AddKey(1,1);

        Configure(spline, mat, innerRadius, outerRadius, samples, thicknessGrowthCurve);
        children = new List<DragonfruitSegment>();
    }

    public void AddChild(DragonfruitSegment seg)
    {
        children.Add(seg);
        //seg.gameObject.transform.parent = gameObject.transform.parent;
    }

    public void Configure(Spline spline, Material mat, float innerRadius, float outerRadius, int samples, AnimationCurve thicknessGrowthCurve)
    {
        this.spline = spline;
        meshRenderer.sharedMaterial = mat;
        this.innerRadius = innerRadius;
        this.outerRadius = outerRadius;
        this.samples = samples;
        this.thicknessGrowthCurve = thicknessGrowthCurve;
    }

    public float GetMass()
    {
        float ans = Mathf.Min(1,lastUpdateTime);
        foreach(DragonfruitSegment seg in children)
        {
            ans += seg.GetMass();
        }
        return ans;
    }

    public override void Update(float time){
        lastUpdateTime = time;
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        if(rb != null)
            rb.mass = GetMass();
        if(time >= 1 && nodes == null)
        {
            nodes = new List<Node>();
            CalculateNodes();
        }
        //tip.transform.localPosition = GetPositionLocal(new Vector2(time, 1));
        //tip.transform.localScale = new Vector3(.01f,.01f,.01f);
        //tip.transform.up = GetDirectionLocal(new Vector2(time, 1));
        //TODO: set capsule collider component on gameObject to be a capsule of width outerRadius from GetPositionLocal(time, 0) to GetPositionLocal(time, 1)
        if(time < 1)
            meshCollider.sharedMesh = meshFilter.mesh = MakeMesh(time);
    }

    public override Vector3 GetPositionLocal(Vector2 timeAndOffset){
        float time = timeAndOffset.x;
        float offset = timeAndOffset.y;
        Vector3 ret = spline.Evaluate(growthOverTime.Evaluate(time) * offset);
        return ret;
    }

    public override Vector3 GetDirectionLocal(Vector2 timeAndOffset){
        float epsilon = .1f;
        return (GetPositionLocal(timeAndOffset) - GetPositionLocal(timeAndOffset - new Vector2(0,epsilon))).normalized;
    }

    public override Vector3 GetDirectionAbsolute(Vector2 timeAndOffset)
    {
        float epsilon = .1f;
        Vector3 ans = (GetPositionAbsolute(timeAndOffset) - GetPositionAbsolute(timeAndOffset - new Vector2(0,epsilon))).normalized;
        return ans;
    }

    public void CalculateNodes()
    {
        Vector3[] vertices = new Vector3[6 * samples];
        float dy = 1f / (samples - 1);

        //for each sample height, if you are within X% of the top, lerp from 0 to desired width

        for(int s = 0;s<samples;s++)
        {
            bool localMinimum = false;
            float r = outerRadius * InterpolateOuterRadius(.9f, 1.1f, -.5f + dy * s, innerRadius, numSegments);
            float vOffset = (float) s / (samples - 1);
            Vector3 currentPos = spline.Evaluate(vOffset);
            Vector3 prevPos = s > 0 ? spline.Evaluate((float)(s - 1) / (samples - 1)) : currentPos - Vector3.up;
            Vector3 direction = (currentPos - prevPos).normalized;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction).normalized;

            
            if(s != 0 && s != samples - 1)
            {
                float prevR = outerRadius * InterpolateOuterRadius(.9f, 1.1f, -.5f + dy * (s-1), innerRadius, numSegments);
                float nextR = outerRadius * InterpolateOuterRadius(.9f, 1.1f, -.5f + dy * (s+1), innerRadius, numSegments);
                if(prevR > r && r < nextR)
                {
                    Debug.Log(prevR+" "+r+" "+nextR);
                    localMinimum = true;
                }
            }
            //List<Node> ring = new List<Node>();
            for(int i = 0;i<6;i++)
            {
                float verticalTime = s / (samples - 1f);
                float sin = Mathf.Sin(Mathf.PI / 3 * i);
                float cos = Mathf.Cos(Mathf.PI / 3 * i);
                //tapering:
                //TODO: make tapering be on a curve instead of linear, frontload the horizontal growth
                float taperTime = .3f;
                float taper;
                if(verticalTime == 1){
                    taper = 0;
                }
                else{
                    float taperEndTime = Mathf.Min(1, verticalTime + taperTime);
                    taper = thicknessGrowthCurve.Evaluate(Mathf.Min(1,Mathf.Max(0,(1 - verticalTime) / (taperEndTime - verticalTime))));
                }
                if((i & 1) == 0)
                {   
                    Vector3 baseOffset = new Vector3(taper * r * cos, 0, taper * r * sin);
                    Vector3 offset = rotation * baseOffset;
                    Vector3 vertex = currentPos + offset;
                    if(localMinimum)
                    {
                        GameObject node = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        node.GetComponent<Collider>().enabled = false;
                        node.transform.position = gameObject.transform.TransformPoint(vertex);
                        node.transform.localScale = new Vector3(.01f, .01f, .01f);
                        node.transform.parent = gameObject.transform;
                        nodes.Add(new Node(){
                            location = node.transform.position,
                            orthagonalDirection = (node.transform.position - gameObject.transform.TransformPoint(currentPos)).normalized,
                            segmentDirection = GetDirectionAbsolute(new Vector2(1, vOffset))
                        });
                        /*ring.Add(new Node(){
                            location = gameObject.transform.TransformPoint(vertex),
                            used = false,
                        });*/
                        /*GameObject node = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        node.name = i+"";
                        node.transform.parent = gameObject.transform;
                        node.transform.localPosition = vertex;
                        node.transform.localScale = new Vector3(.01f,.01f, .01f);*/
                    }
                }
            }
        }
    }

    //samples must be at least 2
    //radius and whatnot are given in absolutes
    public Mesh MakeMesh(float time)
    {
        Vector3[] vertices = new Vector3[6 * samples];
        float dy = 1f / (samples - 1);

        //for each sample height, if you are within X% of the top, lerp from 0 to desired width

        for(int s = 0;s<samples;s++)
        {
            bool localMinimum = false;
            float r = outerRadius * InterpolateOuterRadius(.9f, 1.1f, -.5f + dy * s, innerRadius, numSegments);
            Vector3 currentPos = spline.Evaluate((float)s / (samples - 1));
            Vector3 prevPos = s > 0 ? spline.Evaluate((float)(s - 1) / (samples - 1)) : currentPos - Vector3.up;
            Vector3 direction = (currentPos - prevPos).normalized;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction).normalized;

            //List<Node> ring = new List<Node>();
            for(int i = 0;i<6;i++)
            {
                float verticalTime = s / (samples - 1f);
                float sin = Mathf.Sin(Mathf.PI / 3 * i);
                float cos = Mathf.Cos(Mathf.PI / 3 * i);
                //tapering:
                //TODO: make tapering be on a curve instead of linear, frontload the horizontal growth
                float taperTime = .3f;
                float taper;
                if(verticalTime == 1){
                    taper = 0;
                }
                else{
                    float taperEndTime = Mathf.Min(1, verticalTime + taperTime);
                    taper = thicknessGrowthCurve.Evaluate(Mathf.Min(1,Mathf.Max(0,(time - verticalTime) / (taperEndTime - verticalTime))));
                }
                if((i & 1) == 0)
                {   
                    Vector3 baseOffset = new Vector3(taper * r * cos, 0, taper * r * sin);
                    Vector3 offset = rotation * baseOffset;
                    Vector3 vertex = currentPos + offset;
                    vertices[s * 6 + i] = vertex;
                }else{
                    //tapering:
                    Vector3 offset = rotation * new Vector3(taper * innerRadius * cos, 0, taper * innerRadius * sin);
                    Vector3 vertex = currentPos + offset;
                    vertices[s * 6 + i] = vertex;
                }
            }
            //if(localMaximum)
            //    rings.Add(ring);
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

    public float InterpolateOuterRadius(float outerRadiusMin, float outerRadiusMax, float y, float innerRadius, int numSegments)
    {
        if(y < -.3f)
        {
            float percent = (-.3f - y) / .2f;
            return Mathf.Lerp(outerRadiusMin, innerRadius, percent);
        }else if(y > .4f)
        {
            float percent = (.5f - y) / .1f;
            return Mathf.Lerp(innerRadius, outerRadiusMin, percent);
        }
        y = ((y - -.3f) / .7f)-.5f;
        y = (y+.5f)*numSegments%1-.5f;
        float ans = 4 * (outerRadiusMin - outerRadiusMax) * y * y + outerRadiusMax;
        return ans;
    }

    /*public float InterpolateOuterRadius(float outerRadiusMin, float outerRadiusMax, float y, float innerRadius, int numSegments)
    {
        float origY = y;
        y = (y+.5f)*numSegments%1-.5f;
        float ans = 4 * (outerRadiusMin - outerRadiusMax) * y * y + outerRadiusMax;
        if(origY < -.3f)
        {
            float percent = (-.3f - origY) / .2f;
            return Mathf.Lerp(ans, innerRadius, percent);
        }else if(origY > .4f)
        {
            float percent = (.5f - origY) / .1f;
            return Mathf.Lerp(innerRadius, ans, percent);
        }else{
            return ans;
        }
    }*/
}