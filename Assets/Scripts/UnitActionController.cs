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

    public PunchAction GetSlot1Punch()
    {
        return slot1 as PunchAction;
    }

    private Unit unit;
    private UnitAction selectedAction;
    private bool actionUsed;

    private void Awake()
    {
        unit = GetComponent<Unit>();
    }

    private void OnEnable()
    {
        Bind(action1, OnAction1);
        Bind(action2, OnAction2);
        Bind(action3, OnAction3);
        Bind(confirmAction, OnConfirm);

        if (turnManager != null)
        {
            turnManager.TurnStarted += OnTurnStarted;
            turnManager.TurnEnded += OnTurnEnded;
            turnManager.PhaseChanged += OnPhaseChanged;
        }
    }

    private void OnDisable()
    {
        Unbind(action1, OnAction1);
        Unbind(action2, OnAction2);
        Unbind(action3, OnAction3);
        Unbind(confirmAction, OnConfirm);

        if (turnManager != null)
        {
            turnManager.TurnStarted -= OnTurnStarted;
            turnManager.TurnEnded -= OnTurnEnded;
            turnManager.PhaseChanged -= OnPhaseChanged;
        }
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

        if (selectedAction.TryExecute(unit, turnManager))
        {
            actionUsed = true;
        }
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

        selectedAction = action;
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
}
