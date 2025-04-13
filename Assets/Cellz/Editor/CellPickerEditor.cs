using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class CellPickerEditor_MouseUp
{
    static CellPickerEditor_MouseUp()
    {
        // Subscribe to SceneView events
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!Application.isPlaying) 
            return;

        Event e = Event.current;

        // 1) Find Display and Field
        Display display = Object.FindObjectOfType<Display>();
        Field field = Object.FindObjectOfType<Field>();
        if (display == null || field == null)
            return;

        // 2) Determine if the mouse is within the Display's sprite bounds
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Bounds spriteBounds = display.GetComponentInChildren<SpriteRenderer>().bounds;
        float denom = ray.direction.z;
        if (Mathf.Abs(denom) < 1e-6f) return;
        float t = (spriteBounds.center.z - ray.origin.z) / denom;
        if (t < 0) return; // behind camera
        Vector3 worldClick = ray.GetPoint(t);

        bool isWithinDisplay = spriteBounds.Contains(worldClick);

        // 3) In the Layout event, disable default picking if within the Display
        //    so Unity won't pick the Display on mouse down or drag.
        if (isWithinDisplay && e.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }

        // 4) If left mouse up, do our picking logic
        if (e.type == EventType.MouseUp && e.button == 0 && e.modifiers == EventModifiers.None)
        {
            if (!isWithinDisplay)
                return; // mouse was released outside the display

            // Convert to pixel coords
            Vector2Int? pixelCoords = SceneMouseToPixelCoords(worldClick, spriteBounds, display);
            if (!pixelCoords.HasValue) return;

            // Convert to world via Field
            Vector2 worldClickPos = field.PixelToWorld(pixelCoords.Value.x, pixelCoords.Value.y);

            // Which cell?
            Cell clickedCell = FindClickedCell(worldClickPos);
            if (clickedCell != null)
            {
                Selection.activeGameObject = clickedCell.gameObject;
                e.Use(); // consume the event
            }
        }
    }

    private static Cell FindClickedCell(Vector2 worldClickPos)
    {
        float bestDistance = float.MaxValue;
        Cell bestCell = null;

        Cell[] allCells = Object.FindObjectsOfType<Cell>();
        foreach (Cell c in allCells)
        {
            float dist = Vector2.Distance(worldClickPos, c.transform.position);
            if (dist < c.outerRadius && dist < bestDistance)
            {
                bestDistance = dist;
                bestCell = c;
            }
        }
        return bestCell;
    }

    private static Vector2Int? SceneMouseToPixelCoords(
        Vector3 worldClick,
        Bounds spriteBounds,
        Display display
    )
    {
        if (!spriteBounds.Contains(worldClick))
            return null;

        Vector3 localPos = worldClick - spriteBounds.min;
        float normalizedX = localPos.x / spriteBounds.size.x;
        float normalizedY = localPos.y / spriteBounds.size.y;

        int texX = Mathf.FloorToInt(normalizedX * display.width);
        int texY = Mathf.FloorToInt(normalizedY * display.height);

        texX = Mathf.Clamp(texX, 0, display.width - 1);
        texY = Mathf.Clamp(texY, 0, display.height - 1);

        return new Vector2Int(texX, texY);
    }
}
