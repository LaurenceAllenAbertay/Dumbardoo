using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the three in-game action buttons.
/// • Shows them only during the Action phase.
/// • Syncs each button's child Image to the current unit's equipped action icon.
/// • Clicking a button is equivalent to pressing the Action1 / Action2 / Action3 input.
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

    // ── Runtime state ─────────────────────────────────────────────────────────

    private UnitActionController currentController;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }
    }

    private void OnEnable()
    {
        if (turnManager != null)
        {
            turnManager.TurnStarted += OnTurnStarted;
            turnManager.PhaseChanged += OnPhaseChanged;
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

        // Start hidden; OnPhaseChanged will reveal them when appropriate.
        SetVisible(false);
    }

    private void OnDisable()
    {
        if (turnManager != null)
        {
            turnManager.TurnStarted -= OnTurnStarted;
            turnManager.PhaseChanged -= OnPhaseChanged;
        }

        foreach (Button btn in actionButtons)
        {
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
            }
        }
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnTurnStarted(Unit unit)
    {
        currentController = unit != null ? unit.GetComponent<UnitActionController>() : null;
        // Icons will be refreshed when the phase switches to Action.
    }

    private void OnPhaseChanged(TurnManager.TurnPhase phase)
    {
        bool isActionPhase = phase == TurnManager.TurnPhase.Action;
        SetVisible(isActionPhase);

        if (isActionPhase)
        {
            RefreshIcons();
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Syncs each icon Image to the sprite on the current unit's equipped action.
    /// </summary>
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
    }

    /// <summary>
    /// Shows or hides the button root without destroying it.
    /// </summary>
    private void SetVisible(bool visible)
    {
        GameObject root = buttonsRoot != null ? buttonsRoot : gameObject;
        if (root.activeSelf != visible)
        {
            root.SetActive(visible);
        }
    }
}