using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [System.Serializable]
    public class TeamUI
    {
        public string defaultTeamName = "Team";
        public TMP_InputField teamNameInput;
        public TMP_Dropdown unitCountDropdown;
        public Transform unitNamesRoot;
        public TMP_InputField unitNamePrefab;
    }

    [Header("Teams")]
    [SerializeField] private TeamUI[] teams = new TeamUI[0];

    [Header("Unit Count")]
    [SerializeField] private int minUnits = 2;
    [SerializeField] private int maxUnits = 6;

    private void Awake()
    {
        for (int i = 0; i < teams.Length; i++)
        {
            InitializeTeam(teams[i], i + 1);
        }
    }

    private void InitializeTeam(TeamUI team, int index)
    {
        if (team == null)
        {
            return;
        }

        if (team.teamNameInput != null && string.IsNullOrWhiteSpace(team.teamNameInput.text))
        {
            team.teamNameInput.text = string.IsNullOrWhiteSpace(team.defaultTeamName)
                ? $"Team {index}"
                : team.defaultTeamName;
        }

        if (team.unitCountDropdown != null)
        {
            BuildUnitCountOptions(team.unitCountDropdown);
            team.unitCountDropdown.onValueChanged.RemoveListener(_ => OnUnitCountChanged(team));
            team.unitCountDropdown.onValueChanged.AddListener(_ => OnUnitCountChanged(team));
        }

        RebuildUnitNameInputs(team);
    }

    private void BuildUnitCountOptions(TMP_Dropdown dropdown)
    {
        dropdown.options.Clear();
        for (int i = minUnits; i <= maxUnits; i++)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData(i.ToString()));
        }

        dropdown.value = 0;
        dropdown.RefreshShownValue();
    }

    private void OnUnitCountChanged(TeamUI team)
    {
        RebuildUnitNameInputs(team);
    }

    private void RebuildUnitNameInputs(TeamUI team)
    {
        if (team == null || team.unitNamesRoot == null || team.unitNamePrefab == null)
        {
            return;
        }

        int desired = GetSelectedUnitCount(team);
        for (int i = team.unitNamesRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(team.unitNamesRoot.GetChild(i).gameObject);
        }

        for (int i = 0; i < desired; i++)
        {
            TMP_InputField field = Instantiate(team.unitNamePrefab, team.unitNamesRoot);
            if (field != null && string.IsNullOrWhiteSpace(field.text))
            {
                field.text = $"Unit {i + 1}";
            }
        }
    }

    private int GetSelectedUnitCount(TeamUI team)
    {
        if (team == null || team.unitCountDropdown == null)
        {
            return minUnits;
        }

        int index = Mathf.Clamp(team.unitCountDropdown.value, 0, maxUnits - minUnits);
        return minUnits + index;
    }

    public void StartMatch(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("MainMenuController: sceneName is empty.");
            return;
        }

        MatchSetupData.Clear();
        foreach (var team in teams)
        {
            if (team == null)
            {
                continue;
            }

            string teamName = team.teamNameInput != null && !string.IsNullOrWhiteSpace(team.teamNameInput.text)
                ? team.teamNameInput.text
                : team.defaultTeamName;

            int unitCount = GetSelectedUnitCount(team);
            List<string> unitNames = new List<string>(unitCount);

            if (team.unitNamesRoot != null)
            {
                for (int i = 0; i < team.unitNamesRoot.childCount; i++)
                {
                    TMP_InputField field = team.unitNamesRoot.GetChild(i).GetComponent<TMP_InputField>();
                    if (field != null && !string.IsNullOrWhiteSpace(field.text))
                    {
                        unitNames.Add(field.text);
                    }
                }
            }

            while (unitNames.Count < unitCount)
            {
                unitNames.Add($"Unit {unitNames.Count + 1}");
            }

            MatchSetupData.Teams.Add(new MatchSetupData.TeamSetup(teamName, unitCount, unitNames));
        }

        SceneManager.LoadScene(sceneName);
    }
}
