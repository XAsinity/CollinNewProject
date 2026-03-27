using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[AddComponentMenu("Player/First Person Controller")]
public class FirstPersonController : MonoBehaviour
{
    // ─── MOVEMENT ────────────────────────────────────────────────────────────

    [Header("Movement")]
    [Tooltip("Walking speed in units per second.")]
    public float walkSpeed = 5f;

    [Tooltip("Sprint speed in units per second.")]
    public float sprintSpeed = 9f;

    [Tooltip("Jump force applied vertically.")]
    public float jumpForce = 7f;

    [Tooltip("Gravity applied per second squared (should be negative).")]
    public float gravity = -15f;

    [Tooltip("Smoothing time for acceleration / deceleration.")]
    public float moveSmoothTime = 0.1f;

    // ─── MOUSE LOOK ──────────────────────────────────────────────────────────

    [Header("Mouse Look")]
    [Tooltip("Mouse sensitivity multiplier.")]
    public float mouseSensitivity = 2f;

    [Tooltip("Smoothing time for mouse look (0 = no smoothing).")]
    public float lookSmoothTime = 0.05f;

    [Tooltip("Invert the vertical (Y) look axis.")]
    public bool invertY = false;

    // ─── GROUND CHECK ────────────────────────────────────────────────────────

    [Header("Ground Check")]
    [Tooltip("Distance below the character centre used for ground detection.")]
    public float groundCheckDistance = 0.3f;

    [Tooltip("Layer mask for surfaces considered as ground.")]
    public LayerMask groundLayer = ~0; // everything by default

    // ─── HEAD BOB ────────────────────────────────────────────────────────────

    [Header("Head Bob")]
    [Tooltip("Enable camera head-bob while walking.")]
    public bool enableHeadBob = true;

    [Tooltip("Frequency of the head-bob oscillation.")]
    public float bobSpeed = 10f;

    [Tooltip("Vertical displacement amount for head-bob.")]
    public float bobAmount = 0.05f;

    // ─── INPUT ACTIONS ───────────────────────────────────────────────────────

    [Header("Input Actions")]
    [Tooltip("2D axis action for movement (WASD / left stick).")]
    [SerializeField] private InputAction moveAction;

    [Tooltip("2D axis action for look (mouse delta / right stick).")]
    [SerializeField] private InputAction lookAction;

    [Tooltip("Button action for jump (Space).")]
    [SerializeField] private InputAction jumpAction;

    [Tooltip("Button action for sprint (Left Shift).")]
    [SerializeField] private InputAction sprintAction;

    [Tooltip("Button action to unlock the cursor (Escape).")]
    [SerializeField] private InputAction cursorUnlockAction;

    // ─── DEBUG ───────────────────────────────────────────────────────────────

    [Header("Debug")]
    [Tooltip("Draw a gizmo sphere showing the ground-check position.")]
    public bool showGroundCheck = true;

    // ─── CONSTANTS ───────────────────────────────────────────────────────────

    // Small buffer added around the character radius for sphere-cast origin/distance.
    private const float GroundCheckOffset = 0.05f;

    // Scales the raw mouse-delta input (which is in pixels/frame) to degrees/second.
    private const float MouseSensitivityScale = 100f;

    // Small downward velocity kept when grounded so the CharacterController stays
    // pressed against the ground and IsGrounded/SphereCast remain reliable.
    private const float GroundedVerticalVelocity = -2f;

    // ─── PRIVATE STATE ───────────────────────────────────────────────────────

    private CharacterController _controller;
    private Camera _camera;

    // Look
    private float _pitch = 0f;          // vertical camera angle
    private float _yaw = 0f;            // horizontal player angle
    private Vector2 _lookVelocity;      // used for smooth damp
    private Vector2 _currentLook;

    // Movement
    private Vector3 _velocity;          // includes gravity
    private Vector2 _moveVelocity;      // smooth damp ref
    private Vector2 _currentMove;

    // Head bob
    private float _bobTimer = 0f;
    private Vector3 _cameraRestPosition;

    // Ground
    private bool _isGrounded;

    // Cursor lock state
    private bool _cursorLocked = true;

    // ─── LIFECYCLE ───────────────────────────────────────────────────────────

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        _camera = GetComponentInChildren<Camera>();
        if (_camera == null)
            Debug.LogWarning("[FirstPersonController] No Camera found as a child. Please add one.");

