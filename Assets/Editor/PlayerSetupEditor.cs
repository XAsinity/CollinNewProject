using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Provides two Editor-mode ways to create a first-person player:
///   1. GameObject menu → "Player" → "Create First Person Player"
///   2. A "Generate Player" button shown in the PlayerSetup Inspector
/// Neither requires pressing Play.
/// </summary>
[CustomEditor(typeof(PlayerSetup))]
public class PlayerSetupEditor : Editor
{
    // ── Inspector UI ─────────────────────────────────────────────────────────

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        PlayerSetup setup = (PlayerSetup)target;

        if (GUILayout.Button("Generate Player", GUILayout.Height(35)))
        {
            setup.GeneratePlayer();
            EditorUtility.SetDirty(setup.gameObject);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        EditorGUILayout.HelpBox(
            "Click 'Generate Player' to set up CharacterController, Camera, and " +
            "FirstPersonController on this GameObject.\n\n" +
            "Or use: GameObject → Player → Create First Person Player (to create from scratch).",
            MessageType.Info);
    }

    // ── Menu Item ─────────────────────────────────────────────────────────────

    [MenuItem("GameObject/Player/Create First Person Player", false, 10)]
    static void CreatePlayer()
    {
        // Create parent "Player" GameObject
        GameObject player = new GameObject("Player");
        Undo.RegisterCreatedObjectUndo(player, "Create First Person Player");

        // Add CharacterController
        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0f, 1f, 0f);

        // Create child camera
        GameObject camObj = new GameObject("PlayerCamera");
        Undo.RegisterCreatedObjectUndo(camObj, "Create First Person Player");
        camObj.transform.SetParent(player.transform);
        camObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        camObj.transform.localRotation = Quaternion.identity;

        Camera cam = camObj.AddComponent<Camera>();
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane  = 1000f;
        cam.fieldOfView   = 75f;
        camObj.AddComponent<AudioListener>();

        // Add controller
        player.AddComponent<FirstPersonController>();

        // Position
        player.transform.position = new Vector3(0f, 1f, 0f);

        // Select the new player in the Hierarchy
        Selection.activeGameObject = player;

        // Mark scene dirty so Unity prompts to save
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
}
