using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// idea can we have each segment of an L system apply a repulsive force on others
// and plants joints harden as they develop
// plants start close to weightless or weight can vary to influence growth patterns
public class LSystem
{
    [Header("Stats")]
    //when does this offshoot begin growing, relative to parent segment grow time
    public float startTime = 0;
    //where does this offshoot begin growing, normalized to parent.
    public float startOffset = 0;
    //how long does it take this segment to be fully grown, not counting children of this segment.
    //Normalized to parent grow time
    public float growTime = 1;
    //local rotation relative to the direction of the parent at startTime
    public Vector3 localRotation;
    //local position relative to the direction of the parent at startTime
    public Vector3 localPosition;
    public LSystem parent;

    public List<LSystem> subSystems = new List<LSystem>();

    public GameObject gameObject;
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    public LSystemMono mono;
    //TODO add parent field

    public LSystem(
        float startTime, 
        float startOffset, 
        float growTime, 
        Vector3 localRotation, 
        Vector3 localPosition, 
        float scale, 
        LSystem parent){
        this.startTime = startTime;
        this.startOffset = startOffset;
        this.growTime = growTime;
        this.localRotation = localRotation;
        this.localPosition = localPosition;
        this.parent = parent;
        if(parent != null){
            parent.AddSubSystem(this);
            //origin = parent.GetAbsolutePosition(startOffset) + localPosition;
            //rotation = parent.GetDirection(startOffset) * LocalRotation();
            //scale = parent.scale * localScale;
            SetParent(parent);
        }
        gameObject = new GameObject("LSystem");
        mono = gameObject.AddComponent<LSystemMono>();
        mono.lSystem = this;
        gameObject.transform.localScale = new Vector3(1,1,1)*scale;
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshFilter = gameObject.AddComponent<MeshFilter>();
    }

    public Quaternion LocalRotation()
    {
        return Quaternion.Euler(localRotation.x, localRotation.y, localRotation.z);
    }

    //this is relative to L system growth time - 
    // i.e. if getTotalTime returns 3, although this LSystem will be fully developed at time T, all children won't until 3T
    public float GetTotalTime()
    {
        if(subSystems.Count == 0)
        {
            return 1;
        }
        float max = 0;
        foreach(LSystem sub in subSystems)
        {
            max = Mathf.Max(max, sub.GetTotalTime() + sub.startTime);
        }
        return max;
    }

    //segments have animation curves for length over time, girth over time, and "growth" over time
    //growth being only rendering a portion of the entire mesh at a given time, to give the appearance of new growth coming from the tip

    //relativePosition(time, offset) :
    //1) use "Time" to create a partially grown L-system
    //2) "Offset" is what percentage up that partially grown L-system we wanna be
    public virtual Vector3 GetRelativePosition(float time, float offset){
        throw new NotImplementedException("GetRelativePosition needs to be implemented by the child class");
    }

    //TODO: need to make this a function of offset and time
    //assuming that position and rotation are 0, and scale is 1, get position of a certain "time" on the segment
    //public virtual Vector3 GetRelativePosition(float time){
    //    throw new NotImplementedException("GetRelativePosition needs to be implemented by the child class");
    //}

    //returns the absolute position of a certain "time" on the segment
    /*public Vector3 GetAbsolutePosition(float time)
    {
        Vector3 relativePosition = GetRelativePosition(time);

        // Scale the position
        Vector3 scaledPosition = relativePosition * scale;

        // Rotate the position
        Vector3 rotatedPosition = rotation * scaledPosition;

        // Translate the position by adding it to the origin
        Vector3 absolutePosition = origin + rotatedPosition;

        return absolutePosition;
    }*/

    //returns the direction of a certain "time" on the segment
    /*public Quaternion GetDirection(float time)
    {
        Vector3 previous = GetAbsolutePosition(time - .001f);
        Vector3 current = GetAbsolutePosition(time);
        return Quaternion.FromToRotation(previous, current);
    }*/

    public Quaternion GetRelativeDirection(float time, float offset)
    {
        Vector3 previous = GetRelativePosition(time, offset);
        Vector3 current = GetRelativePosition(time, offset - .001f);
        return Quaternion.FromToRotation(previous, current);
    }

    //generate a mesh for this segment for a given development time
    public virtual Mesh MakeSelfMesh(float time) {
        throw new NotImplementedException("MakeSelfMesh needs to be implemented by the child class");
    }

    //Makes the mesh for an entire L-system for a given development time
    /*public Mesh MakeMesh(float time)
    {
        List<Mesh> meshes = new List<Mesh>();
        foreach(LSystem sub in subSystems)
        {
            if(time >= sub.startTime)
            {
                float subTime = time - sub.startTime;
                Mesh subMesh = sub.MakeMesh(subTime);
                Vector3 origin = GetRelativePosition(sub.startOffset) + sub.localPosition;
                Quaternion parentDirection = GetDirection(sub.startOffset);
                Quaternion rotation = parentDirection * sub.LocalRotation();
                Mesh m = MeshUtils.Adjust(subMesh, origin, rotation, scale * sub.localScale);
                meshes.Add(subMesh);
            }
        }
        Mesh mesh = MakeSelfMesh(Mathf.Min(time, 1));
        meshes.Add(mesh);
        return MeshUtils.Combine(meshes.ToArray());
    }*/

    //TODO: fix a bug with this approach - you have to add them in order right now since child position's position depends on yours already being set
    public void AddSubSystem(LSystem sub)
    {
        subSystems.Add(sub);
        sub.SetParent(this);
    }

    public void SetParent(LSystem parent)
    {
        this.parent = parent;
        gameObject.transform.parent = parent.gameObject.transform;
    }

    public void UpdateMesh(float time)
    {
        float parentTime = Mathf.Min(1,startTime + time * growTime);
        meshFilter.mesh = MakeSelfMesh(Mathf.Min(1,time));
        foreach(LSystem child in subSystems)
        {
            float childTime = (time - child.startTime) / child.growTime;
            if(childTime > 0){
                child.UpdateMesh(childTime); 
            }
        }
        if(parent != null)
        {
            gameObject.transform.position = parent.GetRelativePosition(parentTime, startOffset);
            gameObject.transform.localRotation = parent.GetRelativeDirection(time, startOffset) * Quaternion.Euler(localRotation);
        }    
    }

    public void SetMaterial(Material mat)
    {
        meshRenderer.material = mat;
    }

    //we will call this from the "root" LSystem to initialize all origins, rotations, and scales
    /*public void Init(){
        foreach(LSystem sub in subSystems)
        {
            sub.origin = GetAbsolutePosition(sub.startOffset) + sub.localPosition;
            sub.rotation = GetDirection(sub.startOffset) * sub.LocalRotation();
            sub.scale = scale * sub.localScale;
        }
    }*/
    
    
}

