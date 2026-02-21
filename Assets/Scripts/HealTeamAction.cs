using UnityEngine;

[CreateAssetMenu(menuName = "StickWarfare/Actions/HealTeam")]
public class HealTeamAction : UnitAction
{
    protected override void Execute(Unit unit, TurnManager turnManager)
    {
        Unit[] allUnits = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (Unit target in allUnits)
        {
            if (target == null || !target.IsAlive || target.TeamId != unit.TeamId)
            {
                continue;
            }

            int missing = target.MaxHealth - target.CurrentHealth;
            if (missing > 0)
            {
                target.Heal(missing);
            }
        }
    }
}