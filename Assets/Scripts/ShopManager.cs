using UnityEngine;
using UnityEngine.Events;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private TeamCurrencyManager currencyManager;
    [SerializeField] private ShopPanelUI shopPanel;
    [SerializeField] private UnityEvent allTeamsReady;

    private int nextTeamIndex;
    private bool shopActive;

    private void OnEnable()
    {
        EnsureReferences();
        if (turnManager != null)
        {
            turnManager.TeamWon += OnTeamWon;
        }
    }

    private void OnDisable()
    {
        if (turnManager != null)
        {
            turnManager.TeamWon -= OnTeamWon;
        }
    }

    private void EnsureReferences()
    {
        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }

        if (currencyManager == null)
        {
            currencyManager = FindFirstObjectByType<TeamCurrencyManager>();
        }

        if (shopPanel == null)
        {
            shopPanel = FindFirstObjectByType<ShopPanelUI>();
        }
    }

    private void OnTeamWon(int winningTeamId)
    {
        if (currencyManager == null || shopPanel == null)
        {
            return;
        }

        shopActive = true;
        nextTeamIndex = 0;
        OpenNextTeam();
    }

    public void OpenNextTeam()
    {
        if (!shopActive || shopPanel == null)
        {
            return;
        }

        if (nextTeamIndex >= MatchSetupData.Teams.Count)
        {
            shopPanel.Close();
            shopActive = false;
            allTeamsReady?.Invoke();
            return;
        }

        int teamId = nextTeamIndex;
        nextTeamIndex++;
        shopPanel.Open(currencyManager, teamId, GetTeamName(teamId));
    }

    public void CloseShop()
    {
        if (shopPanel != null)
        {
            shopPanel.Close();
        }

        shopActive = false;
        nextTeamIndex = 0;
    }

    private string GetTeamName(int teamId)
    {
        if (teamId >= 0 && teamId < MatchSetupData.Teams.Count)
        {
            string name = MatchSetupData.Teams[teamId].TeamName;
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        return $"Team {teamId + 1}";
    }
}
