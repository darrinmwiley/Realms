using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SegmentedLSystem : SplineLSystem
{
    public int numSegments;

    private List<GameObject> segments;

    //first, just have them fixed
    //second, add fixed joints and only have the bottom be fixed
    //third, make the joints rotating instead and have a "stiffness" parameter that changes freedom and drive

    //might want to employ the builder pattern for all LSystems so we can take advantage of a constructor.

    //want to add more segments as we go

    //the general idea is: 

    //add a very small segment with the correct orientation
    //grow it until it reaches the target
    //add a new very small segment at the end of the previous
    //repeat

    public SegmentedLSystem(
        float startTime, 
        float startOffset, 
        float growTime, 
        Vector3 localRotation, 
        Vector3 localPosition, 
        float scale, 
        LSystem parent,
        Spline spline,
        AnimationCurve thicknessCurve,
        int verticalSamples,
        int horizontalSamples,
        int numSegments)
        : base(startTime, startOffset, growTime, localRotation, localPosition, scale, parent, spline, thicknessCurve, verticalSamples, horizontalSamples){
        this.numSegments = numSegments;
        segments = new List<GameObject>();
        gameObject.name = "Segmented L-System";
        for(int i = 0;i<numSegments;i++)
        {
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            segments.Add(segment);
            segment.transform.parent = gameObject.transform;
        }
        UpdateSkeleton(0);
    }

    public void UpdateSkeleton(float time)
    {
        float timePerSegment = 1f / numSegments;
        int completeSegments = (int)(time / timePerSegment);
        float remainder = time % timePerSegment;
        for(int i = 0;i<completeSegments;i++)
        {
            UpdateSegment(i,1);
        }
        UpdateSegment(completeSegments, remainder / timePerSegment);
        for(int i = completeSegments + 1;i<numSegments;i++)
        {
            UpdateSegment(i,0);
        }
    }

    public void UpdateSegment(int segment, float time)
    {
        if(segment >= numSegments)
            return;
        float timePerSegment = 1f / numSegments;
        if(time == 0)
        {
            segments[segment].SetActive(false);
        }else
            segments[segment].SetActive(true);
        Vector3 startPosition = spline.Evaluate(segment * timePerSegment);
        Vector3 targetPosition = spline.Evaluate((segment + 1) * (timePerSegment));
        //rotate by absolute rotation around spline.Evaluate(0), then shift by absolute position
        Vector3 endPosition = Vector3.Lerp(startPosition, targetPosition, time);
        //alter the cylinder primitive position and scale such that it starts and ends at these positions

        Vector3 midPoint = (startPosition + endPosition) / 2f;

        // Calculate the distance between the start and end as the scale for our capsule
        float length = Vector3.Distance(startPosition, endPosition);

        // Set the capsule's position to the midpoint
        segments[segment].transform.localPosition = midPoint;

        // Assuming the capsule's initial scale is set properly for a unit length,
        // Scale the capsule's Y-axis by the length. Adjust X and Z for the capsule's thickness as needed.
        segments[segment].transform.localScale = new Vector3(1f, length / 2f, 1f); // Unity's capsule height scales from the center, so we divide by 2

        // Rotate the capsule to align with the start and end positions
        // Direction from start to end
        Vector3 direction = endPosition - startPosition;
        segments[segment].transform.up = direction.normalized;
    }

    public override Mesh MakeSelfMesh(float time)
    {
        UpdateSkeleton(time);
        List<Line> lines = new List<Line>();
        for(int i = 0;i<verticalSamples;i++)
        {
            float verticalTime = i / (verticalSamples - 1f);
            Vector3 center = GetRelativePosition(time, verticalTime);
            float radius = thicknessCurve.Evaluate(time * verticalTime);
            List<Vector3> points = new List<Vector3>();
            for(int j = 0;j<horizontalSamples;j++)
            {
                float horizontalTime = j / (horizontalSamples - 1f);
                float theta = Mathf.PI * 2 * horizontalTime;
                float x = Mathf.Sin(theta) * radius;
                float z = Mathf.Cos(theta) * radius;
                points.Add(center + new Vector3(x,0,z));
            }
            lines.Add(new Line(){points = points});
        }
        Mesh mesh = new Face(){lines = lines, pointsPerLine = horizontalSamples}.MakeMesh();
        MeshUtils.Flip(mesh);
        return mesh;
    }
}