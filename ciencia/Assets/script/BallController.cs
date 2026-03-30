using UnityEngine;
using UnityEngine.InputSystem;

namespace script
{
    [RequireComponent(typeof(Rigidbody))]
    public class BallController : MonoBehaviour
    {
    [Header("Movement")]
    [Tooltip("Force multiplier applied from input (higher = faster acceleration)")]
    public float speed = 10f;

    [Header("Debug")]
    [Tooltip("Enable debug logs for input/velocity (will print when input changes)")]
    public bool debugLogs;

    [Tooltip("If true, horizontal velocity will be clamped to maxSpeed")]
    public bool clampMaxSpeed = true;
    [Tooltip("Maximum horizontal speed (meters/second) when clamping is enabled")]
    public float maxSpeed = 6f;

    // Current cached input from the Input System (x = horizontal, y = vertical)
    private Vector2 moveInput = Vector2.zero;
    private Rigidbody rb;
    private Vector2 lastLoggedInput = Vector2.zero;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private bool _subscribedToPlayerInputTriggered;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("BallController requires a Rigidbody component.");
        }
        // Try to find a PlayerInput on the same GameObject so we can auto-bind if
        // the user forgot to hook up the Move event in the inspector.
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null && debugLogs)
        {
            Debug.Log("[BallController] No PlayerInput component found on GameObject. If input events aren't wired, the ball won't move.");
        }
    }

    void OnEnable()
    {
        // If there's a PlayerInput with an action map, try to find a "Move" action and subscribe
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null && playerInput.actions != null)
        {
            // Try common name variations first
            moveAction = playerInput.actions.FindAction("Move") ?? playerInput.actions.FindAction("move");
            if (moveAction != null)
            {
                moveAction.performed += OnMove;
                moveAction.canceled += OnMove;
                if (debugLogs) Debug.Log("[BallController] Subscribed to PlayerInput Move action.");
            }
            else
            {
                // If there's no direct reference, subscribe to onActionTriggered and filter by action name.
                playerInput.onActionTriggered += OnPlayerInputActionTriggered;
                _subscribedToPlayerInputTriggered = true;
                if (debugLogs) Debug.Log("[BallController] Subscribed to PlayerInput.onActionTriggered to watch for Move action.");
            }
        }
    }

    void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.performed -= OnMove;
            moveAction.canceled -= OnMove;
            moveAction = null;
        }

        if (playerInput != null && _subscribedToPlayerInputTriggered)
        {
            playerInput.onActionTriggered -= OnPlayerInputActionTriggered;
            _subscribedToPlayerInputTriggered = false;
        }
    }

    private void OnPlayerInputActionTriggered(InputAction.CallbackContext ctx)
    {
        // Filter for Move action (common name variations)
        if (ctx.action == null)
            return;

        var actionName = ctx.action.name;
        if (actionName == "Move" || actionName == "move")
        {
            OnMove(ctx);
        }
    }

    // Called by the Input System (PlayerInput -> Invoke Unity Events) for the Move action
    // Signature uses CallbackContext so it can be wired directly from the InputAction UnityEvent.
    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (ctx.phase == InputActionPhase.Canceled)
        {
            moveInput = Vector2.zero;
            return;
        }

        // ReadValue<Vector2>() returns the 2D vector from keyboard/gamepad bindings
        moveInput = ctx.ReadValue<Vector2>();
    }

    void FixedUpdate()
    {
        if (rb == null)
            return;

        // Convert 2D input to 3D movement on the XZ plane
        Vector3 force = new Vector3(moveInput.x, 0f, moveInput.y) * speed;

        // Apply force. ForceMode.Force gives more natural, mass-dependent acceleration.
        rb.AddForce(force, ForceMode.Force);

        // Optionally clamp horizontal speed to avoid runaway velocities
        if (clampMaxSpeed && maxSpeed > 0f)
        {
            // Use Rigidbody.linearVelocity (project's Unity version)
            Vector3 horizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float sqrMax = maxSpeed * maxSpeed;
            if (horizontal.sqrMagnitude > sqrMax)
            {
                horizontal = horizontal.normalized * maxSpeed;
                rb.linearVelocity = new Vector3(horizontal.x, rb.linearVelocity.y, horizontal.z);
            }
        }

        if (debugLogs)
        {
            // Only log when input changes to avoid spamming the console
            if (moveInput != lastLoggedInput)
            {
                Debug.Log($"[BallController] Move input: {moveInput} | Velocity: {rb.linearVelocity}");
                lastLoggedInput = moveInput;
            }
        }
    }

    // Optional: expose current input/velocity for debug or other systems
    public Vector2 GetMoveInput() => moveInput;
    public Vector3 GetVelocity() => rb != null ? rb.linearVelocity : Vector3.zero;
    }
}

