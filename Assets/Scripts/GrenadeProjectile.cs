using UnityEngine;

/// <summary>
/// Handles grenade fuse timing and explosion behavior.
/// </summary>
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
    private ThirdPersonCameraController cameraController;
    private int cameraFollowId = -1;

    /// <summary>
    /// Initializes the grenade settings for this instance.
    /// </summary>
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

    /// <summary>
    /// Registers a temporary camera follow so it can be released on explosion.
    /// </summary>
    public void SetCameraFollow(ThirdPersonCameraController controller, int followId)
    {
        cameraController = controller;
        cameraFollowId = followId;
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

        if (cameraController != null && cameraFollowId >= 0)
        {
            cameraController.EndTemporaryFollow(cameraFollowId, 1f);
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, hitMask, QueryTriggerInteraction.Ignore);
        bool hitAny = false;
        foreach (Collider hit in hits)
        {
            Unit target = hit.GetComponentInParent<Unit>();
            if (target == null || !target.IsAlive)
            {
                continue;
            }

            int appliedDamage = CalculateDamage(hit, out float falloff);
            target.ApplyDamage(appliedDamage, sourceUnit, actionName);
            hitAny = true;

            Rigidbody body = hit.attachedRigidbody ?? target.GetComponent<Rigidbody>();
            if (body != null)
            {
                float force = explosionForce * falloff;
                float upForce = explosionUpForce * falloff;
                body.AddExplosionForce(force, transform.position, explosionRadius, upForce, ForceMode.VelocityChange);
            }
        }

        if (!hitAny && sourceUnit != null)
        {
            Debug.Log($"{sourceUnit.name} used {actionName} but hit nothing.");
        }

        Destroy(gameObject);
    }

    private int CalculateDamage(Collider hit, out float falloff)
    {
        falloff = 1f;
        if (explosionRadius <= 0f)
        {
            return damage;
        }

        Vector3 closest = hit.ClosestPoint(transform.position);
        float distance = Vector3.Distance(transform.position, closest);
        float t = Mathf.Clamp01(distance / explosionRadius);
        falloff = Mathf.Lerp(1f, minDamagePercent, t);
        float minDamage = damage * minDamagePercent;
        return Mathf.RoundToInt(Mathf.Lerp(damage, minDamage, t));
    }
}
