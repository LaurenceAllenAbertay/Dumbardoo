using UnityEngine;

/// <summary>
/// Throws a grenade projectile that explodes after a fuse.
/// </summary>
[CreateAssetMenu(menuName = "StickWarfare/Actions/Grenade")]
public class GrenadeAction : UnitAction
{
    [SerializeField] private GrenadeProjectile grenadePrefab;
    [SerializeField] private float minThrowForce = 8f;
    [SerializeField] private float maxThrowForce = 18f;
    [SerializeField] private float chargeSpeed = 1.5f;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 1.2f, 0.6f);

    [Header("Explosion")]
    [SerializeField] private float fuseSeconds = 3f;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private int damage = 20;
    [SerializeField] private float explosionForce = 12f;
    [SerializeField] private float explosionUpForce = 3f;
    [SerializeField] private LayerMask hitMask = ~0;

    private float currentThrowForce = -1f;

    /// <summary>
    /// Gets the minimum throw force for charging.
    /// </summary>
    public float MinThrowForce => minThrowForce;

    /// <summary>
    /// Gets the maximum throw force for charging.
    /// </summary>
    public float MaxThrowForce => maxThrowForce;

    /// <summary>
    /// Gets the charge speed used to ramp throw force.
    /// </summary>
    public float ChargeSpeed => chargeSpeed;

    /// <summary>
    /// Sets the current throw force used on the next grenade.
    /// </summary>
    public void SetThrowForce(float force)
    {
        currentThrowForce = force;
    }

    protected override void Execute(Unit unit, TurnManager turnManager)
    {
        if (grenadePrefab == null)
        {
            Debug.LogWarning($"{unit.name} tried to use {ActionName} but no grenade prefab is assigned.");
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning($"{unit.name} tried to use {ActionName} but no Main Camera was found.");
            return;
        }

        Vector3 spawnPos = unit.transform.TransformPoint(spawnOffset);
        GrenadeProjectile grenade = Object.Instantiate(grenadePrefab, spawnPos, Quaternion.identity);
        grenade.transform.rotation = Random.rotation;
        grenade.Initialize(unit, ActionName, fuseSeconds, explosionRadius, damage, explosionForce, explosionUpForce, hitMask);
        IgnoreThrowerCollision(grenade, unit);

        Rigidbody body = grenade.GetComponent<Rigidbody>();
        ThirdPersonCameraController cameraController = cam.GetComponent<ThirdPersonCameraController>();
        if (cameraController != null)
        {
            int followId = cameraController.BeginGrenadeFollow(grenade.transform, unit.transform, body, cam.transform.forward);
            grenade.SetCameraFollow(cameraController, followId);
        }

        if (body != null)
        {
            Vector3 throwDir = cam.transform.forward.normalized;
            float force = currentThrowForce > 0f ? currentThrowForce : maxThrowForce;
            body.linearVelocity = throwDir * force;
        }

        currentThrowForce = -1f;
    }

    private static void IgnoreThrowerCollision(GrenadeProjectile grenade, Unit unit)
    {
        if (grenade == null || unit == null)
        {
            return;
        }

        Collider[] grenadeColliders = grenade.GetComponentsInChildren<Collider>(true);
        Collider[] unitColliders = unit.GetComponentsInChildren<Collider>(true);
        if (grenadeColliders.Length == 0 || unitColliders.Length == 0)
        {
            return;
        }

        foreach (Collider grenadeCollider in grenadeColliders)
        {
            if (grenadeCollider == null || grenadeCollider.isTrigger)
            {
                continue;
            }

            foreach (Collider unitCollider in unitColliders)
            {
                if (unitCollider == null || unitCollider.isTrigger)
                {
                    continue;
                }

                Physics.IgnoreCollision(grenadeCollider, unitCollider, true);
            }
        }
    }
}
