using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a UI Slider to visualise how much movement range the current unit
/// has remaining this turn.
///
/// • Visible only during the Movement phase.
/// • Full bar  = no distance travelled yet  (value == MoveRange).
/// • Empty bar = unit has reached the edge of their allowed range (value == 0).
///
/// Wire-up:
///   1. Add this component to any persistent HUD GameObject.
///   2. Assign the TurnManager and a UI Slider in the inspector.
///   3. Optionally assign a SliderRoot override (otherwise the Slider's own
///      GameObject is shown / hidden).
/// </summary>
public class MovementRangeSliderUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;

    [Tooltip("The slider that shows remaining movement (0 = none left, 1 = full range).")]
    [SerializeField] private Slider movementSlider;

    [Tooltip("Root object to show/hide. If empty, the Slider's own GameObject is used.")]
    [SerializeField] private GameObject sliderRoot;

    // Runtime state

    private Unit currentUnit;

    // ── Unity lifecycle ────────────────────────────────────────────────────────

    private void Awake()
    {
        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }

        if (movementSlider != null)
        {
            movementSlider.minValue = 0f;
            movementSlider.maxValue = 1f;
            movementSlider.interactable = false;
        }
    }

    private void OnEnable()
    {
        if (turnManager != null)
        {
            turnManager.TurnStarted  += OnTurnStarted;
            turnManager.PhaseChanged += OnPhaseChanged;
        }

        SetVisible(false);
    }

    private void OnDisable()
    {
        if (turnManager != null)
        {
            turnManager.TurnStarted  -= OnTurnStarted;
            turnManager.PhaseChanged -= OnPhaseChanged;
        }
    }

    private void Update()
    {
        if (turnManager == null || turnManager.Phase != TurnManager.TurnPhase.Movement)
        {
            return;
        }

        if (currentUnit == null || !currentUnit.IsAlive)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        UpdateSlider();
    }

    // ── Event handlers ─────────────────────────────────────────────────────────

    private void OnTurnStarted(Unit unit)
    {
        currentUnit = unit;
        // Snap slider to full at the start of every new turn.
        SetSliderValue(1f);
        SetVisible(false); // Hidden until the Movement phase begins.
    }

    private void OnPhaseChanged(TurnManager.TurnPhase phase)
    {
        bool isMovementPhase = phase == TurnManager.TurnPhase.Movement;
        SetVisible(isMovementPhase);

        if (isMovementPhase)
        {
            // Reset to full whenever the movement phase starts.
            SetSliderValue(1f);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void UpdateSlider()
    {
        float moveRange = currentUnit.MoveRange;
        if (moveRange <= 0f)
        {
            SetSliderValue(0f);
            return;
        }

        Vector3 startPosition = turnManager.CurrentTurnStartPosition;
        Vector3 currentPosition = currentUnit.transform.position;

        // Ignore vertical displacement — movement range is measured on the XZ plane.
        Vector3 offset = currentPosition - startPosition;
        offset.y = 0f;
        float distanceTravelled = offset.magnitude;

        float remaining = Mathf.Clamp01(1f - (distanceTravelled / moveRange));
        SetSliderValue(remaining);
    }

    private void SetSliderValue(float normalizedValue)
    {
        if (movementSlider != null)
        {
            movementSlider.value = normalizedValue;
        }
    }

    private void SetVisible(bool visible)
    {
        GameObject root = sliderRoot != null
            ? sliderRoot
            : movementSlider != null ? movementSlider.gameObject : null;

        if (root != null && root.activeSelf != visible)
        {
            root.SetActive(visible);
        }
    }
}