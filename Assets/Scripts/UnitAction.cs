using UnityEngine;

public abstract class UnitAction : ScriptableObject
{
    [SerializeField] private string actionName = "Action";

    public string ActionName => actionName;

    public bool TryExecute(Unit unit, TurnManager turnManager)
    {
        if (unit == null || turnManager == null)
        {
            return false;
        }

        if (!unit.IsTurnActive || turnManager.CurrentUnit != unit)
        {
            return false;
        }

        if (turnManager.Phase != TurnManager.TurnPhase.Action)
        {
            return false;
        }

        Execute(unit, turnManager);
        return true;
    }

    protected abstract void Execute(Unit unit, TurnManager turnManager);
}
