using UnityEngine;

[CreateAssetMenu(menuName = "StickWarfare/Actions/TShirtCannon")]
public class TShirtCannonAction : UnitAction
{
    [SerializeField] private TShirtProjectile projectilePrefab;
    [SerializeField] private float launchSpeed = 22f;
    [SerializeField] private int damage = 3;
    [SerializeField] private float knockbackForce = 18f;
    [SerializeField] private float knockbackUpForce = 6f;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 1.2f, 0.6f);
    [SerializeField] private LayerMask hitMask = ~0;

    protected override void Execute(Unit unit, TurnManager turnManager)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning($"{unit.name} tried to use {ActionName} but no Main Camera was found.");
            return;
        }

        if (projectilePrefab == null)
        {
            Debug.LogWarning($"{unit.name} tried to use {ActionName} but no projectile prefab is assigned.");
            return;
        }

        Vector3 spawnPos = unit.transform.TransformPoint(spawnOffset);
        Vector3 launchDir = cam.transform.forward.normalized;

        TShirtProjectile projectile = Object.Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(launchDir));
        projectile.Initialize(unit, ActionName, damage, knockbackForce, knockbackUpForce, hitMask);

        IgnoreShooterCollision(projectile, unit);

        Rigidbody body = projectile.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = launchDir * launchSpeed;
        }

        ThirdPersonCameraController cameraController = Object.FindFirstObjectByType<ThirdPersonCameraController>();
        if (cameraController != null)
        {
            int followId = cameraController.BeginProjectileFollow(projectile.transform, unit.transform, body, launchDir);
            projectile.SetCameraFollow(cameraController, followId);
        }
    }

    private static void IgnoreShooterCollision(TShirtProjectile projectile, Unit unit)
    {
        Collider[] projectileColliders = projectile.GetComponentsInChildren<Collider>(true);
        Collider[] unitColliders = unit.GetComponentsInChildren<Collider>(true);

        foreach (Collider pc in projectileColliders)
        {
            if (pc == null || pc.isTrigger) continue;
            foreach (Collider uc in unitColliders)
            {
                if (uc == null || uc.isTrigger) continue;
                Physics.IgnoreCollision(pc, uc, true);
            }
        }
    }
}