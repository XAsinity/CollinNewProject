using UnityEngine;

/// <summary>
/// Attach this to any empty GameObject and use the "Generate Player" context menu
/// or Inspector button to set up a complete first-person player in Edit mode —
/// no Play button required.
/// </summary>
[AddComponentMenu("Player/Quick Player Setup")]
public class PlayerSetup : MonoBehaviour
{
    [Header("Player Setup")]
    [Tooltip("Total height of the CharacterController capsule.")]
    public float playerHeight = 2f;

    [Tooltip("Radius of the CharacterController capsule.")]
    public float playerRadius = 0.4f;

    [Tooltip("Local Y position of the camera (eye height).")]
    public float cameraHeight = 1.6f;

    /// <summary>
    /// Configures CharacterController, child Camera, AudioListener, and
    /// FirstPersonController on this GameObject.  Works in Edit mode via the
    /// Inspector context menu or the PlayerSetupEditor button.
    /// </summary>
    [ContextMenu("Generate Player")]
    public void GeneratePlayer()
    {
        // ── CharacterController ──────────────────────────────────────────────
        var cc = GetComponent<CharacterController>();
        if (cc == null)
        {
            // Use Undo.AddComponent in the Editor so the operation is undoable;
            // the plain AddComponent path is a safety fallback for runtime calls.
#if UNITY_EDITOR
            cc = UnityEditor.Undo.AddComponent<CharacterController>(gameObject);
#else
            cc = gameObject.AddComponent<CharacterController>();
#endif
        }
        cc.height = playerHeight;
        cc.radius = playerRadius;
        cc.center = new Vector3(0f, playerHeight / 2f, 0f);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameObject);
#endif

        // ── Camera ───────────────────────────────────────────────────────────
        Camera cam = GetComponentInChildren<Camera>();
        if (cam == null)
        {
            GameObject camObj = new GameObject("PlayerCamera");

#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(camObj, "Create PlayerCamera");
#endif

            camObj.transform.SetParent(transform);
            camObj.transform.localPosition = new Vector3(0f, cameraHeight, 0f);
            camObj.transform.localRotation = Quaternion.identity;

            cam = camObj.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane  = 1000f;
            cam.fieldOfView   = 75f;

            camObj.AddComponent<AudioListener>();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(camObj);
#endif
        }

        // ── FirstPersonController ────────────────────────────────────────────
        if (GetComponent<FirstPersonController>() == null)
        {
            // Use Undo.AddComponent in the Editor so the operation is undoable;
            // the plain AddComponent path is a safety fallback for runtime calls.
#if UNITY_EDITOR
            UnityEditor.Undo.AddComponent<FirstPersonController>(gameObject);
#else
            gameObject.AddComponent<FirstPersonController>();
#endif
        }

        // Place slightly above ground to avoid clipping at origin
        transform.position = new Vector3(0f, 1f, 0f);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
    }
}
