using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(Rigidbody))]
public class UnitMovementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4.5f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1f;
    [SerializeField] private float jumpForwardSpeed = 6f;

    [Header("Grounding")]
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundMask = ~0;

    private Unit unit;
    private Rigidbody body;
    private Vector2 moveInput;
    private bool isJumping;
    private bool wasGrounded;
    private bool loggedMissingRefs;

    public InputActionReference JumpAction => jumpAction;
    public InputActionReference MoveAction => moveAction;
    public Transform CameraTransform => cameraTransform;
    public float GroundCheckRadius => groundCheckRadius;
    public float GroundCheckDistance => groundCheckDistance;
    public LayerMask GroundMask => groundMask;

    private void Awake()
    {
        unit = GetComponent<Unit>();
        body = GetComponent<Rigidbody>();
        body.freezeRotation = true;
        body.useGravity = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void OnEnable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.performed += OnMovePerformed;
            moveAction.action.canceled += OnMoveCanceled;
            moveAction.action.Enable();
        }

        if (jumpAction != null && jumpAction.action != null)
        {
            jumpAction.action.performed += OnJumpPerformed;
            jumpAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.performed -= OnMovePerformed;
            moveAction.action.canceled -= OnMoveCanceled;
            // Do NOT call .Disable() here. InputActionReferences are shared
            // assets used by every unit. Disabling one would globally kill
            // input for every other unit still alive in the scene.
        }

        if (jumpAction != null && jumpAction.action != null)
        {
            jumpAction.action.performed -= OnJumpPerformed;
            // Same reason — do not disable the shared asset.
        }
    }

    private bool IsMyMovementPhase()
    {
        return unit.IsTurnActive
            && turnManager != null
            && turnManager.CurrentUnit == unit
            && turnManager.Phase == TurnManager.TurnPhase.Movement;
    }

    private void FixedUpdate()
    {
        if (!EnsureReferences())
        {
            return;
        }

        bool grounded = CheckGrounded();

        if (!IsMyMovementPhase())
        {
            wasGrounded = grounded;
            return;
        }

        if (isJumping && grounded && !wasGrounded)
        {
            isJumping = false;
            OnLanded();
        }

        UpdateMovement(grounded);

        wasGrounded = grounded;
    }

    private void UpdateMovement(bool grounded)
    {
        bool allowMove = IsMyMovementPhase() && grounded && !isJumping;
        Vector3 velocity = body.linearVelocity;
        Vector3 direction = Vector3.zero;

        if (allowMove)
        {
            direction = GetMoveDirection();
            Vector3 desiredHorizontal = direction * moveSpeed;
            Vector3 currentPosition = body.position;
            Vector3 targetPosition = currentPosition + desiredHorizontal * Time.fixedDeltaTime;
            targetPosition = ClampToMoveRange(targetPosition);
            Vector3 horizontalVelocity = (targetPosition - currentPosition) / Time.fixedDeltaTime;
            velocity.x = horizontalVelocity.x;
            velocity.z = horizontalVelocity.z;
        }
        else if (grounded && !isJumping)
        {
            velocity.x = 0f;
            velocity.z = 0f;
        }

        if (grounded && !isJumping)
        {
            velocity.y = 0f;
        }

        body.linearVelocity = velocity;

        if (allowMove && direction.sqrMagnitude > 0f)
        {
            body.MoveRotation(Quaternion.LookRotation(direction, Vector3.up));
        }
    }

    private Vector3 GetMoveDirection()
    {
        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);
        if (input.sqrMagnitude <= 0f)
        {
            return Vector3.zero;
        }

        Transform cam = cameraTransform != null ? cameraTransform : Camera.main?.transform;
        if (cam == null)
        {
            return input.normalized;
        }

        Vector3 forward = cam.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 right = cam.right;
        right.y = 0f;
        right.Normalize();
        Vector3 direction = forward * input.z + right * input.x;
        return direction.normalized;
    }

    private Vector3 ClampToMoveRange(Vector3 position)
    {
        Vector3 origin = turnManager.CurrentTurnStartPosition;
        Vector3 offset = position - origin;
        offset.y = 0f;
        if (offset.sqrMagnitude <= unit.MoveRange * unit.MoveRange)
        {
            return position;
        }

        Vector3 clamped = origin + offset.normalized * unit.MoveRange;
        clamped.y = position.y;
        return clamped;
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (!IsMyMovementPhase() || isJumping || !CheckGrounded())
        {
            return;
        }

        isJumping = true;
        float jumpVelocity = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * jumpHeight);
        Vector3 jumpDir;
        if (moveInput.sqrMagnitude > 0f)
        {
            jumpDir = GetMoveDirection();
        }
        else
        {
            Transform cam = cameraTransform != null ? cameraTransform : Camera.main?.transform;
            jumpDir = cam != null ? cam.forward : transform.forward;
            jumpDir.y = 0f;
            jumpDir.Normalize();
        }

        Vector3 jumpForward = jumpDir * jumpForwardSpeed;
        body.linearVelocity = new Vector3(jumpForward.x, jumpVelocity, jumpForward.z);
    }

    private void OnLanded()
    {
        Vector3 origin = turnManager.CurrentTurnStartPosition;
        Vector3 offset = body.position - origin;
        offset.y = 0f;
        if (offset.sqrMagnitude > unit.MoveRange * unit.MoveRange)
        {
            turnManager.EndMovementPhase();
        }
    }

    private bool CheckGrounded()
    {
        Vector3 origin = body.position + Vector3.up * 0.05f;
        return Physics.SphereCast(origin, groundCheckRadius, Vector3.down, out _, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }

    private bool EnsureReferences()
    {
        if (turnManager == null || moveAction == null || jumpAction == null)
        {
            if (!loggedMissingRefs)
            {
                Debug.LogWarning($"{name} UnitMovementController missing refs. TurnManager: {(turnManager != null)}, MoveAction: {(moveAction != null)}, JumpAction: {(jumpAction != null)}");
                loggedMissingRefs = true;
            }
            return false;
        }

        return true;
    }
}