using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Plant))]
public class PlantEdior : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Plant plant = (Plant)target;

        EditorGUI.BeginChangeCheck(); // Start checking for changes

        // Create a slider and bind it to the growthProgress variable of the Plant
        float newGrowthProgress = EditorGUILayout.Slider("Growth Progress", plant.GetTime(), 0.0f, 1.0f);

        if (EditorGUI.EndChangeCheck()) // Check if the slider value was changed
        {
            Undo.RecordObject(plant, "Change Growth Progress"); // Record changes for undo
            plant.SetTime(newGrowthProgress); // Call the method to react to the change
            EditorUtility.SetDirty(plant); // Mark the object as dirty to ensure changes are saved
        }
    }
}