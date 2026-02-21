using UnityEngine;

/// <summary>
/// Throws a Dynamite projectile that explodes after a fuse.
/// </summary>
[CreateAssetMenu(menuName = "StickWarfare/Actions/Dynamite")]
public class DynamiteAction : UnitAction
{
    [SerializeField] private DynamiteProjectile DynamitePrefab;
    [SerializeField] private float minThrowForce = 8f;
    [SerializeField] private float maxThrowForce = 18f;
    [SerializeField] private float chargeSpeed = 1.5f;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 1.2f, 0.6f);
    [SerializeField] private float maxSpinSpeed = 12f;

    [Header("Explosion")]
    [SerializeField] private float fuseSeconds = 3f;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private int damage = 20;
    [SerializeField] private float explosionForce = 12f;
    [SerializeField] private float explosionUpForce = 3f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("VFX")]
    [SerializeField] private GameObject explosionVfxPrefab;

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
    /// Sets the current throw force used on the next Dynamite.
    /// </summary>
    public void SetThrowForce(float force)
    {
        currentThrowForce = force;
    }

    protected override void Execute(Unit unit, TurnManager turnManager)
    {
        if (DynamitePrefab == null)
        {
            Debug.LogWarning($"{unit.name} tried to use {ActionName} but no Dynamite prefab is assigned.");
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning($"{unit.name} tried to use {ActionName} but no Main Camera was found.");
            return;
        }

        Vector3 spawnPos = unit.transform.TransformPoint(spawnOffset);
        DynamiteProjectile Dynamite = Object.Instantiate(DynamitePrefab, spawnPos, Quaternion.identity);
        Dynamite.transform.rotation = Random.rotation;
        Dynamite.Initialize(unit, ActionName, fuseSeconds, explosionRadius, damage, explosionForce, explosionUpForce, hitMask, explosionVfxPrefab);
        IgnoreThrowerCollision(Dynamite, unit);

        Rigidbody body = Dynamite.GetComponent<Rigidbody>();
        ThirdPersonCameraController cameraController = cam.GetComponent<ThirdPersonCameraController>();
        if (cameraController != null)
        {
            int followId = cameraController.BeginProjectileFollow(Dynamite.transform, unit.transform, body, cam.transform.forward);
            Dynamite.SetCameraFollow(cameraController, followId);
        }

        if (body != null)
        {
            Vector3 throwDir = cam.transform.forward.normalized;
            float force = currentThrowForce > 0f ? currentThrowForce : maxThrowForce;
            body.linearVelocity = throwDir * force;
            body.angularVelocity = Random.insideUnitSphere * maxSpinSpeed;
        }

        currentThrowForce = -1f;
    }

    private static void IgnoreThrowerCollision(DynamiteProjectile Dynamite, Unit unit)
    {
        if (Dynamite == null || unit == null)
        {
            return;
        }

        Collider[] DynamiteColliders = Dynamite.GetComponentsInChildren<Collider>(true);
        Collider[] unitColliders = unit.GetComponentsInChildren<Collider>(true);
        if (DynamiteColliders.Length == 0 || unitColliders.Length == 0)
        {
            return;
        }

        foreach (Collider DynamiteCollider in DynamiteColliders)
        {
            if (DynamiteCollider == null || DynamiteCollider.isTrigger)
            {
                continue;
            }

            foreach (Collider unitCollider in unitColliders)
            {
                if (unitCollider == null || unitCollider.isTrigger)
                {
                    continue;
                }

                Physics.IgnoreCollision(DynamiteCollider, unitCollider, true);
            }
        }
    }
}