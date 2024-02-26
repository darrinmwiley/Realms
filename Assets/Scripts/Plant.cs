using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plant : MonoBehaviour
{
    [SerializeField]
    public LSystem lSystem;
    public Material mat;
    public float growTime;

    private float totalGrowTime;
    private float growStartTime;
    public bool growing;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    private float time;

    public List<GameObject> components;

    void Start()
    {
        if(meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
        if(meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        if(mat != null)
            meshRenderer.sharedMaterial = mat;
        lSystem = LSystems.GenTest();
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
        meshFilter.mesh = lSystem.MakeMesh(time * lSystem.GetTotalTime());
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
