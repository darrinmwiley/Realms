using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TreeLSystem : SplineLSystem
{
    public float minBranchHeight = .5f;
    public float maxBranchHeight = .8f;
    //in degrees
    public float branchRotation = 137.5f;
    public int numBranches = 10;
    //if generation is zero, don't make more sub-branches
    public int generation = 1;

    public void Start()
    {
        float dh = (maxBranchHeight - minBranchHeight) / (numBranches - 1);
        if(generation != 0)
            for(int i = 0;i<numBranches;i++)
            {
                float h = minBranchHeight + i * dh;
                GameObject childGO = new GameObject();
                TreeLSystem childSystem = childGO.AddComponent<TreeLSystem>();
                childSystem.startTime = childSystem.startOffset = h;
                childSystem.generation = generation - 1;
            }
    }
}