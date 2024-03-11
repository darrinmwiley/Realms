using UnityEngine;
using System.Collections.Generic;

public abstract class Spline
{
    public List<Vector3> points;
    public List<float> arcLength = new List<float>();

    public Spline(List<Vector3> controlPoints, int pointsBetweenControlPoints){
        points = CreateSpline(controlPoints, pointsBetweenControlPoints);
        arcLength.Add(0);
        for(int i = 1;i<points.Count;i++)
        {
            float dist = Vector3.Distance(points[i], points[i-1]);
            arcLength.Add(dist + arcLength[i-1]);
        }
    }

    public abstract List<Vector3> CreateSpline(List<Vector3> controlPoints, int pointsBetweenControlPoints);
    
    public Vector3 Evaluate(float time){
        float targetArcLength = arcLength[arcLength.Count - 1] * time;
        int L = -1;
        int R = arcLength.Count;
        int M = (L+R) / 2;
        while(R - L > 1)
        {
            M = (L+R) / 2;
            if(arcLength[M] <= targetArcLength)
            {
                L = M;
            }else{
                R = M;
            }
        }
        if(M == points.Count - 1)
            return points[M];
        float lerpPercentage = (targetArcLength - arcLength[M]) / (arcLength[M + 1] - arcLength[M]);
        return Vector3.Lerp(points[M], points[M+1], lerpPercentage);
    }

}
