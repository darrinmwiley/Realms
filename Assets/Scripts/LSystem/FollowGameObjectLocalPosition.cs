using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowGameObjectLocalPosition : MonoBehaviour
{
    public GameObject toFollow;
    public Vector3 localPosition;

    //TODO think about rotation

    void Update()
    {
        transform.position = toFollow.transform.TransformPoint(localPosition);
        //transform.up = toFollow.transform.up;
    }
}