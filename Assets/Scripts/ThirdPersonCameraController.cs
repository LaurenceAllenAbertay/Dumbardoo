using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls a third-person camera that can temporarily follow alternate targets.
/// </summary>
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

    [Header("Dynamite Follow")]
    [SerializeField] private float DynamiteFollowDistance = 7f;
    [SerializeField] private Vector3 DynamiteTargetOffset = new Vector3(0f, 2.6f, 0f);
    [SerializeField] private float DynamitePitch = 30f;
    [SerializeField] private float DynamiteVelocityDeadZone = 0.25f;

    [Header("Rotation")]
    [SerializeField] private float lookSensitivity = 120f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private bool lockCursor = true;

    [Header("Intro")]
    [SerializeField] private bool playIntro = true;
    [SerializeField] private float introDuration = 5f;
    [SerializeField] private Vector3 introOffset = new Vector3(0f, 40f, -50f);
    [SerializeField] private float introRotationDegrees = 45f;
    [SerializeField] private bool allowIntroSkip = true;

    private Transform target;
    private Transform overrideReturnTarget;
    private int overrideId;
    private Coroutine overrideRoutine;
    private float yaw;
    private float pitch;
    private Vector2 lookInput;
    private Vector3 followVelocity;
    private float turnTransitionElapsed;
    private Vector3 turnTransitionStartPos;
    private Quaternion turnTransitionStartRot;
    private bool overrideActive;
    private Vector3 overrideTargetOffset;
    private float overrideFollowDistance;
    private float overridePitch;
    private bool overrideUseVelocity;
    private Rigidbody overrideVelocitySource;
    private Vector3 overrideLastDirection;
    private float overrideYawOffset;
    private float overridePitchOffset;
    private bool turnTransitionNotified;
    private bool introPlaying;
    private float introElapsed;
    private Unit pendingIntroUnit;

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
        if (playIntro && introDuration > 0f)
        {
            introPlaying = true;
            introElapsed = 0f;
            if (turnManager != null && turnManager.CurrentUnit != null)
            {
                pendingIntroUnit = turnManager.CurrentUnit;
            }
        }
        else if (turnManager != null && turnManager.CurrentUnit != null)
        {
            SetTarget(turnManager.CurrentUnit.transform, true);
        }

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void LateUpdate()
    {
        if (introPlaying)
        {
            UpdateIntro();
            return;
        }

        if (target == null)
        {
            return;
        }

        float dt = Time.deltaTime;
        bool isTransitioning = turnTransitionElapsed < turnChangeDuration;
        if (!isTransitioning && !overrideActive)
        {
            yaw += lookInput.x * lookSensitivity * dt;
            pitch -= lookInput.y * lookSensitivity * dt;
        }

        float effectivePitch = pitch;
        float effectiveYaw = yaw;
        Vector3 effectiveOffset = targetOffset;
        float effectiveDistance = followDistance;

        if (overrideActive)
        {
            if (!isTransitioning)
            {
                overrideYawOffset += lookInput.x * lookSensitivity * dt;
                overridePitchOffset -= lookInput.y * lookSensitivity * dt;
            }

            Vector3 direction = GetOverrideDirection();
            Vector3 planar = new Vector3(direction.x, 0f, direction.z);
            if (planar.sqrMagnitude > 0.0001f)
            {
                planar.Normalize();
                float baseYaw = Mathf.Atan2(planar.x, planar.z) * Mathf.Rad2Deg;
                effectiveYaw = baseYaw + overrideYawOffset;
            }

            float targetPitch = overridePitch + overridePitchOffset;
            effectivePitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
            effectiveOffset = overrideTargetOffset;
            effectiveDistance = overrideFollowDistance;
        }
        else
        {
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            effectivePitch = pitch;
        }

        Quaternion rotation = Quaternion.Euler(effectivePitch, effectiveYaw, 0f);
        Vector3 pivot = target.position + effectiveOffset;

        float desiredDistance = ResolveCollisionDistance(pivot, rotation, effectiveDistance);
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
        else if (!turnTransitionNotified)
        {
            turnTransitionNotified = true;
            if (turnManager != null)
            {
                turnManager.NotifyTurnTransitionComplete();
            }
        }

        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, followSmoothTime);
        transform.SetPositionAndRotation(smoothedPosition, rotation);
    }

    private void HandleTurnStarted(Unit unit)
    {
        if (unit == null)
        {
            return;
        }

        if (overrideActive)
        {
            overrideReturnTarget = unit.transform;
        }

        if (introPlaying)
        {
            pendingIntroUnit = unit;
            return;
        }

        SetTarget(unit.transform, true);
    }

    /// <summary>
    /// Begins a temporary follow of a target using the default follow settings.
    /// </summary>
    public int BeginTemporaryFollow(Transform newTarget, Transform returnTarget)
    {
        if (newTarget == null)
        {
            return -1;
        }

        ClearOverrideSettings();
        overrideId++;
        overrideReturnTarget = returnTarget;
        SetTarget(newTarget, false);
        return overrideId;
    }

    /// <summary>
    /// Begins a temporary Dynamite follow from above and behind the travel direction.
    /// </summary>
    public int BeginDynamiteFollow(Transform newTarget, Transform returnTarget, Rigidbody velocitySource, Vector3 initialDirection)
    {
        if (newTarget == null)
        {
            return -1;
        }

        overrideActive = true;
        overrideTargetOffset = DynamiteTargetOffset;
        overrideFollowDistance = DynamiteFollowDistance;
        overridePitch = DynamitePitch;
        overrideUseVelocity = true;
        overrideVelocitySource = velocitySource;
        overrideLastDirection = initialDirection.sqrMagnitude > 0.001f ? initialDirection : Vector3.forward;
        overrideYawOffset = 0f;
        overridePitchOffset = 0f;

        overrideId++;
        overrideReturnTarget = returnTarget;
        SetTarget(newTarget, false);
        return overrideId;
    }

    /// <summary>
    /// Ends a temporary follow and returns to the original target after a delay.
    /// </summary>
    public void EndTemporaryFollow(int id, float returnDelaySeconds)
    {
        if (id != overrideId)
        {
            return;
        }

        if (overrideRoutine != null)
        {
            StopCoroutine(overrideRoutine);
        }

        overrideRoutine = StartCoroutine(ReturnToOverrideTarget(id, returnDelaySeconds));
    }

    private System.Collections.IEnumerator ReturnToOverrideTarget(int id, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (id != overrideId)
        {
            yield break;
        }

        Transform returnTarget = overrideReturnTarget;
        overrideReturnTarget = null;

        if (returnTarget != null)
        {
            ClearOverrideSettings();
            SetTarget(returnTarget, true);
        }
        else
        {
            ClearOverrideSettings();
        }

        overrideRoutine = null;
    }

    private void SetTarget(Transform newTarget, bool resetYawPitch)
    {
        target = newTarget;

        if (resetYawPitch && target != null)
        {
            Vector3 forward = target.forward;
            yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            pitch = 10f;
        }

        turnTransitionElapsed = 0f;
        turnTransitionStartPos = transform.position;
        turnTransitionStartRot = transform.rotation;
        turnTransitionNotified = false;
    }

    private void UpdateIntro()
    {
        if (allowIntroSkip && IsIntroSkipPressed())
        {
            EndIntro();
            return;
        }

        introElapsed += Time.deltaTime;
        float duration = Mathf.Max(0.01f, introDuration);
        float t = Mathf.Clamp01(introElapsed / duration);
        float angle = t * introRotationDegrees;
        Vector3 pivot = Vector3.zero;
        Vector3 rotatedOffset = Quaternion.AngleAxis(angle, Vector3.up) * introOffset;
        Vector3 position = pivot + rotatedOffset;
        Quaternion rotation = Quaternion.LookRotation(pivot - position, Vector3.up);
        transform.SetPositionAndRotation(position, rotation);

        if (introElapsed >= introDuration)
        {
            EndIntro();
        }
    }

    private void EndIntro()
    {
        introPlaying = false;
        introElapsed = 0f;
        if (pendingIntroUnit != null)
        {
            SetTarget(pendingIntroUnit.transform, true);
            pendingIntroUnit = null;
        }
    }

    private static bool IsIntroSkipPressed()
    {
        bool keyboardPressed = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        if (keyboardPressed)
        {
            return true;
        }

        if (Mouse.current == null)
        {
            return false;
        }

        return Mouse.current.leftButton.wasPressedThisFrame
            || Mouse.current.rightButton.wasPressedThisFrame
            || Mouse.current.middleButton.wasPressedThisFrame;
    }

    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    private void OnLookCanceled(InputAction.CallbackContext context)
    {
        lookInput = Vector2.zero;
    }

    private float ResolveCollisionDistance(Vector3 pivot, Quaternion rotation, float desiredDistance)
    {
        Vector3 desiredDirection = rotation * Vector3.back;

        if (Physics.SphereCast(pivot, collisionRadius, desiredDirection, out RaycastHit hit, desiredDistance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            desiredDistance = Mathf.Max(0.1f, hit.distance - collisionBuffer);
        }

        return desiredDistance;
    }

    private Vector3 GetOverrideDirection()
    {
        if (overrideUseVelocity && overrideVelocitySource != null)
        {
            Vector3 velocity = overrideVelocitySource.linearVelocity;
            if (velocity.sqrMagnitude >= DynamiteVelocityDeadZone * DynamiteVelocityDeadZone)
            {
                overrideLastDirection = velocity;
            }
        }

        if (overrideLastDirection.sqrMagnitude < 0.0001f)
        {
            overrideLastDirection = target != null ? target.forward : Vector3.forward;
        }

        return overrideLastDirection;
    }

    private void ClearOverrideSettings()
    {
        overrideActive = false;
        overrideUseVelocity = false;
        overrideVelocitySource = null;
        overrideTargetOffset = targetOffset;
        overrideFollowDistance = followDistance;
        overridePitch = 0f;
        overrideLastDirection = Vector3.forward;
        overrideYawOffset = 0f;
        overridePitchOffset = 0f;
    }

    public bool IsTemporaryFollowActive => overrideActive;

    /// <summary>
    /// The world-space offset used as the camera's starting position during
    /// the intro sequence (at rotation angle 0). ShopManager repositions the
    /// camera here when the shop is open.
    /// </summary>
    public Vector3 IntroOffset => introOffset;
}