using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DragonfruitSegmentMono))]
public class DragonfruitEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        DragonfruitSegmentMono plant = (DragonfruitSegmentMono)target;

        EditorGUI.BeginChangeCheck();

        // Create a slider and bind it to the growthProgress variable of the Plant
        float newGrowthProgress = EditorGUILayout.Slider("Growth Progress", plant.GetTime(), 0.0f, 1.0f);

        if (EditorGUI.EndChangeCheck())
        {
            plant.SetTime(newGrowthProgress);
        }

        if (GUILayout.Button("Update"))
        {
            plant.lSystem.Update(newGrowthProgress);
        }
    }
}