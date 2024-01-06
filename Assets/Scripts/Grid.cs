using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class Grid : MonoBehaviour
{
    public static Grid instance;
    public static Dictionary<Vector3Int, GameObject> map = new Dictionary<Vector3Int, GameObject>();
    
    private Transform placedParent;

    public void Awake() {
        GameObject placed = new GameObject("Grid");
        placedParent = placed.transform;
        placedParent.parent = gameObject.transform;
        instance = this;
    }

    public static void Remove(Vector3Int location)
    {
        if(map.ContainsKey(location))
        {
            GameObject obj = map[location];
            map.Remove(location);
            Destroy(obj);
        }
    }

    public static void Clear()
    {
        List<Vector3Int> keys = new List<Vector3Int>(map.Keys);
        for(int i = keys.Count - 1;i>=0;i--)
            Remove(keys[i]);
    }

    public static void Set(Vector3Int location, GameObject obj)
    {
        Set(obj, location.x, location.y, location.z);
    }

    public static void Set(GameObject gameObj, int x, int y, int z)
    {
        Remove(new Vector3Int(x,y,z));
        map[new Vector3Int(x,y,z)] = gameObj;
        gameObj.transform.position = new Vector3Int(x,y,z);
        gameObj.transform.parent = instance.placedParent;
    }

    public static GameObject Get(Vector3Int location)
    {
        return Get(location.x, location.y, location.z);
    }

    public static GameObject Get(int x, int y, int z)
    {
        Vector3Int location = new Vector3Int(x,y,z);
        if(!map.ContainsKey(location))
            return map[location];
        return null;
    }
}