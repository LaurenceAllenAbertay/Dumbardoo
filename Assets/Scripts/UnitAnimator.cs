using UnityEngine;

/// <summary>
/// Drives the unit's Animator based on movement state and action input.
///
///  ActionIndex: identifies which action is selected / fired:
///                           0  None
///                           1  Punch
///                           2  Gun
///                           3  Dynamite
///                           4  Jetpack
///                           5  Explode
///                           6  T-Shirt Cannon
///
/// </summary>
[RequireComponent(typeof(Unit))]
public class UnitAnimator : MonoBehaviour
{
    // Action index constants
    public const int ActionNone        = 0;
    public const int ActionPunch       = 1;
    public const int ActionGun         = 2;
    public const int ActionDynamite    = 3;
    public const int ActionJetpack     = 4;
    public const int ActionExplode     = 5;
    public const int ActionTShirtCannon = 6;

    // Animator parameter hashes
    private static readonly int IsWalkingHash    = Animator.StringToHash("IsWalking");
    private static readonly int IsFallingHash    = Animator.StringToHash("IsFalling");
    private static readonly int IsJumpingHash    = Animator.StringToHash("IsJumping");
    private static readonly int IsAimingHash     = Animator.StringToHash("IsAiming");
    private static readonly int IsChargingHash   = Animator.StringToHash("IsCharging");
    private static readonly int IsActionActiveHash = Animator.StringToHash("IsActionActive");
    private static readonly int ActionIndexHash  = Animator.StringToHash("ActionIndex");
    private static readonly int JumpHash         = Animator.StringToHash("Jump");
    private static readonly int FireHash         = Animator.StringToHash("Fire");

    // Inspector
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private UnitMovementController movementController;
    [SerializeField] private UnitActionController actionController;
    
    private Unit unit;
    private UnitAction lastFiredAction;

    // Unity lifecycle
    private void Awake()
    {
        unit = GetComponent<Unit>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (movementController == null)
        {
            movementController = GetComponent<UnitMovementController>();
        }

        if (actionController == null)
        {
            actionController = GetComponent<UnitActionController>();
        }

        if (animator == null)
        {
            Debug.LogWarning($"{name} UnitAnimator: No Animator found. " +
                             "Assign one in the inspector or add it to a child GameObject.");
        }
    }

    private void OnEnable()
    {
        if (actionController != null)
        {
            actionController.ActionSelectionChanged += OnActionSelectionChanged;
            actionController.ActionFired            += OnActionFired;
        }

        if (movementController != null)
        {
            movementController.JumpStarted += OnJumpStarted;
        }
    }

    private void OnDisable()
    {
        if (actionController != null)
        {
            actionController.ActionSelectionChanged -= OnActionSelectionChanged;
            actionController.ActionFired            -= OnActionFired;
        }

        if (movementController != null)
        {
            movementController.JumpStarted -= OnJumpStarted;
        }
    }

    private void Update()
    {
        if (animator == null)
        {
            return;
        }

        UpdateLocomotion();
        UpdateCharging();
        UpdateActionActive();
    }

    // Movement

    private void UpdateLocomotion()
    {
        bool walking = movementController != null && movementController.IsMoving;
        bool falling = movementController != null && movementController.IsFalling;
        bool jumping = movementController != null && movementController.IsJumping; 

        animator.SetBool(IsWalkingHash, walking);
        animator.SetBool(IsFallingHash, falling);
        animator.SetBool(IsJumpingHash, jumping); 
    }

    // Jump

    private void OnJumpStarted()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetTrigger(JumpHash);
    }

    // Charging

    private void UpdateCharging()
    {
        bool charging = actionController != null && actionController.IsCharging;
        animator.SetBool(IsChargingHash, charging);
    }

    // Action selection

    private void UpdateActionActive()
    {
        bool active = lastFiredAction != null && unit != null && lastFiredAction.GetIsActive(unit);
        animator.SetBool(IsActionActiveHash, active);
    }

    private void OnActionSelectionChanged(UnitAction action)
    {
        if (animator == null)
        {
            return;
        }

        if (action == null)
        {
            lastFiredAction = null;
            animator.SetBool(IsAimingHash,    false);
            animator.SetInteger(ActionIndexHash, ActionNone);
        }
        else
        {
            animator.SetInteger(ActionIndexHash, GetActionIndex(action));
            animator.SetBool(IsAimingHash, true);
        }
    }

    // Action fire

    private void OnActionFired(UnitAction action)
    {
        if (animator == null)
        {
            return;
        }

        lastFiredAction = action;
        animator.SetInteger(ActionIndexHash, GetActionIndex(action));

        // Aiming ends the moment the action fires.
        animator.SetBool(IsAimingHash, false);

        // Trigger the one-shot fire animation.
        animator.SetTrigger(FireHash);
    }

    // Helpers
    
    private static int GetActionIndex(UnitAction action)
    {
        if (action == null)               return ActionNone;
        if (action is PunchAction)        return ActionPunch;
        if (action is GunAction)          return ActionGun;
        if (action is DynamiteAction)     return ActionDynamite;
        if (action is JetpackAction)      return ActionJetpack;
        if (action is ExplodeAction)      return ActionExplode;
        if (action is TShirtCannonAction) return ActionTShirtCannon;
        
        Debug.LogWarning($"[UnitAnimator] Unrecognised action type '{action.GetType().Name}'. " +
                         "Defaulting to ActionIndex 0. Add a case to GetActionIndex().");
        return ActionNone;
    }
}