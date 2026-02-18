using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GrenadeProjectile : MonoBehaviour
{
    [SerializeField] private float fuseSeconds = 3f;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private int damage = 20;
    [SerializeField, Range(0f, 1f)] private float minDamagePercent = 0.2f;
    [SerializeField] private float explosionForce = 12f;
    [SerializeField] private float explosionUpForce = 3f;
    [SerializeField] private LayerMask hitMask = ~0;

    private Unit sourceUnit;
    private string actionName;
    private float spawnTime;
    private bool exploded;

    public void Initialize(Unit source, string actionLabel, float fuse, float radius, int dmg, float force, float upForce, LayerMask mask)
    {
        sourceUnit = source;
        actionName = actionLabel;
        fuseSeconds = fuse;
        explosionRadius = radius;
        damage = dmg;
        explosionForce = force;
        explosionUpForce = upForce;
        hitMask = mask;
    }

    private void OnEnable()
    {
        spawnTime = Time.time;
    }

    private void Update()
    {
        if (exploded)
        {
            return;
        }

        if (Time.time - spawnTime >= fuseSeconds)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (exploded)
        {
            return;
        }

        exploded = true;

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, hitMask, QueryTriggerInteraction.Ignore);
        bool hitAny = false;
        foreach (Collider hit in hits)
        {
            Unit target = hit.GetComponentInParent<Unit>();
            if (target == null || !target.IsAlive)
            {
                continue;
            }

            int appliedDamage = CalculateDamage(hit);
            target.ApplyDamage(appliedDamage, sourceUnit, actionName);
            hitAny = true;

            Rigidbody body = hit.attachedRigidbody ?? target.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.AddExplosionForce(explosionForce, transform.position, explosionRadius, explosionUpForce, ForceMode.VelocityChange);
            }
        }

        if (!hitAny && sourceUnit != null)
        {
            Debug.Log($"{sourceUnit.name} used {actionName} but hit nothing.");
        }

        Destroy(gameObject);
    }

    private int CalculateDamage(Collider hit)
    {
        if (explosionRadius <= 0f)
        {
            return damage;
        }

        Vector3 closest = hit.ClosestPoint(transform.position);
        float distance = Vector3.Distance(transform.position, closest);
        float t = Mathf.Clamp01(distance / explosionRadius);
        float minDamage = damage * minDamagePercent;
        return Mathf.RoundToInt(Mathf.Lerp(damage, minDamage, t));
    }
}
