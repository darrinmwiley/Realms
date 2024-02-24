using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowerV2 : MonoBehaviour
{
    public GameObject petalPrefab;
    public int numPetals;
    public float height;

    public float startZAngle;
    public float endZAngle;

    public float startScale;
    public float endScale;

    public float phylotaxisRotation = 137.5f;

    public bool regenerateMode;

    //TODO: add start and end radius as well

    // Start is called before the first frame update
    void Start()
    {
        Generate();
    }

    void Update()
    {
        if(regenerateMode)
        {
            DestroyAllChildren();
            Generate();
        }
    }

    public void Generate()
    {
        for(int i = 0;i<numPetals;i++)
        {
            float time = i / (numPetals - 1f);
            GameObject petal = Instantiate(petalPrefab);
            petal.transform.position = new Vector3(0,height * time, 0);
            float angleZ = Mathf.Lerp(startZAngle, endZAngle, time);
            float scale = Mathf.Lerp(startScale, endScale, time);
            petal.transform.rotation = Quaternion.Euler(0,i * phylotaxisRotation * Mathf.Rad2Deg, angleZ);
            petal.transform.localScale = new Vector3(scale, scale, scale);
            petal.transform.parent = gameObject.transform;
        }
    }

    public void DestroyAllChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

}
