using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class LSystem : MonoBehaviour
{
    [Header("Stats")]
    //when does this offshoot begin growing, relative to parent segment grow time
    public float startTime;
    //where does this offshoot begin growing, normalized to parent.
    public float startOffset;
    //how long does it take this segment to be fully grown, not counting children of this segment.
    //Normalized to parent grow time
    public float growTime;
    //local rotation relative to the direction of the parent at startTime
    public Quaternion localRotation;
    //local position relative to the direction of the parent at startTime
    public Vector3 localPosition;
    //local scale relative to parent
    public float localScale;
    //LSystem to handle geometry of the segment
    public bool isRoot;
    
    //origin of segment begin in world space
    private Vector3 origin;
    //segment absolute rotation
    private Quaternion rotation;
    //segment absolute scale
    private float scale = 1;
    private List<LSystem> subSystems = new List<LSystem>();
    //TODO add parent field

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

    //TODO: need to make this a function of offset and time
    //assuming that position and rotation are 0, and scale is 1, get position of a certain "time" on the segment
    public virtual Vector3 GetRelativePosition(float time){
        throw new NotImplementedException("GetRelativePosition needs to be implemented by the child class");
    }

    //returns the absolute position of a certain "time" on the segment
    public Vector3 GetAbsolutePosition(float time)
    {
        Vector3 relativePosition = GetRelativePosition(time);

        // Scale the position
        Vector3 scaledPosition = relativePosition * scale;

        // Rotate the position
        Vector3 rotatedPosition = rotation * scaledPosition;

        // Translate the position by adding it to the origin
        Vector3 absolutePosition = origin + rotatedPosition;

        return absolutePosition;
    }

    //returns the direction of a certain "time" on the segment
    public Quaternion GetDirection(float time)
    {
        Vector3 previous = GetAbsolutePosition(time - .001f);
        Vector3 current = GetAbsolutePosition(time);
        return Quaternion.FromToRotation(previous, current);
    }

    //generate a mesh for this segment for a given development time
    public virtual Mesh MakeSelfMesh(float time) {
        throw new NotImplementedException("MakeSelfMesh needs to be implemented by the child class");
    }

    public List<Mesh> MakeMeshes(float time){
        List<Mesh> meshes = new List<Mesh>();
        foreach(LSystem sub in subSystems)
        {
            if(time >= sub.startTime)
            {
                float subTime = time - sub.startTime;
                Mesh subMesh = sub.MakeMesh(subTime);
                Vector3 origin = GetRelativePosition(sub.startOffset) + sub.localPosition;
                Quaternion parentDirection = GetDirection(sub.startOffset);
                Quaternion rotation = parentDirection * sub.localRotation;
                Mesh m = MeshUtils.Adjust(subMesh, origin, rotation, scale * sub.localScale);
                meshes.Add(subMesh);
            }
        }
        Mesh mesh = MakeSelfMesh(Mathf.Min(time, 1));
        meshes.Add(mesh);
        return meshes;
    }

    //Makes the mesh for an entire L-system for a given development time
    public Mesh MakeMesh(float time)
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
                Quaternion rotation = parentDirection * sub.localRotation;
                Mesh m = MeshUtils.Adjust(subMesh, origin, rotation, scale * sub.localScale);
                meshes.Add(subMesh);
            }
        }
        Mesh mesh = MakeSelfMesh(Mathf.Min(time, 1));
        meshes.Add(mesh);
        return MeshUtils.Combine(meshes.ToArray());
    }

    //TODO: fix a bug with this approach - you have to add them in order right now since child position's position depends on yours already being set
    public void AddSubSystem(LSystem sub)
    {
        subSystems.Add(sub);
    }

    //we will call this from the "root" LSystem to initialize all origins, rotations, and scales
    public void Init(){
        foreach(LSystem sub in subSystems)
        {
            sub.origin = GetAbsolutePosition(sub.startOffset) + sub.localPosition;
            sub.rotation = GetDirection(sub.startOffset) * sub.localRotation;
            sub.scale = scale * sub.localScale;
        }
    }
}