        SetupDefaultBindings();
    }

    private void Start()
    {
        if (_camera != null)
            _cameraRestPosition = _camera.transform.localPosition;

        _yaw = transform.eulerAngles.y;

        LockCursor(true);
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        lookAction?.Enable();
        jumpAction?.Enable();
        sprintAction?.Enable();
        cursorUnlockAction?.Enable();

        if (cursorUnlockAction != null)
            cursorUnlockAction.performed += OnCursorUnlock;
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        lookAction?.Disable();
        jumpAction?.Disable();
        sprintAction?.Disable();
        cursorUnlockAction?.Disable();

        if (cursorUnlockAction != null)
            cursorUnlockAction.performed -= OnCursorUnlock;
    }

    private void Update()
    {
        HandleLook();
        HandleMovement();
        HandleHeadBob();
    }

    // ─── INPUT SETUP ─────────────────────────────────────────────────────────

    /// <summary>
    /// Populates any un-configured InputAction fields with sensible default
    /// bindings so the controller works out of the box without touching the
    /// Inspector.
    /// </summary>
    private void SetupDefaultBindings()
    {
        if (NeedsDefaultBinding(moveAction))
        {
            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up",    "<Keyboard>/w")
                .With("Down",  "<Keyboard>/s")
                .With("Left",  "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.expectedControlType = "Vector2";
        }

        if (NeedsDefaultBinding(lookAction))
        {
            lookAction = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
            lookAction.expectedControlType = "Vector2";
        }

        if (NeedsDefaultBinding(jumpAction))
        {
            jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        }

        if (NeedsDefaultBinding(sprintAction))
        {
            sprintAction = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
        }

        if (NeedsDefaultBinding(cursorUnlockAction))
        {
            cursorUnlockAction = new InputAction("CursorUnlock", InputActionType.Button, "<Keyboard>/escape");
        }
    }

    private static bool NeedsDefaultBinding(InputAction action)
    {
        return action == null || action.bindings.Count == 0;
    }

    // ─── CURSOR ──────────────────────────────────────────────────────────────

    private void LockCursor(bool locked)
    {
        _cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible  = !locked;
    }

    private void OnCursorUnlock(InputAction.CallbackContext ctx)
    {
        LockCursor(!_cursorLocked);
    }

    // ─── LOOK ────────────────────────────────────────────────────────────────

    private void HandleLook()
    {
        if (!_cursorLocked || _camera == null) return;

        Vector2 rawLook = lookAction?.ReadValue<Vector2>() ?? Vector2.zero;

        if (invertY)
            rawLook.y = -rawLook.y;

        // Smooth damp the look input
        _currentLook = lookSmoothTime > 0f
            ? Vector2.SmoothDamp(_currentLook, rawLook, ref _lookVelocity, lookSmoothTime)
            : rawLook;

        float scaledDelta = mouseSensitivity * Time.deltaTime * MouseSensitivityScale;

        _yaw   += _currentLook.x * scaledDelta;
        _pitch -= _currentLook.y * scaledDelta;   // subtract = look up when mouse goes up
        _pitch  = Mathf.Clamp(_pitch, -90f, 90f);

        // Rotate the whole player body horizontally
        transform.localEulerAngles = new Vector3(0f, _yaw, 0f);

        // Tilt only the camera vertically
        _camera.transform.localEulerAngles = new Vector3(_pitch, 0f, 0f);
    }

    // ─── MOVEMENT ────────────────────────────────────────────────────────────

    private void HandleMovement()
    {
        // Ground detection via SphereCast from the character's feet.
        // The sphere origin is placed just above the bottom of the capsule to
        // avoid starting inside the ground.
        Vector3 sphereOrigin = transform.position + Vector3.up * (_controller.radius + GroundCheckOffset);
        _isGrounded = Physics.SphereCast(
            sphereOrigin,
            _controller.radius,
            Vector3.down,
            out _,
            groundCheckDistance + _controller.radius + GroundCheckOffset,
            groundLayer,
            QueryTriggerInteraction.Ignore);

        // Reset vertical velocity when freshly grounded.
        // Using a small negative value instead of 0 keeps the CharacterController
        // pressed against the ground so ground detection remains stable.
        if (_isGrounded && _velocity.y < 0f)
            _velocity.y = GroundedVerticalVelocity;

        // Read input
        Vector2 rawMove    = moveAction?.ReadValue<Vector2>()    ?? Vector2.zero;
        bool    isSprinting = (sprintAction?.ReadValue<float>() ?? 0f) > 0.5f;
        bool    jumpPressed = jumpAction?.WasPerformedThisFrame() ?? false;

        // Smooth horizontal movement
        _currentMove = Vector2.SmoothDamp(_currentMove, rawMove, ref _moveVelocity, moveSmoothTime);

        float speed   = isSprinting ? sprintSpeed : walkSpeed;
        Vector3 move  = transform.right   * _currentMove.x
                      + transform.forward * _currentMove.y;
        move *= speed;

        // Jump — uses the kinematic formula v = sqrt(2 * |gravity| * height)
        // where jumpForce represents the desired jump height in units.
        if (jumpPressed && _isGrounded)
            _velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);

        // Gravity
        _velocity.y += gravity * Time.deltaTime;

        // Final move
        _controller.Move((move + new Vector3(0f, _velocity.y, 0f)) * Time.deltaTime);
    }

    // ─── HEAD BOB ────────────────────────────────────────────────────────────

    private void HandleHeadBob()
    {
        if (!enableHeadBob || _camera == null) return;

        bool isMoving = _currentMove.sqrMagnitude > 0.01f && _isGrounded;

        if (isMoving)
        {
            _bobTimer += Time.deltaTime * bobSpeed;
            float bobY = Mathf.Sin(_bobTimer) * bobAmount;
            _camera.transform.localPosition = _cameraRestPosition + new Vector3(0f, bobY, 0f);
        }
        else
        {
            // Smoothly return to rest position
            _bobTimer = 0f;
            _camera.transform.localPosition = Vector3.Lerp(
                _camera.transform.localPosition,
                _cameraRestPosition,
                Time.deltaTime * bobSpeed);
        }
    }

    // ─── GIZMOS ──────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (!showGroundCheck) return;

        CharacterController cc = GetComponent<CharacterController>();
        float radius = cc != null ? cc.radius : 0.4f;
        Vector3 sphereOrigin = transform.position + Vector3.up * (radius + GroundCheckOffset);
        Vector3 groundSpherePos = sphereOrigin + Vector3.down * (groundCheckDistance + radius + GroundCheckOffset);

        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundSpherePos, radius);
    }
}
