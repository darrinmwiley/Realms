using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowSegmentLocalPosition : MonoBehaviour
{
    public Segment2 segment;
    public Vector3 localPosition;

    void Update()
    {
        transform.position = segment.transform.TransformPoint(localPosition);
    }
}