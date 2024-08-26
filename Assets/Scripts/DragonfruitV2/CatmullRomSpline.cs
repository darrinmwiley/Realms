using UnityEngine;
using System.Collections.Generic;

public class CatmullRomSpline : Spline
{
    public CatmullRomSpline(List<Vector3> controlPoints, int pointsBetweenControlPoints) : base(controlPoints, pointsBetweenControlPoints){}

    public override List<Vector3> CreateSpline(List<Vector3> controlPoints, int pointsBetweenControlPoints)
    {
        List<Vector3> splinePoints = new List<Vector3>();

        for (int i = 0; i < controlPoints.Count - 1; i++)
        {
            Vector3 p0 = controlPoints[Mathf.Max(i - 1, 0)];
            Vector3 p1 = controlPoints[i];
            Vector3 p2 = controlPoints[i + 1];
            Vector3 p3 = controlPoints[Mathf.Min(i + 2, controlPoints.Count - 1)];

            for (int j = 0; j < pointsBetweenControlPoints; j++)
            {
                float t = (float)j / pointsBetweenControlPoints;
                Vector3 point = CatmullRom(p0, p1, p2, p3, t);
                splinePoints.Add(point);
            }
        }

        splinePoints.Add(controlPoints[controlPoints.Count - 1]);
        return splinePoints;
    }

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        Vector3 part1 = 2f * p1;
        Vector3 part2 = p2 - p0;
        Vector3 part3 = 2f * p0 - 5f * p1 + 4f * p2 - p3;
        Vector3 part4 = -p0 + 3f * p1 - 3f * p2 + p3;

        return 0.5f * (part1 + t * part2 + t2 * part3 + t3 * part4);
    }
}