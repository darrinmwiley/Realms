using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Plant))]
public class PlantEdior : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        Plant plant = (Plant)target;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Start Growth"))
        {
            // Call a method on the LSystem script when the button is clicked
            plant.StartGrowth();
        }
        EditorGUILayout.EndHorizontal();
    }
}