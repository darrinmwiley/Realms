using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplineLSystemBuilder : MonoBehaviour
{
    public AnimationCurve x;
    public AnimationCurve y;
    public AnimationCurve z;
    public AnimationCurve thickness; 

    public float height;
    public float width;
    public float horizontalVariance;

    SplineLSystem lSystem;

    public Plant plant;

    public bool update;

    // Update is called once per frame
    void Update()
    {
        if(update)
        {
            lSystem = new SplineLSystem(){
                height = height,
                width = width,
                verticalSamples = 10,
                horizontalSamples = 10,
                x = x,
                y = y,
                z = z,
                thickness = thickness,
                horizontalVariance = horizontalVariance
            };
            plant.lSystem = lSystem; 
        }
        
    }
}
