using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the three in-game action buttons plus the ultimate button.
/// • Shows them only during the Action phase.
/// • Syncs each button's child Image to the current unit's equipped action icon.
/// • The ultimate button is only interactable when the current unit's team has 1000 dumb points.
/// </summary>
public class ActionButtonsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;

    [Tooltip("The root GameObject that contains all three buttons. " +
             "If left empty the GameObject this script lives on is used instead.")]
    [SerializeField] private GameObject buttonsRoot;

    [Header("Buttons (slots 1 – 3)")]
    [SerializeField] private Button[] actionButtons = new Button[3];

    [Header("Icons — child Image of each button")]
    [SerializeField] private Image[] actionIcons = new Image[3];

    [Header("Ultimate Button")]
    [SerializeField] private Button ultimateButton;
    [SerializeField] private Image ultimateIcon;

    private UnitActionController currentController;
    private TeamCurrencyManager currencyManager;
    private int currentTeamId = -1;

    // Unity lifecycle

    private void Awake()
    {
        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }

        currencyManager = FindFirstObjectByType<TeamCurrencyManager>();
    }

    private void OnEnable()
    {
        if (turnManager != null)
        {
            turnManager.TurnStarted += OnTurnStarted;
            turnManager.PhaseChanged += OnPhaseChanged;
        }

        if (currencyManager != null)
        {
            currencyManager.DumbPointsChanged += OnDumbPointsChanged;
        }

        for (int i = 0; i < actionButtons.Length; i++)
        {
            if (actionButtons[i] == null)
            {
                continue;
            }

            int capturedIndex = i;
            actionButtons[i].onClick.AddListener(() => OnButtonClicked(capturedIndex));
        }

        if (ultimateButton != null)
        {
            ultimateButton.onClick.AddListener(OnUltimateButtonClicked);
        }

        SetVisible(false);
    }

    private void OnDisable()
    {
        if (turnManager != null)
        {
            turnManager.TurnStarted -= OnTurnStarted;
            turnManager.PhaseChanged -= OnPhaseChanged;
        }

        if (currencyManager != null)
        {
            currencyManager.DumbPointsChanged -= OnDumbPointsChanged;
        }

        foreach (Button btn in actionButtons)
        {
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
            }
        }

        if (ultimateButton != null)
        {
            ultimateButton.onClick.RemoveAllListeners();
        }
    }

    // Event handlers

    private void OnTurnStarted(Unit unit)
    {
        currentController = unit != null ? unit.GetComponent<UnitActionController>() : null;
        currentTeamId = unit != null ? unit.TeamId : -1;
    }

    private void OnPhaseChanged(TurnManager.TurnPhase phase)
    {
        bool isActionPhase = phase == TurnManager.TurnPhase.Action;
        SetVisible(isActionPhase);

        if (isActionPhase)
        {
            RefreshIcons();
            RefreshUltimateButton();
        }
    }

    private void OnButtonClicked(int index)
    {
        if (currentController == null)
        {
            return;
        }

        currentController.SelectSlotByIndex(index);
    }

    private void OnUltimateButtonClicked()
    {
        if (currentController == null)
        {
            return;
        }

        currentController.SelectUltimate();
    }

    private void OnDumbPointsChanged(int teamId, int newPoints)
    {
        if (teamId == currentTeamId)
        {
            RefreshUltimateButton();
        }
    }

    // Helpers

    private void RefreshIcons()
    {
        for (int i = 0; i < actionIcons.Length; i++)
        {
            if (actionIcons[i] == null)
            {
                continue;
            }

            UnitAction action = currentController != null ? currentController.GetSlot(i) : null;
            Sprite icon = action != null ? action.Icon : null;
            actionIcons[i].sprite = icon;
            actionIcons[i].enabled = icon != null;
        }

        RefreshUltimateIcon();
    }

    private void RefreshUltimateIcon()
    {
        if (ultimateIcon == null || currentTeamId < 0 || currentTeamId >= MatchSetupData.Teams.Count)
        {
            return;
        }

        UnitAction ultimate = MatchSetupData.Teams[currentTeamId].UltimateAction;
        Sprite icon = ultimate != null ? ultimate.Icon : null;
        ultimateIcon.sprite = icon;
        ultimateIcon.enabled = icon != null;
    }

    private void RefreshUltimateButton()
    {
        if (ultimateButton == null)
        {
            return;
        }

        bool ready = currencyManager != null
            && currentTeamId >= 0
            && currencyManager.GetDumbPoints(currentTeamId) >= 1000
            && currentTeamId < MatchSetupData.Teams.Count
            && MatchSetupData.Teams[currentTeamId].UltimateAction != null;

        ultimateButton.interactable = ready;
        RefreshUltimateIcon();
    }

    private void SetVisible(bool visible)
    {
        GameObject root = buttonsRoot != null ? buttonsRoot : gameObject;
        if (root.activeSelf != visible)
        {
            root.SetActive(visible);
        }
    }
}