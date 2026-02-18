using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private InputActionReference lookAction;

    [Header("Follow")]
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] private float followDistance = 4.5f;
    [SerializeField] private float followSmoothTime = 0.08f;
    [SerializeField] private float turnChangeDuration = 1f;

    [Header("Rotation")]
    [SerializeField] private float lookSensitivity = 120f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private bool lockCursor = true;

    private Transform target;
    private float yaw;
    private float pitch;
    private Vector2 lookInput;
    private Vector3 followVelocity;
    private float turnTransitionElapsed;
    private Vector3 turnTransitionStartPos;
    private Quaternion turnTransitionStartRot;

    [Header("Collision")]
    [SerializeField] private float collisionRadius = 0.25f;
    [SerializeField] private float collisionBuffer = 0.1f;
    [SerializeField] private LayerMask collisionMask = ~0;

    private void OnEnable()
    {
        if (turnManager != null)
        {
            turnManager.TurnStarted += HandleTurnStarted;
        }

        if (lookAction != null && lookAction.action != null)
        {
            lookAction.action.performed += OnLookPerformed;
            lookAction.action.canceled += OnLookCanceled;
            lookAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (turnManager != null)
        {
            turnManager.TurnStarted -= HandleTurnStarted;
        }

        if (lookAction != null && lookAction.action != null)
        {
            lookAction.action.performed -= OnLookPerformed;
            lookAction.action.canceled -= OnLookCanceled;
            lookAction.action.Disable();
        }
    }

    private void Start()
    {
        if (turnManager != null && turnManager.CurrentUnit != null)
        {
            SetTarget(turnManager.CurrentUnit.transform);
        }

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        float dt = Time.deltaTime;
        bool isTransitioning = turnTransitionElapsed < turnChangeDuration;
        if (!isTransitioning)
        {
            yaw += lookInput.x * lookSensitivity * dt;
            pitch -= lookInput.y * lookSensitivity * dt;
        }
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + targetOffset;

        float desiredDistance = ResolveCollisionDistance(pivot, rotation);
        Vector3 desiredPosition = pivot + rotation * new Vector3(0f, 0f, -desiredDistance);

        if (isTransitioning)
        {
            turnTransitionElapsed += dt;
            float t = Mathf.Clamp01(turnTransitionElapsed / turnChangeDuration);
            Vector3 position = Vector3.Lerp(turnTransitionStartPos, desiredPosition, t);
            Quaternion rot = Quaternion.Slerp(turnTransitionStartRot, rotation, t);
            transform.SetPositionAndRotation(position, rot);
            return;
        }

        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, followSmoothTime);
        transform.SetPositionAndRotation(smoothedPosition, rotation);
    }

    private void HandleTurnStarted(Unit unit)
    {
        if (unit != null)
        {
            SetTarget(unit.transform);
        }
    }

    private void SetTarget(Transform newTarget)
    {
        target = newTarget;

        Vector3 forward = target.forward;
        yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        pitch = 10f;

        turnTransitionElapsed = 0f;
        turnTransitionStartPos = transform.position;
        turnTransitionStartRot = transform.rotation;
    }

    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    private void OnLookCanceled(InputAction.CallbackContext context)
    {
        lookInput = Vector2.zero;
    }

    private float ResolveCollisionDistance(Vector3 pivot, Quaternion rotation)
    {
        Vector3 desiredDirection = rotation * Vector3.back;
        float desiredDistance = followDistance;

        if (Physics.SphereCast(pivot, collisionRadius, desiredDirection, out RaycastHit hit, followDistance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            desiredDistance = Mathf.Max(0.1f, hit.distance - collisionBuffer);
        }

        return desiredDistance;
    }
}
