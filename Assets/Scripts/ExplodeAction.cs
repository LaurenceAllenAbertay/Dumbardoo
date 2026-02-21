using UnityEngine;

/// <summary>
/// The unit sacrifices itself, instantly dying and dealing massive damage
/// and knockback to all units within the explosion radius.
/// </summary>
[CreateAssetMenu(menuName = "StickWarfare/Actions/Explode")]
public class ExplodeAction : UnitAction
{
    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private int damage = 80;
    [SerializeField] private float explosionForce = 20f;
    [SerializeField] private float explosionUpForce = 8f;
    [SerializeField, Range(0f, 1f)] private float minDamagePercent = 0.25f;
    [SerializeField, Range(0f, 1f)] private float minKnockbackPercent = 0.35f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("VFX")]
    [SerializeField] private GameObject explosionVfxPrefab;

    protected override void Execute(Unit unit, TurnManager turnManager)
    {
        if (unit == null)
        {
            return;
        }

        Vector3 origin = unit.transform.position;

        // Spawn optional VFX at the unit's position before it's destroyed.
        if (explosionVfxPrefab != null)
        {
            Object.Instantiate(explosionVfxPrefab, origin, Quaternion.identity);
        }

        // Damage and knock back every unit in range (excluding self).
        Collider[] hits = Physics.OverlapSphere(origin, explosionRadius, hitMask, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            Unit target = hit.GetComponentInParent<Unit>();
            if (target == null || !target.IsAlive || target == unit)
            {
                continue;
            }

            int appliedDamage = CalculateDamage(origin, hit);
            target.ApplyDamage(appliedDamage, unit, ActionName);

            Rigidbody body = hit.attachedRigidbody ?? target.GetComponent<Rigidbody>();
            if (body != null)
            {
                ApplyKnockback(body, origin);
            }
        }

        unit.SpawnAndLaunchDeathVfx(origin, explosionForce);

        // Kill the user last so their name is still valid for damage attribution above.
        unit.ApplyDamage(unit.CurrentHealth, null, ActionName);
    }

    private int CalculateDamage(Vector3 origin, Collider hit)
    {
        if (explosionRadius <= 0f)
        {
            return damage;
        }

        Vector3 closest = hit.ClosestPoint(origin);
        float distance = Vector3.Distance(origin, closest);
        float t = Mathf.Clamp01(distance / explosionRadius);
        float minDmg = damage * minDamagePercent;
        return Mathf.RoundToInt(Mathf.Lerp(damage, minDmg, t));
    }

    private void ApplyKnockback(Rigidbody body, Vector3 origin)
    {
        Vector3 toBody = body.worldCenterOfMass - origin;
        float distance = toBody.magnitude;
        Vector3 direction = distance > 0.0001f ? toBody / distance : Vector3.up;
        float t = explosionRadius > 0.0001f ? Mathf.Clamp01(1f - (distance / explosionRadius)) : 1f;
        float forceFalloff = Mathf.Lerp(minKnockbackPercent, 1f, t);

        Vector3 impulse = direction * (explosionForce * forceFalloff)
                        + Vector3.up * (explosionUpForce * forceFalloff);
        body.AddForce(impulse, ForceMode.VelocityChange);
    }
}