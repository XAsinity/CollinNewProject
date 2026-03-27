using UnityEngine;

/// <summary>
/// Attach this to any empty GameObject and it will configure a complete
/// first-person player at runtime (useful for testing).
/// Adds CharacterController, creates a child camera, then adds FirstPersonController.
/// </summary>
[AddComponentMenu("Player/Quick Player Setup")]
public class PlayerSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    [Tooltip("If true, the player is configured automatically when the scene starts.")]
    public bool autoSetupOnAwake = true;

    [Tooltip("Total height of the CharacterController capsule.")]
    public float playerHeight = 2f;

    [Tooltip("Radius of the CharacterController capsule.")]
    public float playerRadius = 0.4f;

    [Tooltip("Local Y position of the camera (eye height).")]
    public float cameraHeight = 1.6f;

    void Awake()
    {
        if (!autoSetupOnAwake) return;

        // ── CharacterController ──────────────────────────────────────────────
        var cc = GetComponent<CharacterController>();
        if (cc == null)
        {
            cc = gameObject.AddComponent<CharacterController>();
            cc.height = playerHeight;
            cc.radius = playerRadius;
            cc.center = new Vector3(0f, playerHeight / 2f, 0f);
        }

        // ── Camera ───────────────────────────────────────────────────────────
        Camera cam = GetComponentInChildren<Camera>();
        if (cam == null)
        {
            GameObject camObj = new GameObject("PlayerCamera");
            camObj.transform.SetParent(transform);
            camObj.transform.localPosition = new Vector3(0f, cameraHeight, 0f);
            camObj.transform.localRotation = Quaternion.identity;

            cam = camObj.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane  = 1000f;
            cam.fieldOfView   = 75f;

            camObj.AddComponent<AudioListener>();
        }

        // ── FirstPersonController ────────────────────────────────────────────
        if (GetComponent<FirstPersonController>() == null)
            gameObject.AddComponent<FirstPersonController>();

        // Place slightly above ground to avoid clipping at origin
        transform.position = new Vector3(0f, 1f, 0f);
    }
}
