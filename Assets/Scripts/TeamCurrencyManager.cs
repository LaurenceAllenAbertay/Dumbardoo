using System;
using System.Collections.Generic;
using UnityEngine;

public class TeamCurrencyManager : MonoBehaviour
{
    [SerializeField] private int baseTeamSize = 4;
    [SerializeField] private int startingGold = 0;

    private readonly Dictionary<int, int> teamGold = new Dictionary<int, int>();
    private readonly Dictionary<int, int> teamSizes = new Dictionary<int, int>();

    public int BaseTeamSize => baseTeamSize;
    public event Action<int, int> GoldChanged;
    public event Action<int, int> DumbPointsChanged;

    private void OnEnable()
    {
        Unit.DamageApplied += OnDamageApplied;
    }

    private void OnDisable()
    {
        Unit.DamageApplied -= OnDamageApplied;
    }

    public void InitializeFromMatchSetupData()
    {
        teamGold.Clear();
        teamSizes.Clear();

        int teamCount = MatchSetupData.Teams.Count;
        for (int i = 0; i < teamCount; i++)
        {
            var team = MatchSetupData.Teams[i];
            int unitCount = Mathf.Max(1, team.UnitCount);
            teamSizes[i] = unitCount;
            teamGold[i] = startingGold;

            // Dumb points persist across rounds — only seed to 0 if not yet present.
            if (!MatchSetupData.TeamDumbPoints.ContainsKey(i))
            {
                MatchSetupData.TeamDumbPoints[i] = 0;
            }
        }
    }

    public int GetTeamSize(int teamId)
    {
        if (teamSizes.TryGetValue(teamId, out int size))
        {
            return size;
        }

        return baseTeamSize;
    }

    public int GetGold(int teamId)
    {
        if (teamGold.TryGetValue(teamId, out int gold))
        {
            return gold;
        }

        return startingGold;
    }

    public void SetTeamSize(int teamId, int size)
    {
        teamSizes[teamId] = Mathf.Max(1, size);
    }

    public void ResetGold(int teamId)
    {
        teamGold[teamId] = startingGold;
        GoldChanged?.Invoke(teamId, startingGold);
    }

    public void AddGold(int teamId, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int current = GetGold(teamId);
        int next = current + amount;
        teamGold[teamId] = next;
        GoldChanged?.Invoke(teamId, next);
    }

    public bool TrySpendGold(int teamId, int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        int current = GetGold(teamId);
        if (current < amount)
        {
            return false;
        }

        int next = current - amount;
        teamGold[teamId] = next;
        GoldChanged?.Invoke(teamId, next);
        return true;
    }

    public int GetDumbPoints(int teamId)
    {
        MatchSetupData.TeamDumbPoints.TryGetValue(teamId, out int pts);
        return pts;
    }

    /// <summary>Adds dumb points for the given team, capped at 1000.</summary>
    public void AddDumbPoints(int teamId, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int current = GetDumbPoints(teamId);
        if (current >= 1000)
        {
            return;
        }

        int next = Mathf.Min(1000, current + amount);
        MatchSetupData.TeamDumbPoints[teamId] = next;
        DumbPointsChanged?.Invoke(teamId, next);
    }

    /// <summary>Returns true and resets dumb points to 0 if the team is at 1000.</summary>
    public bool TrySpendUltimate(int teamId)
    {
        if (GetDumbPoints(teamId) < 1000)
        {
            return false;
        }

        MatchSetupData.TeamDumbPoints[teamId] = 0;
        DumbPointsChanged?.Invoke(teamId, 0);
        return true;
    }

    private void OnDamageApplied(Unit source, Unit target, int amount, string actionName)
    {
        if (source == null || target == null || amount <= 0)
        {
            return;
        }

        if (source.TeamId == target.TeamId)
        {
            return;
        }

        AddGold(source.TeamId, amount);
    }
}