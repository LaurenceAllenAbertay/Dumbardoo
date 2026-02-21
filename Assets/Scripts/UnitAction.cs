using UnityEngine;

public abstract class UnitAction : ScriptableObject
{
    [SerializeField] private string actionName = "Action";
    [SerializeField] private Sprite icon;
    [SerializeField] private int baseCost = 150;
    [SerializeField] private int priceVariance = 20;
    [SerializeField] private int dumbPoints = 10;
    [SerializeField] private bool isUltimate = false;

    public string ActionName => actionName;
    public Sprite Icon => icon;
    public int BaseCost => baseCost;
    public int PriceVariance => priceVariance;
    public int DumbPoints => dumbPoints;
    public bool IsUltimate => isUltimate;
    public virtual bool EndsActionImmediately => true;

    /// <summary>
    /// Returns true while this action should hold a looping animator state on the given unit.
    /// Override in actions that have sustained animation (e.g. jetpack thrust).
    /// </summary>
    public virtual bool GetIsActive(Unit unit) => false;

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