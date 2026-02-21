using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class BoomerangProjectile : MonoBehaviour
{
    private Unit sourceUnit;
    private string actionName;
    private int hitDamage;
    private int selfDamage;
    private float knockbackForce;
    private float knockbackUpForce;
    private float selfKnockbackForce;
    private LayerMask hitMask;

    private Vector3 orbitCenter;
    private Vector3 orbitRight;
    private Vector3 orbitForward;
    private float orbitRadius;
    private float maxArcHeight;
    private float angularSpeed;
    private float catchRadius;

    private const float StartAngle = -Mathf.PI / 2f;
    private const float TotalAngle = 2f * Mathf.PI;
    private const float ReturnThreshold = Mathf.PI;

    private float currentAngle;
    private bool returning;
    private bool catchWindowOpen;
    private bool finished;

    private InputAction catchAction;
    private bool catchPressed;

    private readonly HashSet<Unit> hitOutward = new HashSet<Unit>();
    private readonly HashSet<Unit> hitReturn = new HashSet<Unit>();

    private ThirdPersonCameraController cameraController;
    private int cameraFollowId = -1;

    public void Initialize(
        Unit source, string action,
        int dmg, int selfDmg,
        float knockback, float knockbackUp, float selfKnock,
        Vector3 center, Vector3 right, Vector3 forward,
        float radius, float arcHeight, float speed,
        float catchDist, InputAction catchInput, LayerMask mask)
    {
        sourceUnit = source;
        actionName = action;
        hitDamage = dmg;
        selfDamage = selfDmg;
        knockbackForce = knockback;
        knockbackUpForce = knockbackUp;
        selfKnockbackForce = selfKnock;
        orbitCenter = center;
        orbitRight = right;
        orbitForward = forward;
        orbitRadius = radius;
        maxArcHeight = arcHeight;
        angularSpeed = speed / radius;
        catchRadius = catchDist;
        hitMask = mask;

        catchAction = catchInput;
        if (catchAction != null)
        {
            catchAction.performed += OnCatchPressed;
        }

        currentAngle = StartAngle;
        transform.position = GetPositionAtAngle(currentAngle);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    public void SetCameraFollow(ThirdPersonCameraController controller, int followId)
    {
        cameraController = controller;
        cameraFollowId = followId;
    }

    private void OnCatchPressed(InputAction.CallbackContext context)
    {
        catchPressed = true;
    }

    private void OnDestroy()
    {
        if (catchAction != null)
        {
            catchAction.performed -= OnCatchPressed;
        }
    }

    private void Update()
    {
        if (finished) return;

        currentAngle += angularSpeed * Time.deltaTime;

        float endAngle = StartAngle + TotalAngle;

        if (!returning && currentAngle >= StartAngle + ReturnThreshold)
        {
            returning = true;
        }

        if (currentAngle >= endAngle)
        {
            if (!catchWindowOpen)
            {
                ApplySelfHit();
            }
            Finish();
            return;
        }

        Vector3 newPos = GetPositionAtAngle(currentAngle);

        if (returning)
        {
            float distToPlayer = Vector3.Distance(newPos, sourceUnit != null ? sourceUnit.transform.position : orbitCenter);
            catchWindowOpen = distToPlayer <= catchRadius;

            if (catchWindowOpen && catchPressed)
            {
                Finish();
                return;
            }
        }

        catchPressed = false;

        transform.position = newPos;

        Vector3 travelDir = GetTravelDirectionAtAngle(currentAngle);
        if (travelDir.sqrMagnitude > 0.001f)
        {
            transform.Rotate(Vector3.up, 720f * Time.deltaTime, Space.World);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (finished) return;

        int layer = 1 << other.gameObject.layer;
        if ((hitMask.value & layer) == 0) return;

        Unit target = other.GetComponentInParent<Unit>();
        if (target == null || !target.IsAlive || target == sourceUnit) return;

        HashSet<Unit> hitSet = returning ? hitReturn : hitOutward;
        if (!hitSet.Add(target)) return;

        target.ApplyDamage(hitDamage, sourceUnit, actionName);

        Rigidbody body = other.attachedRigidbody ?? target.GetComponent<Rigidbody>();
        if (body != null)
        {
            Vector3 travelDir = GetTravelDirectionAtAngle(currentAngle);
            travelDir.y = 0f;
            if (travelDir.sqrMagnitude < 0.001f) travelDir = orbitRight;
            body.AddForce(travelDir.normalized * knockbackForce + Vector3.up * knockbackUpForce, ForceMode.VelocityChange);
        }
    }

    /// <summary>
    /// Samples a point on the circular arc with a sinusoidal vertical rise and fall.
    /// </summary>
    private Vector3 GetPositionAtAngle(float angle)
    {
        Vector3 flat = orbitCenter
            + orbitRight * (orbitRadius * Mathf.Cos(angle))
            + orbitForward * (orbitRadius * Mathf.Sin(angle));

        float t = (angle - StartAngle) / TotalAngle;
        flat.y += maxArcHeight * Mathf.Sin(t * Mathf.PI);

        return flat;
    }

    /// <summary>
    /// Returns the normalised travel direction at a given arc angle (derivative of position w.r.t. angle).
    /// </summary>
    private Vector3 GetTravelDirectionAtAngle(float angle)
    {
        Vector3 tangent = -orbitRight * (orbitRadius * Mathf.Sin(angle))
                        +  orbitForward * (orbitRadius * Mathf.Cos(angle));
        return tangent.normalized;
    }

    private void ApplySelfHit()
    {
        if (sourceUnit == null || !sourceUnit.IsAlive) return;

        sourceUnit.ApplyDamage(selfDamage, sourceUnit, actionName);

        Rigidbody body = sourceUnit.GetComponent<Rigidbody>();
        if (body != null)
        {
            Vector3 approachDir = GetTravelDirectionAtAngle(currentAngle);
            approachDir.y = 0f;
            Vector3 sideways = Vector3.Cross(approachDir.normalized, Vector3.up).normalized;
            body.AddForce(sideways * selfKnockbackForce + Vector3.up * (selfKnockbackForce * 0.4f), ForceMode.VelocityChange);
        }
    }

    private void Finish()
    {
        if (finished) return;
        finished = true;

        if (cameraController != null && cameraFollowId >= 0)
        {
            cameraController.EndTemporaryFollow(cameraFollowId, 1f);
        }

        Destroy(gameObject);
    }
}