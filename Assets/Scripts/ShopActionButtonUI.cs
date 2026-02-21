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
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color ultimateColor = new Color(1f, 0.84f, 0f, 1f);
    [SerializeField] private string soldOutLabel = "SOLD OUT!";

    private Color defaultBackgroundColor = Color.white;

    private void Awake()
    {
        if (backgroundImage != null)
        {
            defaultBackgroundColor = backgroundImage.color;
        }
    }

    public void Configure(UnitAction action, int price, Action onClicked)
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (actionNameText != null)
        {
            actionNameText.text = action != null ? action.ActionName : string.Empty;
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

        SetUltimateStyle(action != null && action.IsUltimate);

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

    private void SetUltimateStyle(bool isUltimate)
    {
        if (backgroundImage == null)
        {
            return;
        }

        backgroundImage.color = isUltimate ? ultimateColor : defaultBackgroundColor;
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