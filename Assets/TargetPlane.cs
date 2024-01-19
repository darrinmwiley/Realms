using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetPlane : MonoBehaviour
{
    public Transform plane;

    public Vector3 RandomPointOnPlane()
    {
        float x = Random.Range(-5f, 5f);
        float z = Random.Range(-5f, 5f);
        return plane.TransformPoint(new Vector3(x, 0, z));
    }
}
