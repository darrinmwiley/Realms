using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridTester : MonoBehaviour
{
    public GameObject objectPrefab;

    public int perlinAmplitude;
    public int perlinScale;

    void Start()
    {
        for(int i = 0;i<10;i++)
        {
            for(int j = 0;j<10;j++)
            {
                Grid.Set(Instantiate(objectPrefab), i, 0, j);
            }
        }
    }

    float GetElevation(int x, int z)
    {
        return Mathf.PerlinNoise(x,z);
    }
}
