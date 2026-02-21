using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Horizontal strip of round-result slots. On start it instantiates one
/// RoundCounter prefab per round and sets each to the pending sprite. Each
/// time a team wins a round the leftmost pending slot's image is swapped to
/// that team's win sprite.
/// </summary>
public class RoundsDisplayUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;

    [Tooltip("The HorizontalLayoutGroup transform that will hold the instantiated slots.")]
    [SerializeField] private Transform slotsRoot;

    [Header("Slot Prefab")]
    [SerializeField] private GameObject roundCounterPrefab;

    [Header("Sprites")]
    [SerializeField] private Sprite pendingSprite;

    [Tooltip("One sprite per team, indexed by TeamId. Index 0 = Team 1, index 1 = Team 2, etc.")]
    [SerializeField] private Sprite[] teamWinSprites = new Sprite[2];

    private Image[] slots;
    private int filledCount;

    private void Awake()
    {
        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>();
    }

    private void OnEnable()
    {
        if (turnManager != null)
            turnManager.TeamWon += OnTeamWon;
    }

    private void OnDisable()
    {
        if (turnManager != null)
            turnManager.TeamWon -= OnTeamWon;
    }

    private void Start()
    {
        BuildSlots();
        RestoreCompletedRounds();
    }

    /// <summary>
    /// Destroys any existing slot children and instantiates one prefab per round.
    /// </summary>
    private void BuildSlots()
    {
        if (slotsRoot == null || roundCounterPrefab == null) return;

        for (int i = slotsRoot.childCount - 1; i >= 0; i--)
            Destroy(slotsRoot.GetChild(i).gameObject);

        int total = MatchSetupData.TotalRounds;
        slots = new Image[total];
        filledCount = 0;

        for (int i = 0; i < total; i++)
        {
            GameObject go = Instantiate(roundCounterPrefab, slotsRoot);
            Image img = go.GetComponent<Image>();
            img.sprite = pendingSprite;
            slots[i] = img;
        }
    }

    /// <summary>
    /// Fills in slots for any rounds already completed (e.g. after a respawn
    /// mid-session when RoundResults already has entries).
    /// </summary>
    private void RestoreCompletedRounds()
    {
        foreach (int winnerId in MatchSetupData.RoundResults)
            FillNextSlot(winnerId);
    }

    private void OnTeamWon(int winningTeamId)
    {
        FillNextSlot(winningTeamId);
    }

    private void FillNextSlot(int teamId)
    {
        if (slots == null || filledCount >= slots.Length) return;

        Sprite winSprite = teamId >= 0 && teamId < teamWinSprites.Length
            ? teamWinSprites[teamId]
            : null;

        slots[filledCount].sprite = winSprite != null ? winSprite : pendingSprite;
        filledCount++;
    }
}