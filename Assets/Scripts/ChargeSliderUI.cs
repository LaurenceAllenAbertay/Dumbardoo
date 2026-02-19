using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a UI Slider to visualise grenade throw-charge progress.
/// • The slider is visible only while a grenade is being charged.
/// • Its value mirrors the same PingPong ramp used internally by
///   UnitActionController, so "release when bar is full" is intuitive.
/// </summary>
public class ChargeSliderUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;

    [Tooltip("The slider that shows charge progress (0 = min force, 1 = max force).")]
    [SerializeField] private Slider chargeSlider;

    [Tooltip("Root object to show/hide. If empty, the Slider's own GameObject is used.")]
    [SerializeField] private GameObject sliderRoot;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private UnitActionController currentController;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }

        if (chargeSlider != null)
        {
            chargeSlider.minValue = 0f;
            chargeSlider.maxValue = 1f;
            chargeSlider.interactable = false;
        }
    }

    private void OnEnable()
    {
        if (turnManager != null)
        {
            turnManager.TurnStarted += OnTurnStarted;
            turnManager.PhaseChanged += OnPhaseChanged;
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
    }

    private void Update()
    {
        if (currentController == null)
        {
            SetVisible(false);
            return;
        }

        bool charging = currentController.IsCharging;
        SetVisible(charging);

        if (charging && chargeSlider != null)
        {
            chargeSlider.value = currentController.ChargeNormalized;
        }
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnTurnStarted(Unit unit)
    {
        currentController = unit != null ? unit.GetComponent<UnitActionController>() : null;
        SetVisible(false);
    }

    private void OnPhaseChanged(TurnManager.TurnPhase phase)
    {
        // Hide whenever we leave the action phase.
        if (phase != TurnManager.TurnPhase.Action)
        {
            SetVisible(false);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetVisible(bool visible)
    {
        GameObject root = sliderRoot != null
            ? sliderRoot
            : chargeSlider != null ? chargeSlider.gameObject : null;

        if (root != null && root.activeSelf != visible)
        {
            root.SetActive(visible);
        }
    }
}