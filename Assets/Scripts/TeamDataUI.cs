using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeamDataUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text teamNameText;
    [SerializeField] private Transform unitsRemainingRoot;
    [SerializeField] private GameObject playerRemainingPrefab;
    [SerializeField] private Slider totalHealthSlider;

    [Header("Team")]
    [SerializeField] private int teamId = 0;
    [SerializeField] private string teamName = "Team";

    private readonly List<Unit> teamUnits = new List<Unit>();
    private int lastAliveCount = -1;
    private int lastTotalHealth = -1;
    private int lastMaxHealth = -1;
    private bool loggedMissingRefs;

    private void Awake()
    {
        if (teamNameText == null)
        {
            teamNameText = GetComponentInChildren<TMP_Text>(true);
        }

        if (totalHealthSlider == null)
        {
            totalHealthSlider = GetComponentInChildren<Slider>(true);
        }

        RefreshUnits();
        RefreshAll();
    }

    private void OnEnable()
    {
        RefreshUnits();
        RefreshAll();
    }

    private void LateUpdate()
    {
        if (!EnsureReferences())
        {
            return;
        }

        UpdateUnitsRemaining();
        UpdateTeamHealth();
    }

    public void Configure(int id, string name)
    {
        teamId = id;
        if (!string.IsNullOrWhiteSpace(name))
        {
            teamName = name;
        }

        RefreshUnits();
        RefreshAll();
    }

    private void RefreshUnits()
    {
        teamUnits.Clear();
        var units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (var unit in units)
        {
            if (unit != null && unit.TeamId == teamId)
            {
                teamUnits.Add(unit);
            }
        }
    }

    private void RefreshAll()
    {
        if (teamNameText != null)
        {
            teamNameText.text = teamName;
        }

        lastAliveCount = -1;
        lastTotalHealth = -1;
        lastMaxHealth = -1;
        UpdateUnitsRemaining();
        UpdateTeamHealth();
    }

    private void UpdateUnitsRemaining()
    {
        if (unitsRemainingRoot == null || playerRemainingPrefab == null)
        {
            return;
        }

        int aliveCount = 0;
        foreach (var unit in teamUnits)
        {
            if (unit != null && unit.IsAlive)
            {
                aliveCount++;
            }
        }

        if (aliveCount == lastAliveCount)
        {
            return;
        }

        lastAliveCount = aliveCount;
        for (int i = unitsRemainingRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(unitsRemainingRoot.GetChild(i).gameObject);
        }

        for (int i = 0; i < aliveCount; i++)
        {
            Instantiate(playerRemainingPrefab, unitsRemainingRoot);
        }
    }

    private void UpdateTeamHealth()
    {
        if (totalHealthSlider == null)
        {
            return;
        }

        int total = 0;
        int max = 0;
        foreach (var unit in teamUnits)
        {
            if (unit == null || !unit.IsAlive)
            {
                continue;
            }

            total += unit.CurrentHealth;
            max += unit.MaxHealth;
        }

        if (total == lastTotalHealth && max == lastMaxHealth)
        {
            return;
        }

        lastTotalHealth = total;
        lastMaxHealth = max;
        totalHealthSlider.maxValue = Mathf.Max(1, max);
        totalHealthSlider.value = Mathf.Clamp(total, 0, totalHealthSlider.maxValue);
    }

    private bool EnsureReferences()
    {
        if (teamNameText == null || unitsRemainingRoot == null || playerRemainingPrefab == null || totalHealthSlider == null)
        {
            if (!loggedMissingRefs)
            {
                Debug.LogWarning($"{name} TeamDataUI missing refs. TeamNameText: {(teamNameText != null)}, UnitsRoot: {(unitsRemainingRoot != null)}, PlayerPrefab: {(playerRemainingPrefab != null)}, TotalHealthSlider: {(totalHealthSlider != null)}");
                loggedMissingRefs = true;
            }
            return false;
        }

        return true;
    }
}
