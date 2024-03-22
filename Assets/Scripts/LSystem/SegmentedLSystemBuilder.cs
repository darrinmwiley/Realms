using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Base Builder class for LSystem and its subclasses
public class SegmentedLSystemBuilder : SplineLSystemBuilder {
    
    protected int numSegments;

    public static SegmentedLSystem GetDefaultInstance()
    {
        return (SegmentedLSystem)(new SegmentedLSystemBuilder().Build());
    }

    public SegmentedLSystemBuilder SetNumSegments(int numSegments){
        this.numSegments = numSegments;
        return this;
    }

    public override LSystem Build() {
        SegmentedLSystem system = new SegmentedLSystem(
            startTime,
            startOffset,
            growTime,
            localRotation,
            localPosition,
            localScale,
            parent,
            spline,
            thicknessCurve,
            verticalSamples,
            horizontalSamples,
            numSegments
        );
        return system;
    }
}
