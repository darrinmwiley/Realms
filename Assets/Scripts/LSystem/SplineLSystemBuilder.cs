using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Base Builder class for LSystem and its subclasses
public class SplineLSystemBuilder : LSystemBuilder {

    protected int horizontalSamples = 10;
    protected int verticalSamples = 10;
    protected AnimationCurve thicknessCurve;
    protected Spline spline;

    public SplineLSystemBuilder()
    {
        spline = new CatmullRomSpline(new List<Vector3>(){new Vector3(0,0,0), new Vector3(0,1,0)}, 5);
        thicknessCurve = new AnimationCurve();
        thicknessCurve.AddKey(0, 1);
        thicknessCurve.AddKey(1, 1);
    }

    public SplineLSystemBuilder SetHorizontalSamples(int horizontalSamples){
        this.horizontalSamples = horizontalSamples;
        return this;
    }

    public SplineLSystemBuilder SetVerticalSamples(int verticalSamples){
        this.verticalSamples = verticalSamples;
        return this;
    }

    public SplineLSystemBuilder SetThicknessCurve(AnimationCurve curve){
        this.thicknessCurve = curve;
        return this;
    }

    public SplineLSystemBuilder SetSpline(Spline spline){
        this.spline = spline;
        return this;
    }

    public override LSystem Build() {
        SplineLSystem system = new SplineLSystem(
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
            horizontalSamples
        );
        return system;
    }
}
