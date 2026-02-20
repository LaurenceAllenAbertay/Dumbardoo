using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Unit))]
public class UnitActionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private InputActionReference action1;
    [SerializeField] private InputActionReference action2;
    [SerializeField] private InputActionReference action3;
    [SerializeField] private InputActionReference confirmAction;

    [Header("Slots")]
    [SerializeField] private UnitAction slot1;
    [SerializeField] private UnitAction slot2;
    [SerializeField] private UnitAction slot3;

    [Header("Gizmos")]
    [SerializeField] private bool showPunchGizmo = true;
    [SerializeField] private Color punchGizmoColor = new Color(1f, 0.4f, 0.2f, 0.35f);

    private Unit unit;
    private UnitAction selectedAction;
    private bool actionUsed;
    private bool isCharging;
    private float chargeTime;
    private float currentChargeForce;
    private bool turnEventsBound;

    /// <summary>True while the player is holding the confirm button to charge a Dynamite throw.</summary>
    public bool IsCharging => isCharging;

    /// <summary>
    /// Charge progress in the range [0, 1] driven by the same PingPong used for
    /// currentChargeForce, so the slider mirrors exactly what the throw will do.
    /// Returns 0 when not charging or when no Dynamite is selected.
    /// </summary>
    public float ChargeNormalized
    {
        get
        {
            if (!isCharging || !(selectedAction is DynamiteAction Dynamite))
            {
                return 0f;
            }

            return Mathf.PingPong(chargeTime * Dynamite.ChargeSpeed, 1f);
        }
    }

    private void Awake()
    {
        unit = GetComponent<Unit>();
        EnsureTurnManager();
    }

    private void OnEnable()
    {
        EnsureTurnManager();
        Bind(action1, OnAction1);
        Bind(action2, OnAction2);
        Bind(action3, OnAction3);
        BindConfirm();

        BindTurnEvents();
    }

    private void OnDisable()
    {
        Unbind(action1, OnAction1);
        Unbind(action2, OnAction2);
        Unbind(action3, OnAction3);
        UnbindConfirm();

        UnbindTurnEvents();
    }

    private void OnAction1(InputAction.CallbackContext context)
    {
        Select(slot1);
    }

    private void OnAction2(InputAction.CallbackContext context)
    {
        Select(slot2);
    }

    private void OnAction3(InputAction.CallbackContext context)
    {
        Select(slot3);
    }

    private void OnConfirm(InputAction.CallbackContext context)
    {
        // Enter is also bound to EndTurn. Ignore keyboard triggers here so that
        // pressing Enter to end a turn does not simultaneously fire the action.
        if (context.control?.device is UnityEngine.InputSystem.Keyboard)
        {
            return;
        }

        if (selectedAction == null)
        {
            return;
        }

        if (actionUsed)
        {
            return;
        }

        if (selectedAction is DynamiteAction)
        {
            return;
        }

        ExecuteSelected();
    }

    private void OnConfirmStarted(InputAction.CallbackContext context)
    {
        if (selectedAction == null || actionUsed)
        {
            return;
        }

        DynamiteAction Dynamite = selectedAction as DynamiteAction;
        if (Dynamite == null)
        {
            return;
        }

        if (!IsMyActionPhase())
        {
            return;
        }

        isCharging = true;
        chargeTime = 0f;
        currentChargeForce = Dynamite.MinThrowForce;
    }

    private void OnConfirmCanceled(InputAction.CallbackContext context)
    {
        if (!isCharging)
        {
            return;
        }

        isCharging = false;

        DynamiteAction Dynamite = selectedAction as DynamiteAction;
        if (Dynamite == null)
        {
            return;
        }

        Dynamite.SetThrowForce(currentChargeForce);
        ExecuteSelected();
    }

    private void Select(UnitAction action)
    {
        if (action == null || actionUsed)
        {
            return;
        }

        if (!IsMyActionPhase())
        {
            return;
        }

        if (selectedAction == action)
        {
            Debug.Log($"{unit.name} canceled {action.ActionName}.");
            selectedAction = null;
            isCharging = false;
            return;
        }

        if (selectedAction != null)
        {
            Debug.Log($"{unit.name} canceled {selectedAction.ActionName}.");
        }

        selectedAction = action;
        Debug.Log($"{unit.name} selected {action.ActionName}.");
    }

    private bool IsMyActionPhase()
    {
        return unit.IsTurnActive
            && turnManager != null
            && turnManager.CurrentUnit == unit
            && turnManager.Phase == TurnManager.TurnPhase.Action;
    }

    private void OnTurnStarted(Unit startedUnit)
    {
        if (startedUnit == unit)
        {
            ResetActionState();
        }
    }

    private void OnTurnEnded(Unit endedUnit)
    {
        if (endedUnit == unit)
        {
            ResetActionState();
        }
    }

    private void OnPhaseChanged(TurnManager.TurnPhase phase)
    {
        if (phase == TurnManager.TurnPhase.Action && turnManager != null && turnManager.CurrentUnit == unit)
        {
            selectedAction = null;
        }
    }

    private void ResetActionState()
    {
        selectedAction = null;
        actionUsed = false;
        isCharging = false;
        chargeTime = 0f;
        currentChargeForce = 0f;
    }

    private void OnDrawGizmos()
    {
        if (!showPunchGizmo)
        {
            return;
        }

        PunchAction punch = slot1 as PunchAction;
        if (punch == null)
        {
            return;
        }

        Unit gizmoUnit = GetComponent<Unit>();
        if (gizmoUnit == null)
        {
            return;
        }

        Vector3 origin = gizmoUnit.transform.position + gizmoUnit.transform.forward * punch.Range;
        Gizmos.color = punchGizmoColor;
        Gizmos.DrawSphere(origin, punch.HitRadius);
        Gizmos.color = new Color(punchGizmoColor.r, punchGizmoColor.g, punchGizmoColor.b, 1f);
        Gizmos.DrawWireSphere(origin, punch.HitRadius);
    }

    private void Update()
    {
        if (!isCharging)
        {
            return;
        }

        DynamiteAction Dynamite = selectedAction as DynamiteAction;
        if (Dynamite == null)
        {
            isCharging = false;
            return;
        }

        if (!IsMyActionPhase() || actionUsed)
        {
            isCharging = false;
            return;
        }

        chargeTime += Time.deltaTime;
        float t = Mathf.PingPong(chargeTime * Dynamite.ChargeSpeed, 1f);
        currentChargeForce = Mathf.Lerp(Dynamite.MinThrowForce, Dynamite.MaxThrowForce, t);
    }

    private static void Bind(InputActionReference actionRef, System.Action<InputAction.CallbackContext> handler)
    {
        if (actionRef == null || actionRef.action == null)
        {
            return;
        }

        actionRef.action.performed += handler;
        actionRef.action.Enable();
    }

    private static void Unbind(InputActionReference actionRef, System.Action<InputAction.CallbackContext> handler)
    {
        if (actionRef == null || actionRef.action == null)
        {
            return;
        }

        actionRef.action.performed -= handler;
        // Do NOT call .Disable() here. InputActionReferences are shared assets.
        // Disabling them on one unit's destruction kills input for all others.
    }

    private void BindConfirm()
    {
        if (confirmAction == null || confirmAction.action == null)
        {
            return;
        }

        confirmAction.action.performed += OnConfirm;
        confirmAction.action.started += OnConfirmStarted;
        confirmAction.action.canceled += OnConfirmCanceled;
        confirmAction.action.Enable();
    }

    private void UnbindConfirm()
    {
        if (confirmAction == null || confirmAction.action == null)
        {
            return;
        }

        confirmAction.action.performed -= OnConfirm;
        confirmAction.action.started -= OnConfirmStarted;
        confirmAction.action.canceled -= OnConfirmCanceled;
        // Do NOT call .Disable() here. Same reason as Unbind above.
    }

    private void ExecuteSelected()
    {
        if (selectedAction.TryExecute(unit, turnManager))
        {
            actionUsed = true;
            if (turnManager != null && selectedAction.EndsActionImmediately)
            {
                turnManager.NotifyActionEnded(unit);
            }
        }
    }

    public UnitAction GetSlot(int index)
    {
        switch (index)
        {
            case 0:
                return slot1;
            case 1:
                return slot2;
            case 2:
                return slot3;
            default:
                return null;
        }
    }

    /// <summary>Returns a snapshot of all three action slots.</summary>
    public UnitAction[] GetActions()
    {
        return new UnitAction[] { slot1, slot2, slot3 };
    }

    public void SetSlot(int index, UnitAction action)
    {
        switch (index)
        {
            case 0:
                ReplaceSlot(ref slot1, action);
                break;
            case 1:
                ReplaceSlot(ref slot2, action);
                break;
            case 2:
                ReplaceSlot(ref slot3, action);
                break;
        }
    }

    private void ReplaceSlot(ref UnitAction slot, UnitAction action)
    {
        if (slot == selectedAction)
        {
            selectedAction = null;
        }

        slot = action;
    }

    /// <summary>
    /// Selects the action in the given slot (0-based), matching keyboard Action1/2/3 behaviour.
    /// Safe to call from UI; does nothing when it is not this unit's action phase.
    /// </summary>
    public void SelectSlotByIndex(int index)
    {
        switch (index)
        {
            case 0: Select(slot1); break;
            case 1: Select(slot2); break;
            case 2: Select(slot3); break;
        }
    }

    public void SetTurnManager(TurnManager manager)
    {
        if (turnManager == manager)
        {
            return;
        }

        UnbindTurnEvents();
        turnManager = manager;
        BindTurnEvents();
    }

    private void EnsureTurnManager()
    {
        if (turnManager != null)
        {
            return;
        }

        turnManager = FindFirstObjectByType<TurnManager>();
    }

    private void BindTurnEvents()
    {
        if (turnEventsBound || turnManager == null)
        {
            return;
        }

        turnManager.TurnStarted += OnTurnStarted;
        turnManager.TurnEnded += OnTurnEnded;
        turnManager.PhaseChanged += OnPhaseChanged;
        turnEventsBound = true;
    }

    private void UnbindTurnEvents()
    {
        if (!turnEventsBound || turnManager == null)
        {
            turnEventsBound = false;
            return;
        }

        turnManager.TurnStarted -= OnTurnStarted;
        turnManager.TurnEnded -= OnTurnEnded;
        turnManager.PhaseChanged -= OnPhaseChanged;
        turnEventsBound = false;
    }
}