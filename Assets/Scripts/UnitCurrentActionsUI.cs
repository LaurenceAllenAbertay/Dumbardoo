using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitCurrentActionsUI : MonoBehaviour
{
    [SerializeField] private TMP_Text unitNameText;
    [SerializeField] private Button[] actionButtons = new Button[0];
    [SerializeField] private Image[] actionIconImages = new Image[0];

    // Runtime state

    private Unit unit;
    private UnitActionController actionController;

    // Slot-data path (used by the shop so dead units are still listed).
    private MatchSetupData.UnitSlotData slotData;
    private Action<int> onSlotClicked; // slot-data path callback (action slot index only)

    // Legacy path callback kept for any non-shop callers.
    private Action<Unit, int> onSlotSelected;

    public Unit Unit => unit;

    // Initialisation

    /// <summary>
    /// Legacy initialisation using a live Unit reference.
    /// Used by anything outside the shop.
    /// </summary>
    public void Initialize(Unit targetUnit, Action<Unit, int> slotSelected)
    {
        slotData = null;
        unit = targetUnit;
        onSlotSelected = slotSelected;
        onSlotClicked = null;
        actionController = unit != null ? unit.GetComponent<UnitActionController>() : null;

        Refresh();
        BindButtons();
    }

    /// <summary>
    /// Slot-data initialisation used by the shop.
    /// Works for both alive and dead units — the shop passes the roster entry
    /// directly so the list always shows all units that started the round.
    /// </summary>
    public void Initialize(MatchSetupData.UnitSlotData data, Action<int> onActionSlotClicked)
    {
        slotData = data;
        unit = data?.LiveUnit;
        onSlotClicked = onActionSlotClicked;
        onSlotSelected = null;
        actionController = unit != null ? unit.GetComponent<UnitActionController>() : null;

        Refresh();
        BindButtons();
    }

    // Public API

    public void Refresh()
    {
        // Name
        if (unitNameText != null)
        {
            if (slotData != null)
                unitNameText.text = slotData.UnitName ?? "Unit";
            else
                unitNameText.text = unit != null ? unit.name : "Unit";
        }

        // Action icons
        for (int i = 0; i < actionIconImages.Length; i++)
        {
            UnitAction action = null;

            if (slotData != null && i < slotData.Actions.Length)
            {
                action = slotData.Actions[i];
            }
            else if (actionController != null)
            {
                action = actionController.GetSlot(i);
            }

            if (actionIconImages[i] != null)
            {
                actionIconImages[i].sprite = action != null ? action.Icon : null;
                actionIconImages[i].enabled = actionIconImages[i].sprite != null;
            }
        }
    }

    // Private helpers

    private void BindButtons()
    {
        for (int i = 0; i < actionButtons.Length; i++)
        {
            if (actionButtons[i] == null)
            {
                continue;
            }

            int slotIndex = i;
            actionButtons[i].onClick.RemoveAllListeners();

            if (onSlotClicked != null)
            {
                actionButtons[i].onClick.AddListener(() => onSlotClicked(slotIndex));
            }
            else if (onSlotSelected != null)
            {
                actionButtons[i].onClick.AddListener(() => onSlotSelected?.Invoke(unit, slotIndex));
            }
        }
    }
}