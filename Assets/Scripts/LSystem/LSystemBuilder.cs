using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Base Builder class for LSystem and its subclasses
public class LSystemBuilder {
    protected float startTime = 0;
    //offset is a specific way to show "where" along with the "when" (startTime). 
    //When the need arises, generalize this to a way transform context into <position, rotation>
    protected float startOffset = 0;
    protected float growTime = 1;
    protected Vector3 localRotation = Vector3.zero;
    protected Vector3 localPosition = Vector3.zero;
    protected float localScale = 1; 
    protected LSystem parent = null;

    public static LSystem GetDefaultInstance()
    {
        return new LSystemBuilder().Build();
    }

    public LSystemBuilder SetStartTime(float startTime) {
        this.startTime = startTime;
        return this;
    }

    public LSystemBuilder SetStartOffset(float startOffset) {
        this.startOffset = startOffset;
        return this;
    }

    public LSystemBuilder SetGrowTime(float growTime) {
        this.growTime = growTime;
        return this;
    }

    public LSystemBuilder SetLocalRotation(Vector3 localRotation) {
        this.localRotation = localRotation;
        return this;
    }

    public LSystemBuilder SetLocalPosition(Vector3 localPosition) {
        this.localPosition = localPosition;
        return this;
    }

    public LSystemBuilder SetLocalScale(float localScale) {
        this.localScale = localScale;
        return this;
    }

    public LSystemBuilder SetParent(LSystem parent) {
        this.parent = parent;
        return this;
    }

    public virtual LSystem Build() {
        LSystem system = new LSystem(
            startTime,
            startOffset,
            growTime,
            localRotation,
            localPosition,
            localScale,
            parent
        );
        return system;
    }
}
