using UnityEngine;

public class RelativeTarget : MonoBehaviour
{
    private Transform parent;
    private Vector3 localTargetPosition;

    public void Init(GameObject parentObj, Transform targetTransform)
    {
        parent = parentObj.transform;
        // Translate the target's position into the local space of the parent
        localTargetPosition = parent.InverseTransformPoint(targetTransform.position);
    }

    void Update()
    {
        if (parent == null) return;

        // Set the position of the target to match the relative position of the parent
        transform.position = parent.TransformPoint(localTargetPosition);
    }
}
