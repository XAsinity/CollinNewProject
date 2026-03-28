#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector for DayNightVolumeController.
/// Adds a "Reset to Defaults" button so designers can restore the built-in curves.
/// Also adds a menu item to create the controller in the scene pre-wired to DayNightCycle.
/// </summary>
[CustomEditor(typeof(DayNightVolumeController))]
public class DayNightVolumeControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (GUILayout.Button("Reset Curves to Defaults", GUILayout.Height(30)))
        {
            DayNightVolumeController ctrl = (DayNightVolumeController)target;
            Undo.RecordObject(ctrl, "Reset DayNightVolumeController Defaults");
            // Invoke the private helper via reflection so we don't need to make it public
            var method = typeof(DayNightVolumeController)
                .GetMethod("SetDefaultCurves",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(ctrl, null);
            EditorUtility.SetDirty(ctrl);
        }
    }

    // ── Menu item ─────────────────────────────────────────────────────────────

    [MenuItem("GameObject/Environment/Day Night Volume Controller", false, 10)]
    static void CreateDayNightVolumeController(MenuCommand menuCommand)
    {
        // Try to find or create the controller on the DayNightCycle GameObject
        DayNightCycle cycle = FindFirstObjectByType<DayNightCycle>();

        GameObject go;
        if (cycle != null)
        {
            // Add controller directly to the DayNightCycle's GameObject
            go = cycle.gameObject;
        }
        else
        {
            go = new GameObject("DayNightVolumeController");
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        }

        if (go.GetComponent<DayNightVolumeController>() == null)
        {
            DayNightVolumeController ctrl = go.AddComponent<DayNightVolumeController>();
            if (cycle != null)
                ctrl.dayNightCycle = cycle;
        }
        else
        {
            Debug.LogWarning("[DayNightVolumeController] A controller already exists on this GameObject.");
        }

        Undo.RegisterCreatedObjectUndo(go, "Create Day Night Volume Controller");
        Selection.activeObject = go;
    }
}
#endif
