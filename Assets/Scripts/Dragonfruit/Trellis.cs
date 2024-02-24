using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trellis : MonoBehaviour
{
    //this is where the first segment will be planted
    public GameObject plot;
    public Material green;

    // Start is called before the first frame update
    void Start()
    {
        GameObject dragonfruitObj = new GameObject("dragonfruit");
        dragonfruitObj.transform.parent = plot.transform;
        Dragonfruit dragonfruit = dragonfruitObj.AddComponent<Dragonfruit>();
        dragonfruit.green = green;
        dragonfruit.SetBase(plot);
        dragonfruit.AddSeed();
    }
}
