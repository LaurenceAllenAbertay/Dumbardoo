using System.Collections.Generic;
using UnityEngine;

public class MatchSetupSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private Unit unitPrefab;

    [Header("Spawn Area")]
    [SerializeField] private BoxCollider spawnVolume;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundRaycastHeight = 8f;
    [SerializeField] private float groundOffset = 0.05f;
    [SerializeField] private int maxSpawnAttempts = 20;

    [Header("Team Materials")]
    [SerializeField] private Material team1Material;
    [SerializeField] private Material team2Material;

    [Header("UI")]
    [SerializeField] private TeamDataUI[] teamUIs = new TeamDataUI[0];

    [Header("Turn System")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private TeamCurrencyManager currencyManager;

    // Tracks the root GameObjects that own each team's units so they can be
    // destroyed cleanly at the start of every new round.
    private readonly List<GameObject> teamRoots = new List<GameObject>();

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (unitPrefab == null || spawnVolume == null)
        {
            Debug.LogWarning("MatchSetupSpawner: Missing unitPrefab or spawnVolume.");
            return;
        }

        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }

        if (currencyManager == null)
        {
            currencyManager = FindFirstObjectByType<TeamCurrencyManager>();
        }

        if (teamUIs == null || teamUIs.Length == 0)
        {
            teamUIs = FindObjectsByType<TeamDataUI>(FindObjectsSortMode.None);
        }

        EnsureDefaultSetup();

        if (currencyManager != null)
        {
            currencyManager.InitializeFromMatchSetupData();
        }

        SpawnTeams();
        ApplyTeamUI();
        ResetTurns();
    }

    private void OnEnable()
    {
        Unit.UnitDied += OnUnitDied;
    }

    private void OnDisable()
    {
        Unit.UnitDied -= OnUnitDied;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by ShopManager after every team has finished shopping.
    /// Destroys all existing units, respawns them at new random positions,
    /// restores their action loadouts from the roster (including any changes
    /// made in the shop), and restarts the turn order.
    /// </summary>
    public void RespawnForNewRound()
    {
        // Save current actions for units still alive (dead units were already
        // saved by OnUnitDied when they took their fatal hit).
        SyncAliveUnitsToRoster();

        // Destroy every team root and all the unit children under it.
        foreach (GameObject root in teamRoots)
        {
            if (root != null)
            {
                Destroy(root);
            }
        }

        teamRoots.Clear();

        // Reset currency for a clean new round.
        if (currencyManager != null)
        {
            currencyManager.InitializeFromMatchSetupData();
        }

        // Respawn — SpawnTeams detects that UnitSlots is already populated and
        // applies the saved actions instead of reading the prefab defaults.
        SpawnTeams();
        ApplyTeamUI();
        ResetTurns();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void EnsureDefaultSetup()
    {
        if (MatchSetupData.Teams.Count > 0)
        {
            return;
        }

        MatchSetupData.Clear();
        for (int teamIndex = 0; teamIndex < 2; teamIndex++)
        {
            string teamName = $"Team {teamIndex + 1}";
            List<string> unitNames = new List<string>();
            for (int i = 0; i < 4; i++)
            {
                unitNames.Add($"Unit {i + 1}");
            }

            MatchSetupData.Teams.Add(new MatchSetupData.TeamSetup(teamName, 4, unitNames));
        }
    }

    /// <summary>
    /// Spawns (or respawns) all units.
    /// • First round  — UnitSlots is empty; slots are initialised here and the
    ///                  prefab's default action loadout is saved into each slot.
    /// • Later rounds — UnitSlots already has entries with the actions chosen
    ///                  in the shop; LiveUnit references are updated in place
    ///                  and the saved actions are applied to each new unit.
    /// </summary>
    private void SpawnTeams()
    {
        Bounds bounds = spawnVolume.bounds;

        for (int teamIndex = 0; teamIndex < MatchSetupData.Teams.Count; teamIndex++)
        {
            MatchSetupData.TeamSetup team = MatchSetupData.Teams[teamIndex];
            bool isRespawn = team.UnitSlots.Count > 0;

            if (!isRespawn)
            {
                // First spawn: create one slot entry per unit.
                team.UnitSlots.Clear();
                for (int i = 0; i < team.UnitCount; i++)
                {
                    team.UnitSlots.Add(new MatchSetupData.UnitSlotData(GetUnitName(team, i)));
                }
            }

            string teamName = string.IsNullOrWhiteSpace(team.TeamName)
                ? $"Team {teamIndex + 1}"
                : team.TeamName;

            GameObject teamRoot = new GameObject(teamName);
            teamRoots.Add(teamRoot);

            for (int unitIndex = 0; unitIndex < team.UnitSlots.Count; unitIndex++)
            {
                MatchSetupData.UnitSlotData slotData = team.UnitSlots[unitIndex];

                Vector3 spawnPoint = FindSpawnPoint(bounds);
                Unit unitInstance = Instantiate(unitPrefab, spawnPoint, Quaternion.identity);
                unitInstance.transform.SetParent(teamRoot.transform, true);
                unitInstance.SetTeamId(teamIndex);
                unitInstance.SetUnitName(slotData.UnitName);

                // Update the roster to point at the freshly created unit.
                slotData.LiveUnit = unitInstance;

                UnitWorldUI worldUI = unitInstance.GetComponentInChildren<UnitWorldUI>(true);
                if (worldUI != null)
                {
                    worldUI.RefreshName();
                }

                UnitActionController actionController = unitInstance.GetComponent<UnitActionController>();
                if (actionController != null)
                {
                    if (turnManager != null)
                    {
                        actionController.SetTurnManager(turnManager);
                    }

                    if (isRespawn)
                    {
                        // Apply the actions saved from the previous round / shop.
                        for (int i = 0; i < 3; i++)
                        {
                            if (slotData.Actions[i] != null)
                            {
                                actionController.SetSlot(i, slotData.Actions[i]);
                            }
                        }
                    }
                    else
                    {
                        // Record the prefab's default actions so the shop can
                        // show them and future respawns can restore them.
                        for (int i = 0; i < 3; i++)
                        {
                            slotData.Actions[i] = actionController.GetSlot(i);
                        }
                    }
                }

                ApplyTeamMaterial(unitInstance, teamIndex);
            }
        }
    }

    private void ApplyTeamUI()
    {
        for (int i = 0; i < MatchSetupData.Teams.Count; i++)
        {
            if (teamUIs == null || i >= teamUIs.Length || teamUIs[i] == null)
            {
                continue;
            }

            MatchSetupData.TeamSetup team = MatchSetupData.Teams[i];
            teamUIs[i].Configure(i, team.TeamName);
        }
    }

    private void ResetTurns()
    {
        if (turnManager == null)
        {
            return;
        }

        turnManager.BuildTurnOrder();
        turnManager.StartTurns();
    }

    /// <summary>
    /// Writes each alive unit's current action loadout into its roster slot
    /// so nothing is lost when the unit is destroyed during RespawnForNewRound.
    /// </summary>
    private void SyncAliveUnitsToRoster()
    {
        foreach (MatchSetupData.TeamSetup team in MatchSetupData.Teams)
        {
            foreach (MatchSetupData.UnitSlotData slot in team.UnitSlots)
            {
                if (slot.LiveUnit == null)
                {
                    continue;
                }

                UnitActionController ctrl = slot.LiveUnit.GetComponent<UnitActionController>();
                if (ctrl == null)
                {
                    continue;
                }

                for (int i = 0; i < 3; i++)
                {
                    slot.Actions[i] = ctrl.GetSlot(i);
                }
            }
        }
    }

    /// <summary>
    /// Listens to Unit.UnitDied so that a dying unit's action loadout is saved
    /// to its roster slot before the GameObject is destroyed.
    /// </summary>
    private void OnUnitDied(Unit unit)
    {
        if (unit == null)
        {
            return;
        }

        foreach (MatchSetupData.TeamSetup team in MatchSetupData.Teams)
        {
            foreach (MatchSetupData.UnitSlotData slot in team.UnitSlots)
            {
                if (slot.LiveUnit != unit)
                {
                    continue;
                }

                // Save the loadout while the GameObject is still fully intact.
                UnitActionController ctrl = unit.GetComponent<UnitActionController>();
                if (ctrl != null)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        slot.Actions[i] = ctrl.GetSlot(i);
                    }
                }

                // Clear the live reference — the GameObject is about to be destroyed.
                slot.LiveUnit = null;
                return;
            }
        }
    }

    private Vector3 FindSpawnPoint(Bounds bounds)
    {
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float z = Random.Range(bounds.min.z, bounds.max.z);
            Vector3 rayStart = new Vector3(x, bounds.max.y + groundRaycastHeight, z);
            float rayDistance = bounds.size.y + groundRaycastHeight + 5f;

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                return hit.point + Vector3.up * groundOffset;
            }
        }

        return bounds.center;
    }

    private string GetUnitName(MatchSetupData.TeamSetup team, int index)
    {
        if (team.UnitNames != null && index < team.UnitNames.Count)
        {
            string name = team.UnitNames[index];
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        return $"Unit {index + 1}";
    }

    private void ApplyTeamMaterial(Unit unitInstance, int teamId)
    {
        if (unitInstance == null)
        {
            return;
        }

        Material material = teamId == 0 ? team1Material : team2Material;
        if (material == null)
        {
            return;
        }

        Renderer[] renderers = unitInstance.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            renderer.sharedMaterial = material;
        }
    }
}