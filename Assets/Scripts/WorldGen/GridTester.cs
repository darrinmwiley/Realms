using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridTester : MonoBehaviour
{
    public GameObject objectPrefab;
    public GameObject playerPrefab;

    public int perlinAmplitude;
    public float perlinScale;

    void Start()
    {
        for(int i = -200;i<200;i++)
        {
            for(int j = -200;j<200;j++)
            {
                Grid.Set(Instantiate(objectPrefab), i, (int)GetElevation(i+200,j+200), j);
            }
        }
        GameObject player = Instantiate(playerPrefab);
        player.transform.position = new Vector3(0, (int)GetElevation(0,0) + 1, 0);
    }

    float GetElevation(int x, int z)
    {
        float ans = Mathf.PerlinNoise(x * perlinScale,z * perlinScale) * perlinAmplitude;
        return ans;
    }
}
