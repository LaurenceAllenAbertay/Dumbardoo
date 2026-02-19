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
    }

    private void OnDisable()
    {
        if (turnManager != null)
        {
            turnManager.TeamWon -= OnTeamWon;
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
            // Reposition before disabling so LateUpdate does not fire one more
            // frame with a stale transform.
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 introPos = cameraController.IntroOffset;
                cam.transform.position = introPos;

                // Look toward the scene origin.
                if (introPos.sqrMagnitude > 0.001f)
                {
                    Vector3 lookDir = (Vector3.zero - introPos).normalized;
                    cam.transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
                }
            }

            cameraController.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Re-enables the camera controller and locks the cursor, ready for the
    /// next round of gameplay.
    /// </summary>
    private void ExitShopMode()
    {
        if (cameraController != null)
        {
            cameraController.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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