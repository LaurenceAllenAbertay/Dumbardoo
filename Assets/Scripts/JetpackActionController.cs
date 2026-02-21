using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(Rigidbody))]
public class JetpackActionController : MonoBehaviour
{
    private const int GroundHitCapacity = 1;

    private Unit unit;
    private Rigidbody body;
    private TurnManager turnManager;
    private InputActionReference jumpAction;
    private InputActionReference moveAction;
    private Transform cameraTransform;
    private RaycastHit[] groundHits;

    private float maxFuelSeconds;
    private float fuelBurnPerSecond;
    private float thrustAcceleration;
    private float maxUpSpeed;
    private float airMoveSpeed;
    private float groundCheckRadius;
    private float groundCheckDistance;
    private LayerMask groundMask;

    private float fuelRemaining;
    private bool isActive;
    private bool isThrustHeld;
    private bool tookOff;

    public bool IsThrusting => isActive && isThrustHeld;

    private void Awake()
    {
        unit = GetComponent<Unit>();
        body = GetComponent<Rigidbody>();
        groundHits = new RaycastHit[GroundHitCapacity];
    }

    public void Begin(Unit owningUnit,
        TurnManager owningTurnManager,
        InputActionReference jumpActionRef,
        InputActionReference moveActionRef,
        Transform cameraRef,
        float maxFuel,
        float burnPerSecond,
        float acceleration,
        float maxSpeed,
        float airSpeed,
        float checkRadius,
        float checkDistance,
        LayerMask mask)
    {
        unit = owningUnit != null ? owningUnit : unit;
        turnManager = owningTurnManager;
        BindJump(false);
        jumpAction = jumpActionRef;
        moveAction = moveActionRef;
        cameraTransform = cameraRef;

        maxFuelSeconds = Mathf.Max(0f, maxFuel);
        fuelBurnPerSecond = Mathf.Max(0f, burnPerSecond);
        thrustAcceleration = Mathf.Max(0f, acceleration);
        maxUpSpeed = Mathf.Max(0f, maxSpeed);
        airMoveSpeed = Mathf.Max(0f, airSpeed);
        groundCheckRadius = Mathf.Max(0.01f, checkRadius);
        groundCheckDistance = Mathf.Max(0.01f, checkDistance);
        groundMask = mask;

        fuelRemaining = maxFuelSeconds;
        isActive = true;
        isThrustHeld = false;
        tookOff = false;

        BindJump(true);
    }

    private void OnDisable()
    {
        BindJump(false);
    }

    private void FixedUpdate()
    {
        if (!isActive || unit == null || body == null)
        {
            return;
        }

        if (!unit.IsAlive)
        {
            StopAction(true);
            return;
        }

        if (!IsMyActionPhase())
        {
            StopAction(false);
            return;
        }

        bool grounded = CheckGrounded();

        if (!tookOff && !grounded)
        {
            tookOff = true;
        }

        if (tookOff && grounded)
        {
            StopAction(true);
            return;
        }

        if (!tookOff && grounded && fuelRemaining <= 0f)
        {
            StopAction(true);
            return;
        }

        Vector3 velocity = body.linearVelocity;
        Vector3 moveDirection = Vector3.zero;
        bool canMoveHorizontally = tookOff && !grounded && fuelRemaining > 0f;
        if (canMoveHorizontally)
        {
            moveDirection = GetAirMoveDirection();
            if (moveDirection.sqrMagnitude > 0f)
            {
                Vector3 horizontal = moveDirection * airMoveSpeed;
                velocity.x = horizontal.x;
                velocity.z = horizontal.z;

                if (turnManager != null && turnManager.Phase == TurnManager.TurnPhase.Action)
                {
                    body.MoveRotation(Quaternion.LookRotation(moveDirection, Vector3.up));
                }
            }
        }

        float burnRate = 0f;
        if (isThrustHeld && fuelRemaining > 0f)
        {
            burnRate += fuelBurnPerSecond;
            float targetY = Mathf.Min(maxUpSpeed, velocity.y + thrustAcceleration * Time.fixedDeltaTime);
            velocity.y = targetY;
        }
        else if (grounded && !tookOff)
        {
            velocity.y = 0f;
        }

        if (moveDirection.sqrMagnitude > 0f && fuelRemaining > 0f)
        {
            burnRate += fuelBurnPerSecond;
        }

        if (burnRate > 0f)
        {
            fuelRemaining = Mathf.Max(0f, fuelRemaining - burnRate * Time.fixedDeltaTime);
        }

        if (fuelRemaining <= 0f && grounded && tookOff)
        {
            StopAction(true);
            return;
        }

        body.linearVelocity = velocity;
    }

    private void StopAction(bool notifyTurnManager)
    {
        isActive = false;
        isThrustHeld = false;
        tookOff = false;
        BindJump(false);

        if (notifyTurnManager && turnManager != null && unit != null)
        {
            turnManager.NotifyActionEnded(unit);
        }
    }

    private bool IsMyActionPhase()
    {
        return unit != null
            && unit.IsTurnActive
            && turnManager != null
            && turnManager.CurrentUnit == unit
            && turnManager.Phase == TurnManager.TurnPhase.Action;
    }

    private bool CheckGrounded()
    {
        Vector3 origin = body.position + Vector3.up * 0.05f;
        int hitCount = Physics.SphereCastNonAlloc(origin,
            groundCheckRadius,
            Vector3.down,
            groundHits,
            groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore);
        return hitCount > 0;
    }

    private void BindJump(bool bind)
    {
        if (jumpAction == null || jumpAction.action == null)
        {
            return;
        }

        if (bind)
        {
            jumpAction.action.started += OnJumpStarted;
            jumpAction.action.performed += OnJumpPerformed;
            jumpAction.action.canceled += OnJumpCanceled;
        }
        else
        {
            jumpAction.action.started -= OnJumpStarted;
            jumpAction.action.performed -= OnJumpPerformed;
            jumpAction.action.canceled -= OnJumpCanceled;
        }
    }

    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        isThrustHeld = true;
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        isThrustHeld = true;
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        isThrustHeld = false;
    }

    private Vector3 GetAirMoveDirection()
    {
        if (moveAction == null || moveAction.action == null)
        {
            return Vector3.zero;
        }

        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 planarInput = new Vector3(input.x, 0f, input.y);
        if (planarInput.sqrMagnitude <= 0f)
        {
            return Vector3.zero;
        }

        Transform cam = cameraTransform != null ? cameraTransform : Camera.main?.transform;
        if (cam == null)
        {
            return planarInput.normalized;
        }

        Vector3 forward = cam.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 right = cam.right;
        right.y = 0f;
        right.Normalize();
        Vector3 direction = forward * planarInput.z + right * planarInput.x;
        return direction.normalized;
    }
}