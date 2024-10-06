using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BallJoint))]
public class BallJointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BallJoint ballJoint = (BallJoint)target;
        if (GUILayout.Button("Add Growth Joint"))
        {
            ballJoint.AddGrowthJoint();
            EditorUtility.SetDirty(ballJoint);  // Ensure changes are saved
        }
        if (GUILayout.Button("Configure Joint"))
        {
            ballJoint.ConfigureJoint();
            EditorUtility.SetDirty(ballJoint);  // Ensure changes are saved
        }
    }
}