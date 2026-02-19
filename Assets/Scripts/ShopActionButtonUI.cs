using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopActionButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text actionNameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Image iconImage;
    [SerializeField] private string soldOutLabel = "SOLD OUT!";

    public void Configure(UnitAction action, int price, Action onClicked)
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (actionNameText != null)
        {
            actionNameText.text = string.Empty;
        }

        EnsurePriceText();
        SetPriceText(action != null ? $"${price}" : string.Empty);

        if (iconImage == null)
        {
            iconImage = GetComponentInChildren<Image>(true);
        }

        if (iconImage != null)
        {
            iconImage.sprite = action != null ? action.Icon : null;
            iconImage.enabled = iconImage.sprite != null;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (action != null && onClicked != null)
            {
                button.onClick.AddListener(() => onClicked());
                button.interactable = true;
            }
            else
            {
                button.interactable = false;
            }
        }
    }

    public void MarkSoldOut()
    {
        EnsurePriceText();
        SetPriceText(soldOutLabel);
        if (button != null)
        {
            button.interactable = false;
        }
    }

    private void EnsurePriceText()
    {
        if (priceText != null)
        {
            return;
        }

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != actionNameText)
            {
                priceText = texts[i];
                break;
            }
        }
    }

    private void SetPriceText(string value)
    {
        if (priceText != null)
        {
            priceText.text = value;
        }
    }
}
