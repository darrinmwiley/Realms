using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowJointObj : MonoBehaviour
{
    public GameObject parentObject; // The parent object to follow

    private Vector3 initialRelativePosition;

    public void Cache()
    {
        if (parentObject != null)
        {
            // Cache the initial relative position
            initialRelativePosition = transform.position - parentObject.transform.position;
        }
    }

    void Update()
    {
        if (parentObject != null)
        {
            // Calculate the new relative position based on parent's rotation
            Quaternion rotationDifference = parentObject.transform.rotation * Quaternion.Inverse(transform.rotation);
            Vector3 newRelativePosition = rotationDifference * initialRelativePosition;

            // Set the position according to the new relative position
            transform.position = parentObject.transform.position + newRelativePosition;

            // Set the rotation to match the parent's rotation
            transform.rotation = parentObject.transform.rotation;
        }
    }
}