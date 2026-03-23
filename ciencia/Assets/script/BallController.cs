using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Force multiplier applied from input (higher = faster acceleration)")]
    public float speed = 10f;

    [Tooltip("If true, horizontal velocity will be clamped to maxSpeed")]
    public bool clampMaxSpeed = true;
    [Tooltip("Maximum horizontal speed (meters/second) when clamping is enabled")]
    public float maxSpeed = 6f;

    // Current cached input from the Input System (x = horizontal, y = vertical)
    private Vector2 moveInput = Vector2.zero;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("BallController requires a Rigidbody component.");
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
            Vector3 horizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float sqrMax = maxSpeed * maxSpeed;
            if (horizontal.sqrMagnitude > sqrMax)
            {
                horizontal = horizontal.normalized * maxSpeed;
                rb.linearVelocity = new Vector3(horizontal.x, rb.linearVelocity.y, horizontal.z);
            }
        }
    }

    // Optional: expose current input/velocity for debug or other systems
    public Vector2 GetMoveInput() => moveInput;
    public Vector3 GetVelocity() => rb != null ? rb.linearVelocity : Vector3.zero;
}

