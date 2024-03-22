using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetRelativePositionTester : MonoBehaviour
{
    public LSystemMono lSystemMono; 
    public float time = 1;
    public float offset = 1;

    // Update is called once per frame
    void Update()
    {
        if(lSystemMono != null){
            gameObject.transform.parent = lSystemMono.lSystem.gameObject.transform;
            gameObject.transform.localPosition = lSystemMono.lSystem.GetRelativePosition(time, offset);
        }
            
    }
}
