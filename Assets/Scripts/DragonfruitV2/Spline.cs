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
        if(L == points.Count - 1)
            return points[L];
        float lerpPercentage = (targetArcLength - arcLength[L]) / (arcLength[L + 1] - arcLength[L]);
        return Vector3.Lerp(points[L], points[L+1], lerpPercentage);
    }

    public static Spline Direction(Vector3 direction){
        return new CatmullRomSpline(new List<Vector3>{new Vector3(0,0,0), direction}, 1);
    }

}
