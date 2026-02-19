using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TurnManager : MonoBehaviour
{
    public enum TurnPhase
    {
        Starting,
        Movement,
        Action,
        TurnEnd,
        NextUnit
    }

    [SerializeField] private InputActionReference endTurnAction;
    [SerializeField] private bool randomizeTurnOrder = true;
    [SerializeField] private bool autoStart = true;
    [Header("Phase UI")]
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private string movementPhaseText = "Movement Phase";
    [SerializeField] private string actionPhaseText = "Action Phase";
    [SerializeField] private string turnEndText = "Turn End";
    [SerializeField] private string nextUnitText = "Next Unit";
    [SerializeField] private bool waitForCameraTransition = true;
    [Header("Camera")]
    [SerializeField] private ThirdPersonCameraController cameraController;
    [SerializeField] private bool blockEndTurnUntilCameraReturns = true;

    private readonly List<Unit> turnOrder = new List<Unit>();
    private int currentIndex = -1;
    private bool started;
    private Vector3 currentTurnStartPosition;
    private bool pendingMovementStart;

    public Unit CurrentUnit { get; private set; }
    public TurnPhase Phase { get; private set; } = TurnPhase.Starting;
    public Vector3 CurrentTurnStartPosition => currentTurnStartPosition;
    public event Action<Unit> TurnStarted;
    public event Action<Unit> TurnEnded;
    public event Action<TurnPhase> PhaseChanged;
    public event Action<int> TeamWon;

    private void OnEnable()
    {
        if (endTurnAction != null && endTurnAction.action != null)
        {
            endTurnAction.action.performed += OnEndTurnPerformed;
            endTurnAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (endTurnAction != null && endTurnAction.action != null)
        {
            endTurnAction.action.performed -= OnEndTurnPerformed;
            endTurnAction.action.Disable();
        }
    }

    private void Start()
    {
        if (cameraController == null && Camera.main != null)
        {
            cameraController = Camera.main.GetComponent<ThirdPersonCameraController>();
        }

        if (phaseText == null)
        {
            phaseText = FindPhaseTextInScene();
        }

        UpdatePhaseTextForCurrentPhase();
        BuildTurnOrder();
        if (autoStart)
        {
            StartTurns();
        }
    }

    public void BuildTurnOrder()
    {
        turnOrder.Clear();
        var units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (var unit in units)
        {
            if (unit != null)
            {
                turnOrder.Add(unit);
            }
        }

        if (randomizeTurnOrder)
        {
            Shuffle(turnOrder);
        }

        currentIndex = -1;
        started = false;
        CurrentUnit = null;
    }

    public void StartTurns()
    {
        if (turnOrder.Count == 0)
        {
            Debug.LogWarning("TurnManager: No units found in scene.");
            return;
        }

        started = true;
        AdvanceToNextUnit(false);
    }

    public void EndCurrentTurn()
    {
        if (!started)
        {
            return;
        }

        // Only block for the camera when the unit is still alive.
        // A dead unit (e.g. killed mid-jetpack by a KillBox) must always be
        // able to advance the turn; otherwise the game can freeze permanently
        // if the camera happens to be in a temporary-follow state when
        // DeathSequence calls here.
        bool currentUnitAlive = CurrentUnit != null && CurrentUnit.IsAlive;
        if (currentUnitAlive
            && blockEndTurnUntilCameraReturns
            && cameraController != null
            && cameraController.IsTemporaryFollowActive)
        {
            return;
        }

        if (CurrentUnit == null || !CurrentUnit.IsAlive)
        {
            if (CurrentUnit != null)
            {
                if (CurrentUnit.IsTurnActive)
                {
                    CurrentUnit.EndTurn();
                }

                TurnEnded?.Invoke(CurrentUnit);
            }

            AdvanceToNextUnit(true);
            return;
        }

        if (Phase == TurnPhase.Movement)
        {
            EndMovementPhase();
            return;
        }

        if (CurrentUnit != null)
        {
            CurrentUnit.EndTurn();
            TurnEnded?.Invoke(CurrentUnit);
        }

        AdvanceToNextUnit(true);
    }

    public void EndMovementPhase()
    {
        if (Phase != TurnPhase.Movement)
        {
            return;
        }

        SetPhase(TurnPhase.Action);
    }

    public void NotifyActionEnded(Unit unit)
    {
        if (!started || unit == null || unit != CurrentUnit)
        {
            return;
        }

        if (Phase == TurnPhase.Action)
        {
            SetPhase(TurnPhase.TurnEnd);
        }
    }

    public void NotifyTurnTransitionComplete()
    {
        if (!pendingMovementStart)
        {
            return;
        }

        pendingMovementStart = false;

        if (CurrentUnit != null && (Phase == TurnPhase.NextUnit || Phase == TurnPhase.Starting))
        {
            SetPhase(TurnPhase.Movement);
        }
    }

    private void AdvanceToNextUnit(bool showNextUnitPhase)
    {
        if (CheckForWinner(out var winningTeamId))
        {
            TeamWon?.Invoke(winningTeamId);
            Debug.Log($"Team {winningTeamId} wins.");
            return;
        }

        for (int i = 0; i < turnOrder.Count; i++)
        {
            currentIndex = (currentIndex + 1) % turnOrder.Count;
            var candidate = turnOrder[currentIndex];
            if (candidate != null && candidate.IsAlive)
            {
                CurrentUnit = candidate;
                CurrentUnit.BeginTurn();
                currentTurnStartPosition = CurrentUnit.transform.position;
                pendingMovementStart = true;
                SetPhase(showNextUnitPhase ? TurnPhase.NextUnit : TurnPhase.Starting);
                TurnStarted?.Invoke(CurrentUnit);

                if (!waitForCameraTransition)
                {
                    NotifyTurnTransitionComplete();
                }

                return;
            }
        }

        Debug.LogWarning("TurnManager: No alive units found.");
    }

    private bool CheckForWinner(out int winningTeamId)
    {
        winningTeamId = -1;
        var aliveTeams = new HashSet<int>();
        foreach (var unit in turnOrder)
        {
            if (unit != null && unit.IsAlive)
            {
                aliveTeams.Add(unit.TeamId);
                if (aliveTeams.Count > 1)
                {
                    return false;
                }
            }
        }

        if (aliveTeams.Count == 1)
        {
            foreach (var id in aliveTeams)
            {
                winningTeamId = id;
                break;
            }
            return true;
        }

        return false;
    }

    private void OnEndTurnPerformed(InputAction.CallbackContext context)
    {
        EndCurrentTurn();
    }

    private static void Shuffle(List<Unit> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void UpdatePhaseTextForCurrentPhase()
    {
        if (phaseText == null)
        {
            return;
        }

        phaseText.text = GetPhaseText(Phase);
    }

    private string GetPhaseText(TurnPhase phase)
    {
        switch (phase)
        {
            case TurnPhase.Movement:
                return movementPhaseText;
            case TurnPhase.Action:
                return actionPhaseText;
            case TurnPhase.TurnEnd:
                return turnEndText;
            case TurnPhase.NextUnit:
                return nextUnitText;
            default:
                return string.Empty;
        }
    }

    private void SetPhase(TurnPhase phase)
    {
        Phase = phase;
        PhaseChanged?.Invoke(Phase);
        UpdatePhaseTextForCurrentPhase();
    }

    private static TMP_Text FindPhaseTextInScene()
    {
        var texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var text in texts)
        {
            if (text != null && text.gameObject.name == "CurrentPhase")
            {
                return text;
            }
        }

        return null;
    }
}