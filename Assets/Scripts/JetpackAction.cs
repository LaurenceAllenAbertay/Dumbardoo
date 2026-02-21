using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Activates a jetpack that lets the unit rise while holding the jump input.
/// </summary>
[CreateAssetMenu(menuName = "StickWarfare/Actions/Jetpack")]
public class JetpackAction : UnitAction
{
    [Header("Fuel")]
    [SerializeField] private float maxFuelSeconds = 3f;
    [SerializeField] private float fuelBurnPerSecond = 1f;
    [SerializeField] private float forwardBurnMultiplier = 1.5f;

    [Header("Thrust")]
    [SerializeField] private float thrustAcceleration = 16f;
    [SerializeField] private float maxUpSpeed = 6f;
    [SerializeField] private float airMoveSpeed = 3.5f;

    [Header("Grounding")]
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundMask = ~0;

    public override bool EndsActionImmediately => false;

    public override bool GetIsActive(Unit unit)
    {
        if (unit == null) return false;
        JetpackActionController controller = unit.GetComponent<JetpackActionController>();
        return controller != null && controller.IsThrusting;
    }

    protected override void Execute(Unit unit, TurnManager turnManager)
    {
        if (unit == null || turnManager == null)
        {
            return;
        }

        UnitMovementController movement = unit.GetComponent<UnitMovementController>();
        InputActionReference jumpAction = movement != null ? movement.JumpAction : null;
        InputActionReference moveAction = movement != null ? movement.MoveAction : null;
        Transform cameraTransform = movement != null ? movement.CameraTransform : null;
        float checkRadius = movement != null ? movement.GroundCheckRadius : groundCheckRadius;
        float checkDistance = movement != null ? movement.GroundCheckDistance : groundCheckDistance;
        LayerMask mask = movement != null ? movement.GroundMask : groundMask;
        if (jumpAction == null)
        {
            Debug.LogWarning($"{unit.name} tried to use {ActionName} but no jump action was found.");
            turnManager.NotifyActionEnded(unit);
            return;
        }

        JetpackActionController controller = unit.GetComponent<JetpackActionController>();
        if (controller == null)
        {
            controller = unit.gameObject.AddComponent<JetpackActionController>();
        }

        controller.Begin(unit,
            turnManager,
            jumpAction,
            moveAction,
            cameraTransform,
            maxFuelSeconds,
            fuelBurnPerSecond,
            forwardBurnMultiplier,
            thrustAcceleration,
            maxUpSpeed,
            airMoveSpeed,
            checkRadius,
            checkDistance,
            mask);
    }
}