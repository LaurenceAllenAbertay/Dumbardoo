using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Unit))]
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
    [SerializeField] private float jumpDuration = 0.5f;
    [SerializeField] private float jumpHeight = 1f;
    [SerializeField] private float jumpForwardDistance = 2f;

    private Unit unit;
    private Vector2 moveInput;
    private bool isJumping;
    private float jumpElapsed;
    private Vector3 jumpStart;
    private Vector3 jumpEnd;

    private void Awake()
    {
        unit = GetComponent<Unit>();
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
            moveAction.action.Disable();
        }

        if (jumpAction != null && jumpAction.action != null)
        {
            jumpAction.action.performed -= OnJumpPerformed;
            jumpAction.action.Disable();
        }
    }

    private void Update()
    {
        if (!IsMyMovementPhase())
        {
            return;
        }

        if (isJumping)
        {
            UpdateJump();
            return;
        }

        UpdateMovement();
    }

    private bool IsMyMovementPhase()
    {
        return unit.IsTurnActive
            && turnManager != null
            && turnManager.CurrentUnit == unit
            && turnManager.Phase == TurnManager.TurnPhase.Movement;
    }

    private void UpdateMovement()
    {
        Vector3 direction = GetMoveDirection();
        if (direction.sqrMagnitude <= 0f)
        {
            return;
        }

        Vector3 delta = direction * (moveSpeed * Time.deltaTime);
        Vector3 nextPosition = transform.position + delta;
        nextPosition = ClampToMoveRange(nextPosition);
        transform.position = nextPosition;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
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
        if (!IsMyMovementPhase() || isJumping)
        {
            return;
        }

        Vector3 forward = transform.forward;
        jumpStart = transform.position;
        jumpEnd = jumpStart + forward * jumpForwardDistance;
        jumpElapsed = 0f;
        isJumping = true;
    }

    private void UpdateJump()
    {
        jumpElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(jumpElapsed / Mathf.Max(0.01f, jumpDuration));
        float height = 4f * t * (1f - t) * jumpHeight;

        Vector3 position = Vector3.Lerp(jumpStart, jumpEnd, t);
        position.y += height;
        transform.position = position;

        if (t >= 1f)
        {
            isJumping = false;
            OnLanded();
        }
    }

    private void OnLanded()
    {
        Vector3 origin = turnManager.CurrentTurnStartPosition;
        Vector3 offset = transform.position - origin;
        offset.y = 0f;
        if (offset.sqrMagnitude > unit.MoveRange * unit.MoveRange)
        {
            turnManager.EndMovementPhase();
        }
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }
}
