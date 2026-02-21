using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "StickWarfare/Actions/Boomerang")]
public class BoomerangAction : UnitAction
{
    [SerializeField] private BoomerangProjectile projectilePrefab;
    [SerializeField] private InputActionReference confirmAction;

    [Header("Orbit")]
    [SerializeField] private float orbitRadius = 6f;
    [SerializeField] private float arcHeight = 2f;
    [SerializeField] private float travelSpeed = 12f;

    [Header("Damage & Knockback")]
    [SerializeField] private int hitDamage = 10;
    [SerializeField] private int selfDamage = 8;
    [SerializeField] private float knockbackForce = 14f;
    [SerializeField] private float knockbackUpForce = 5f;
    [SerializeField] private float selfKnockbackForce = 10f;

    [Header("Catch")]
    [SerializeField] private float catchRadius = 2f;

    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 1.2f, 0f);

    protected override void Execute(Unit unit, TurnManager turnManager)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"{unit.name} tried to use {ActionName} but no projectile prefab is assigned.");
            return;
        }

        Camera cam = Camera.main;
        Vector3 camForward = cam != null ? cam.transform.forward : unit.transform.forward;
        Vector3 camRight = cam != null ? cam.transform.right : unit.transform.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 spawnPos = unit.transform.TransformPoint(spawnOffset);
        Vector3 orbitCenter = spawnPos + camForward * orbitRadius;

        InputAction catchInput = confirmAction != null ? confirmAction.action : null;

        BoomerangProjectile projectile = Object.Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        projectile.Initialize(
            unit, ActionName,
            hitDamage, selfDamage,
            knockbackForce, knockbackUpForce, selfKnockbackForce,
            orbitCenter, camRight, camForward,
            orbitRadius, arcHeight, travelSpeed,
            catchRadius, catchInput, hitMask);

        ThirdPersonCameraController cameraController = Object.FindFirstObjectByType<ThirdPersonCameraController>();
        if (cameraController != null)
        {
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            int followId = cameraController.BeginProjectileFollow(projectile.transform, unit.transform, rb, camRight);
            projectile.SetCameraFollow(cameraController, followId);
        }
    }
}