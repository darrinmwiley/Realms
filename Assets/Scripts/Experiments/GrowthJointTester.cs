using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GrowthJointTester : MonoBehaviour
{
    public GameObject anchor; // The anchor to attach the GrowthJoint to
    public Vector3 direction = Vector3.up;
    public bool parentSpace = true;
    public float flexibility = 45f;
    public float strength = 1000f;
    public float magnitude = 2f;
    public float growTime = 3f;

    [Range(0, 1)]
    public float growthPercentage = 0f; // Slider value for setting the growth percentage

    private Growth growthJoint;

    // Method to create and attach a new GrowthJoint2
    public void CreateGrowthJoint()
    {
        if (anchor != null)
        {
            // Create a new GrowthJoint2 and configure it
            growthJoint = new Growth(anchor, direction, parentSpace, flexibility, strength, magnitude, growTime);
        }
        else
        {
            Debug.LogError("Anchor is not assigned. Please assign an anchor GameObject.");
        }
    }

    void Update()
    {
        if (growthJoint != null)
        {
            // Update the growth of the joint based on the slider value
            growthJoint.SetGrowth(growthPercentage);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(GrowthJointTester))]
    public class GrowthJointTesterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GrowthJointTester tester = (GrowthJointTester)target;
            if (GUILayout.Button("Create Growth Joint"))
            {
                tester.CreateGrowthJoint();
            }

            if (tester.growthJoint != null)
            {
                // Display the growth percentage slider
                tester.growthPercentage = EditorGUILayout.Slider("Growth Percentage", tester.growthPercentage, 0f, 1f);
            }
        }
    }
#endif
}
