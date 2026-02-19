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

        if (teamUIs == null || teamUIs.Length == 0)
        {
            teamUIs = FindObjectsByType<TeamDataUI>(FindObjectsSortMode.None);
        }

        EnsureDefaultSetup();
        SpawnTeams();
        ApplyTeamUI();
        ResetTurns();
    }

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

    private void SpawnTeams()
    {
        Bounds bounds = spawnVolume.bounds;
        for (int teamIndex = 0; teamIndex < MatchSetupData.Teams.Count; teamIndex++)
        {
            MatchSetupData.TeamSetup team = MatchSetupData.Teams[teamIndex];
            int teamId = teamIndex;
            string teamName = string.IsNullOrWhiteSpace(team.TeamName) ? $"Team {teamIndex + 1}" : team.TeamName;
            GameObject teamRoot = new GameObject(teamName);

            for (int unitIndex = 0; unitIndex < team.UnitCount; unitIndex++)
            {
                Vector3 spawnPoint = FindSpawnPoint(bounds);
                Unit unitInstance = Instantiate(unitPrefab, spawnPoint, Quaternion.identity);
                unitInstance.transform.SetParent(teamRoot.transform, true);
                unitInstance.SetTeamId(teamId);

                string unitName = GetUnitName(team, unitIndex);
                unitInstance.SetUnitName(unitName);
                UnitWorldUI worldUI = unitInstance.GetComponentInChildren<UnitWorldUI>(true);
                if (worldUI != null)
                {
                    worldUI.RefreshName();
                }

                UnitActionController actionController = unitInstance.GetComponent<UnitActionController>();
                if (actionController != null && turnManager != null)
                {
                    actionController.SetTurnManager(turnManager);
                }

                ApplyTeamMaterial(unitInstance, teamId);
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
