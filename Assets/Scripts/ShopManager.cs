using UnityEngine;
using UnityEngine.Events;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private TeamCurrencyManager currencyManager;
    [SerializeField] private ShopPanelUI shopPanel;
    [SerializeField] private MatchSetupSpawner spawner;
    [SerializeField] private ThirdPersonCameraController cameraController;
    [SerializeField] private UnityEvent allTeamsReady;

    [Tooltip("GameObjects to hide while the shop is open (e.g. team HUD panels, phase text).")]
    [SerializeField] private GameObject[] hudElementsToHide;

    private int nextTeamIndex;
    private bool shopActive;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void OnEnable()
    {
        EnsureReferences();
        if (turnManager != null)
        {
            turnManager.TeamWon += OnTeamWon;
        }

        if (shopPanel != null)
        {
            shopPanel.DoneClicked += OpenNextTeam;
        }
    }

    private void OnDisable()
    {
        if (turnManager != null)
        {
            turnManager.TeamWon -= OnTeamWon;
        }

        if (shopPanel != null)
        {
            shopPanel.DoneClicked -= OpenNextTeam;
        }
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

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

        if (spawner == null)
        {
            spawner = FindFirstObjectByType<MatchSetupSpawner>();
        }

        if (cameraController == null)
        {
            cameraController = FindFirstObjectByType<ThirdPersonCameraController>();
        }
    }

    private void OnTeamWon(int winningTeamId)
    {
        if (currencyManager == null || shopPanel == null)
        {
            return;
        }

        EnterShopMode();

        shopActive = true;
        nextTeamIndex = 0;
        OpenNextTeam();
    }

    /// <summary>
    /// Disables the camera controller, frees the cursor, and moves the camera
    /// to the intro-offset position so there is a clean neutral view behind the
    /// shop UI. No animation is played; the camera simply snaps there.
    /// </summary>
    private void EnterShopMode()
    {
        if (cameraController != null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 introPos = cameraController.IntroOffset;
                cam.transform.position = introPos;

                if (introPos.sqrMagnitude > 0.001f)
                {
                    Vector3 lookDir = (Vector3.zero - introPos).normalized;
                    cam.transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
                }
            }

            cameraController.enabled = false;
        }

        SetHudVisible(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ExitShopMode()
    {
        if (cameraController != null)
        {
            cameraController.enabled = true;
        }

        SetHudVisible(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetHudVisible(bool visible)
    {
        if (hudElementsToHide == null)
        {
            return;
        }

        foreach (GameObject element in hudElementsToHide)
        {
            if (element != null)
            {
                element.SetActive(visible);
            }
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void OpenNextTeam()
    {
        if (!shopActive || shopPanel == null)
        {
            return;
        }

        if (nextTeamIndex >= MatchSetupData.Teams.Count)
        {
            // All teams have finished shopping — start the next round.
            shopPanel.Close();
            shopActive = false;

            ExitShopMode();

            // Respawn all units at fresh positions with their new loadouts.
            if (spawner != null)
            {
                spawner.RespawnForNewRound();
            }

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

        ExitShopMode();
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