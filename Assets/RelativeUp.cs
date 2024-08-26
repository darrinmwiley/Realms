using UnityEngine;

public class RelativeUp : MonoBehaviour
{
    private Transform parent;

    public void Init(GameObject parentObj)
    {
        parent = parentObj.transform;
    }

    void Update()
    {
        if (parent == null) return;

        // Calculate the direction from the parent to the current object's position
        Vector3 direction = (transform.position - parent.position).normalized;

        // Set the up direction to this direction
        transform.up = direction;
    }
}
