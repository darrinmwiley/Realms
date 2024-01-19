using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JointLook : MonoBehaviour
{
    public GameObject lookTarget;
    public ConfigurableJoint configurableJoint;

    void Update()
    {
        if (lookTarget != null && configurableJoint != null)
        {
            float distance = Vector3.Distance(lookTarget.transform.position, configurableJoint.gameObject.transform.position);
            float dz = lookTarget.transform.position.z - configurableJoint.transform.position.z;
            float dx = lookTarget.transform.position.x - configurableJoint.transform.position.x;
            float dy = lookTarget.transform.position.y - configurableJoint.transform.position.y;
            float angleX = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            float angleZ = Mathf.Atan2(dy, dz) * Mathf.Rad2Deg;
            Debug.Log(dx+" "+dy+" "+dz+" "+angleX+" "+angleZ);
            float targetX = 90 - angleX;
            float targetZ = 90 - angleZ;

            // The ConfigurableJoint expects the target rotation in the local space
            // Here, we are assuming the joint's Y-axis is its primary axis
            configurableJoint.targetRotation = Quaternion.Euler(0, -targetZ, -targetX);
        }
    }
}