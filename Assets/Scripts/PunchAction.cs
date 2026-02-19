using UnityEngine;

/// <summary>
/// Performs a short-range punch that applies damage and knockback.
/// </summary>
[CreateAssetMenu(menuName = "StickWarfare/Actions/Punch")]
public class PunchAction : UnitAction
{
    [SerializeField] private float range = 1.5f;
    [SerializeField] private float hitRadius = 0.6f;
    [SerializeField] private int damage = 12;
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackUpForce = 2f;
    [SerializeField] private Vector3 knockbackDirection = Vector3.forward;
    [SerializeField] private LayerMask hitMask = ~0;

    /// <summary>
    /// Gets the punch range.
    /// </summary>
    public float Range => range;

    /// <summary>
    /// Gets the punch hit radius.
    /// </summary>
    public float HitRadius => hitRadius;

    protected override void Execute(Unit unit, TurnManager turnManager)
    {
        Transform cam = Camera.main != null ? Camera.main.transform : null;
        Vector3 forward = cam != null ? cam.forward : unit.transform.forward;
        Vector3 planarForward = new Vector3(forward.x, 0f, forward.z);
        if (planarForward.sqrMagnitude > 0.0001f)
        {
            unit.transform.rotation = Quaternion.LookRotation(planarForward, Vector3.up);
            forward = unit.transform.forward;
        }
        Vector3 origin = unit.transform.position + forward * range;
        Collider[] hits = Physics.OverlapSphere(origin, hitRadius, hitMask, QueryTriggerInteraction.Ignore);
        bool hitAny = false;

        foreach (Collider hit in hits)
        {
            if (hit.attachedRigidbody == null && hit.GetComponent<Unit>() == null)
            {
                continue;
            }

            Unit target = hit.GetComponentInParent<Unit>();
            if (target == null || !target.IsAlive)
            {
                continue;
            }

            target.ApplyDamage(damage, unit, ActionName);
            hitAny = true;

            Rigidbody body = hit.attachedRigidbody ?? target.GetComponent<Rigidbody>();
            if (body != null)
            {
                Vector3 localDir = knockbackDirection.sqrMagnitude > 0f ? knockbackDirection.normalized : Vector3.forward;
                Vector3 worldDir = cam != null ? cam.TransformDirection(localDir) : unit.transform.TransformDirection(localDir);
                Vector3 force = worldDir * knockbackForce + Vector3.up * knockbackUpForce;
                body.AddForce(force, ForceMode.VelocityChange);
            }
        }

        if (!hitAny)
        {
            Debug.Log($"{unit.name} used {ActionName} but hit nothing.");
        }
    }
}
