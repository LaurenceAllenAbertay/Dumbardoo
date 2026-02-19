using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitCurrentActionsUI : MonoBehaviour
{
    [SerializeField] private TMP_Text unitNameText;
    [SerializeField] private Button[] actionButtons = new Button[0];
    [SerializeField] private Image[] actionIconImages = new Image[0];

    private Unit unit;
    private UnitActionController actionController;
    private Action<Unit, int> onSlotSelected;

    public Unit Unit => unit;

    public void Initialize(Unit targetUnit, Action<Unit, int> slotSelected)
    {
        unit = targetUnit;
        onSlotSelected = slotSelected;
        actionController = unit != null ? unit.GetComponent<UnitActionController>() : null;

        Refresh();
        BindButtons();
    }

    public void Refresh()
    {
        if (unitNameText != null)
        {
            unitNameText.text = unit != null ? unit.name : "Unit";
        }

        for (int i = 0; i < actionIconImages.Length; i++)
        {
            UnitAction action = actionController != null ? actionController.GetSlot(i) : null;
            if (actionIconImages[i] != null)
            {
                actionIconImages[i].sprite = action != null ? action.Icon : null;
                actionIconImages[i].enabled = actionIconImages[i].sprite != null;
            }
        }
    }

    private void BindButtons()
    {
        for (int i = 0; i < actionButtons.Length; i++)
        {
            int slotIndex = i;
            if (actionButtons[i] == null)
            {
                continue;
            }

            actionButtons[i].onClick.RemoveAllListeners();
            actionButtons[i].onClick.AddListener(() => onSlotSelected?.Invoke(unit, slotIndex));
        }
    }
}
