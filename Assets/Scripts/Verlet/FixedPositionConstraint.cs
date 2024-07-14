using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixedPositionConstraint
{
    Particle particle;
    Vector3 fixedPosition;

    public FixedPositionConstraint(Particle particle, Vector3 fixedPosition)
    {
        this.particle = particle;
        this.fixedPosition = fixedPosition;
    }

    public void ApplyConstraint()
    {
        particle.position = fixedPosition;
    }
}