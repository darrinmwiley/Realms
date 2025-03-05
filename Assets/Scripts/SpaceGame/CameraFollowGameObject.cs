using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowGameObject : MonoBehaviour
{
    public GameObject target;         // The GameObject for the camera to follow
    public Vector3 offset = new Vector3(0, 0, -10);  // Offset of the camera from the target

    void Update()
    {
        if (target != null)
        {
            // Calculate the new position with the offset
            Vector3 targetPosition = target.transform.position + offset;

            // Smoothly move the camera towards the target position
            transform.position = targetPosition;
        }
    }
}
