using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a UI Slider to visualise grenade throw-charge progress or jetpack fuel.
/// • The slider is visible only while a grenade is being charged or the jetpack is active.
/// • Its value mirrors the same PingPong ramp used internally by
///   UnitActionController, so "release when bar is full" is intuitive.
/// </summary>
public class PowerSliderUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;

    [Tooltip("The slider that shows charge progress (0 = min force, 1 = max force) or jetpack fuel (1 = full, 0 = empty).")]
    [SerializeField] private Slider powerSlider;

    [Tooltip("Root object to show/hide. If empty, the Slider's own GameObject is used.")]
    [SerializeField] private GameObject sliderRoot;

    // Runtime state

    private UnitActionController currentController;

    // Unity lifecycle

    private void Awake()
    {
        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }

        if (powerSlider != null)
        {
            powerSlider.minValue = 0f;
            powerSlider.maxValue = 1f;
            powerSlider.interactable = false;
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

        if (currentController.IsCharging)
        {
            SetVisible(true);
            if (powerSlider != null)
                powerSlider.value = currentController.ChargeNormalized;
            return;
        }

        JetpackActionController jetpack = currentController.GetComponent<JetpackActionController>();
        if (jetpack != null && jetpack.IsJetpackActive)
        {
            SetVisible(true);
            if (powerSlider != null)
                powerSlider.value = jetpack.FuelNormalized;
            return;
        }

        SetVisible(false);
    }

    // Event handlers

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

    // Helpers

    private void SetVisible(bool visible)
    {
        GameObject root = sliderRoot != null
            ? sliderRoot
            : powerSlider != null ? powerSlider.gameObject : null;

        if (root != null && root.activeSelf != visible)
        {
            root.SetActive(visible);
        }
    }
}