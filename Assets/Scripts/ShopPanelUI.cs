using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShopPanelUI : MonoBehaviour
{
    [Header("Team")]
    [SerializeField] private int teamId = 0;

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private ShopActionButtonUI[] actionButtons = new ShopActionButtonUI[0];
    [SerializeField] private Transform unitListRoot;
    [SerializeField] private UnitCurrentActionsUI unitCurrentActionsPrefab;

    [Header("Copy")]
    [SerializeField] private string shopTitleSuffix = " Shop";
    [SerializeField] private string defaultInfoText = "Select an action to purchase.";
    [SerializeField] private string selectReplaceText = "Select an action to replace!";
    [SerializeField] private string notEnoughGoldText = "Not enough gold.";
    [SerializeField] private string purchaseCompleteText = "Action replaced.";
    [SerializeField] private string noActionsAvailableText = "No actions available.";

    private readonly List<UnitCurrentActionsUI> unitEntries = new List<UnitCurrentActionsUI>();
    private readonly List<ShopOffer> offers = new List<ShopOffer>();
    private TeamCurrencyManager currencyManager;
    private UnitAction pendingAction;
    private int pendingPrice;
    private int pendingOfferIndex = -1;
    private string teamName = "Team";

    private struct ShopOffer
    {
        public UnitAction Action;
        public int Price;
        public bool SoldOut;
    }

    public int TeamId => teamId;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void OnDisable()
    {
        if (currencyManager != null)
        {
            currencyManager.GoldChanged -= OnGoldChanged;
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open(TeamCurrencyManager manager, int team, string name)
    {
        currencyManager = manager;
        teamId = team;
        teamName = string.IsNullOrWhiteSpace(name) ? $"Team {teamId + 1}" : name;
        pendingAction = null;
        pendingPrice = 0;
        pendingOfferIndex = -1;

        if (currencyManager != null)
        {
            currencyManager.GoldChanged -= OnGoldChanged;
            currencyManager.GoldChanged += OnGoldChanged;
        }

        gameObject.SetActive(true);
        UpdateTitle();
        UpdateGold();
        BuildOffers();
        BuildUnitList();
        SetInfoText(defaultInfoText);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        pendingAction = null;
        pendingPrice = 0;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void UpdateTitle()
    {
        if (titleText != null)
        {
            titleText.text = $"{teamName}{shopTitleSuffix}";
        }
    }

    private void UpdateGold()
    {
        if (goldText == null || currencyManager == null)
        {
            return;
        }

        goldText.text = $"${currencyManager.GetGold(teamId)}";
    }

    private void OnGoldChanged(int changedTeamId, int newGold)
    {
        if (changedTeamId == teamId)
        {
            UpdateGold();
        }
    }

    private void BuildOffers()
    {
        offers.Clear();
        List<UnitAction> actionPool = GetActionPool();
        if (actionPool.Count == 0)
        {
            SetInfoText(noActionsAvailableText);
        }

        int offerCount = Mathf.Min(actionButtons.Length, actionPool.Count);
        Shuffle(actionPool);

        for (int i = 0; i < actionButtons.Length; i++)
        {
            if (i < offerCount)
            {
                UnitAction action = actionPool[i];
                int price = GetScaledRandomPrice(action);
                offers.Add(new ShopOffer { Action = action, Price = price });

                if (actionButtons[i] != null)
                {
                    int index = i;
                    actionButtons[i].Configure(action, price, () => OnOfferSelected(index));
                }
            }
            else if (actionButtons[i] != null)
            {
                actionButtons[i].Configure(null, 0, null);
            }
        }
    }

    /// <summary>
    /// Builds the unit list from the team's roster in MatchSetupData.
    /// This includes units that have already died this round, so players can
    /// pre-assign actions to every unit before the next round begins.
    /// </summary>
    private void BuildUnitList()
    {
        if (unitListRoot == null || unitCurrentActionsPrefab == null)
        {
            return;
        }

        for (int i = unitListRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(unitListRoot.GetChild(i).gameObject);
        }

        unitEntries.Clear();

        if (teamId >= MatchSetupData.Teams.Count)
        {
            return;
        }

        List<MatchSetupData.UnitSlotData> slots = MatchSetupData.Teams[teamId].UnitSlots;

        for (int i = 0; i < slots.Count; i++)
        {
            int capturedUnitSlot = i;
            UnitCurrentActionsUI entry = Instantiate(unitCurrentActionsPrefab, unitListRoot);
            entry.Initialize(slots[i], actionSlotIndex => OnSlotSelected(capturedUnitSlot, actionSlotIndex));
            unitEntries.Add(entry);
        }
    }

    private void OnOfferSelected(int offerIndex)
    {
        if (offerIndex < 0 || offerIndex >= offers.Count)
        {
            return;
        }

        if (offers[offerIndex].SoldOut)
        {
            return;
        }

        pendingAction = offers[offerIndex].Action;
        pendingPrice = offers[offerIndex].Price;
        pendingOfferIndex = offerIndex;
        SetInfoText(selectReplaceText);
    }

    /// <summary>
    /// Called when the player clicks an action slot on a unit entry.
    /// Updates both the persistent roster and (if the unit is still alive)
    /// its live UnitActionController.
    /// </summary>
    private void OnSlotSelected(int unitSlotIndex, int actionSlotIndex)
    {
        if (pendingAction == null || currencyManager == null)
        {
            return;
        }

        if (teamId >= MatchSetupData.Teams.Count)
        {
            return;
        }

        List<MatchSetupData.UnitSlotData> slots = MatchSetupData.Teams[teamId].UnitSlots;

        if (unitSlotIndex < 0 || unitSlotIndex >= slots.Count)
        {
            return;
        }

        if (!currencyManager.TrySpendGold(teamId, pendingPrice))
        {
            SetInfoText(notEnoughGoldText);
            pendingAction = null;
            pendingPrice = 0;
            return;
        }

        MatchSetupData.UnitSlotData slotData = slots[unitSlotIndex];

        // Persist in roster (used on next-round respawn).
        if (actionSlotIndex >= 0 && actionSlotIndex < slotData.Actions.Length)
        {
            slotData.Actions[actionSlotIndex] = pendingAction;
        }

        // Also update the live controller if the unit is still alive.
        if (slotData.LiveUnit != null)
        {
            UnitActionController controller = slotData.LiveUnit.GetComponent<UnitActionController>();
            if (controller != null)
            {
                controller.SetSlot(actionSlotIndex, pendingAction);
            }
        }

        // Refresh the entry icon.
        if (unitSlotIndex < unitEntries.Count && unitEntries[unitSlotIndex] != null)
        {
            unitEntries[unitSlotIndex].Refresh();
        }

        // Mark the offer as sold out.
        if (pendingOfferIndex >= 0 && pendingOfferIndex < offers.Count)
        {
            ShopOffer offer = offers[pendingOfferIndex];
            offer.SoldOut = true;
            offers[pendingOfferIndex] = offer;
            if (pendingOfferIndex < actionButtons.Length && actionButtons[pendingOfferIndex] != null)
            {
                actionButtons[pendingOfferIndex].MarkSoldOut();
            }
        }

        pendingAction = null;
        pendingPrice = 0;
        pendingOfferIndex = -1;
        SetInfoText(purchaseCompleteText);
    }

    private void SetInfoText(string message)
    {
        if (infoText != null)
        {
            infoText.text = message;
        }
    }

    private List<UnitAction> GetActionPool()
    {
        var pool = new List<UnitAction>();
        UnitAction[] actions = Resources.LoadAll<UnitAction>("Actions");
        foreach (var action in actions)
        {
            if (action == null)
            {
                continue;
            }

            pool.Add(action);
        }

        return pool;
    }

    private int GetScaledRandomPrice(UnitAction action)
    {
        if (currencyManager == null || action == null)
        {
            return 0;
        }

        int baseCost = Mathf.Max(1, action.BaseCost);
        int variance = Mathf.Max(0, action.PriceVariance);
        float scale = Mathf.Max(0.25f, currencyManager.GetTeamSize(teamId) / (float)currencyManager.BaseTeamSize);

        int min = Mathf.RoundToInt((baseCost - variance) * scale);
        int max = Mathf.RoundToInt((baseCost + variance) * scale);

        if (max < min)
        {
            max = min;
        }

        return Random.Range(min, max + 1);
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}