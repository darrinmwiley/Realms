using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LSystems
{
    public static LSystem GenTest()
    {
        CylinderLSystem trunk = new CylinderLSystem(){
            height = 10,
            baseRadius = 1,
            tipRadius = 1,
            verticalSamples = 10,
            horizontalSamples = 10,
        };

        CylinderLSystem branch = new CylinderLSystem(){
            height = 10,
            baseRadius = 1,
            tipRadius = 1,
            verticalSamples = 10,
            horizontalSamples = 10
        };

        CylinderLSystem flower = new CylinderLSystem(){
            height = 10,
            baseRadius = .25f,
            tipRadius = 10,
            verticalSamples = 10,
            horizontalSamples = 10
        };

        trunk.AddSubSystem(new SubSystem(){
            startTime = .7f,
            startOffset = .5f,
            growTime = 1,
            localRotation = Quaternion.Euler(90,0,90),
            localPosition = new Vector3(0,0,0),
            localScale = .5f,
            lSystem = branch,
        });

        branch.AddSubSystem(new SubSystem(){
            startTime = 1,
            startOffset = 1,
            growTime = 1,
            localRotation = Quaternion.Euler(0,0,0),
            localPosition = new Vector3(0,0,0),
            localScale = .5f,
            lSystem = flower,
        });
        return trunk;
    }
}
