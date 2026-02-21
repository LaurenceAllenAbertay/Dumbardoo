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
    [SerializeField] private Slider dumbPointsSlider;
    [SerializeField] private Image ultimateIconImage;
    [SerializeField] private TMP_Text goldText;

    [Header("Team")]
    [SerializeField] private int teamId = 0;
    [SerializeField] private string teamName = "Team";

    [Header("Currency")]
    [SerializeField] private TeamCurrencyManager currencyManager;

    private readonly List<Unit> teamUnits = new List<Unit>();
    private int initialTotalMaxHealth;
    private int lastAliveCount = -1;
    private int lastTotalHealth = -1;
    private int lastMaxHealth = -1;
    private int lastDumbPoints = -1;
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

        if (dumbPointsSlider != null)
        {
            dumbPointsSlider.minValue = 0f;
            dumbPointsSlider.maxValue = 1000f;
            dumbPointsSlider.interactable = false;
        }

        RefreshUnits();
        RefreshAll();
    }

    private void OnEnable()
    {
        RefreshUnits();
        RefreshAll();

        if (currencyManager == null)
        {
            currencyManager = UnityEngine.Object.FindFirstObjectByType<TeamCurrencyManager>();
        }

        if (currencyManager != null)
        {
            currencyManager.GoldChanged += OnGoldChanged;
            currencyManager.DumbPointsChanged += OnDumbPointsChanged;
            UpdateGold();
            UpdateDumbPoints();
        }

        MatchSetupData.UltimateChanged += OnUltimateChanged;
        UpdateUltimateIcon();
    }

    private void OnDisable()
    {
        if (currencyManager != null)
        {
            currencyManager.GoldChanged -= OnGoldChanged;
            currencyManager.DumbPointsChanged -= OnDumbPointsChanged;
        }

        MatchSetupData.UltimateChanged -= OnUltimateChanged;
    }

    private void LateUpdate()
    {
        if (!EnsureReferences())
        {
            return;
        }

        PruneDeadUnits();
        UpdateUnitsRemaining();
        UpdateTeamHealth();
        UpdateDumbPoints();
    }

    public void Configure(int id, string name)
    {
        teamId = id;
        if (!string.IsNullOrWhiteSpace(name))
        {
            teamName = name;
        }

        if (currencyManager == null)
        {
            currencyManager = UnityEngine.Object.FindFirstObjectByType<TeamCurrencyManager>();
        }

        RefreshUnits();
        RefreshAll();
        UpdateGold();
        UpdateDumbPoints();
        UpdateUltimateIcon();
    }

    private void RefreshUnits()
    {
        teamUnits.Clear();
        initialTotalMaxHealth = 0;
        var units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (var unit in units)
        {
            if (unit != null && unit.TeamId == teamId)
            {
                teamUnits.Add(unit);
                initialTotalMaxHealth += unit.MaxHealth;
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
        lastDumbPoints = -1;
        UpdateUnitsRemaining();
        UpdateTeamHealth();
        UpdateUltimateIcon();
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
        foreach (var unit in teamUnits)
        {
            if (unit == null || !unit.IsAlive)
            {
                continue;
            }

            total += unit.CurrentHealth;
        }

        // Use the fixed initial max so the slider correctly decreases as units
        // die.  Without this, both total and max shrink together whenever a unit
        // is pruned from the list, making the bar look unchanged or even rise.
        int max = Mathf.Max(1, initialTotalMaxHealth);

        if (total == lastTotalHealth && max == lastMaxHealth)
        {
            return;
        }

        lastTotalHealth = total;
        lastMaxHealth = max;
        totalHealthSlider.maxValue = max;
        totalHealthSlider.value = Mathf.Clamp(total, 0, max);
    }

    private void UpdateGold()
    {
        if (goldText == null || currencyManager == null)
        {
            return;
        }

        goldText.text = $"${currencyManager.GetGold(teamId)}";
    }

    private void UpdateDumbPoints()
    {
        if (dumbPointsSlider == null || currencyManager == null)
        {
            return;
        }

        int pts = currencyManager.GetDumbPoints(teamId);
        if (pts == lastDumbPoints)
        {
            return;
        }

        lastDumbPoints = pts;
        dumbPointsSlider.value = pts;
    }

    private void OnGoldChanged(int changedTeamId, int newGold)
    {
        if (changedTeamId == teamId)
        {
            UpdateGold();
        }
    }

    private void OnDumbPointsChanged(int changedTeamId, int newPoints)
    {
        if (changedTeamId == teamId)
        {
            lastDumbPoints = -1;
            UpdateDumbPoints();
        }
    }

    private void UpdateUltimateIcon()
    {
        if (ultimateIconImage == null)
        {
            return;
        }

        UnitAction ultimate = teamId >= 0 && teamId < MatchSetupData.Teams.Count
            ? MatchSetupData.Teams[teamId].UltimateAction
            : null;

        ultimateIconImage.sprite = ultimate != null ? ultimate.Icon : null;
        ultimateIconImage.enabled = ultimateIconImage.sprite != null;
    }

    private void OnUltimateChanged(int changedTeamId, UnitAction action)
    {
        if (changedTeamId == teamId)
        {
            UpdateUltimateIcon();
        }
    }

    private void PruneDeadUnits()
    {
        bool removed = false;
        for (int i = teamUnits.Count - 1; i >= 0; i--)
        {
            Unit unit = teamUnits[i];
            if (unit == null || !unit.IsAlive)
            {
                teamUnits.RemoveAt(i);
                removed = true;
            }
        }

        if (removed)
        {
            lastAliveCount = -1;
            lastTotalHealth = -1;
            lastMaxHealth = -1;
        }
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