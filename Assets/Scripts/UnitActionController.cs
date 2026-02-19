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
        if (selectedAction == null)
        {
            return;
        }

        if (actionUsed)
        {
            return;
        }

        if (selectedAction is GrenadeAction)
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

        GrenadeAction grenade = selectedAction as GrenadeAction;
        if (grenade == null)
        {
            return;
        }

        if (!IsMyActionPhase())
        {
            return;
        }

        isCharging = true;
        chargeTime = 0f;
        currentChargeForce = grenade.MinThrowForce;
    }

    private void OnConfirmCanceled(InputAction.CallbackContext context)
    {
        if (!isCharging)
        {
            return;
        }

        isCharging = false;

        GrenadeAction grenade = selectedAction as GrenadeAction;
        if (grenade == null)
        {
            return;
        }

        grenade.SetThrowForce(currentChargeForce);
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

        GrenadeAction grenade = selectedAction as GrenadeAction;
        if (grenade == null)
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
        float t = Mathf.PingPong(chargeTime * grenade.ChargeSpeed, 1f);
        currentChargeForce = Mathf.Lerp(grenade.MinThrowForce, grenade.MaxThrowForce, t);
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
        actionRef.action.Disable();
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
        confirmAction.action.Disable();
    }

    private void ExecuteSelected()
    {
        if (selectedAction.TryExecute(unit, turnManager))
        {
            actionUsed = true;
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
