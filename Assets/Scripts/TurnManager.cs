using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TurnManager : MonoBehaviour
{
    [SerializeField] private InputActionReference endTurnAction;
    [SerializeField] private bool randomizeTurnOrder = true;
    [SerializeField] private bool autoStart = true;

    private readonly List<Unit> turnOrder = new List<Unit>();
    private int currentIndex = -1;
    private bool started;

    public Unit CurrentUnit { get; private set; }
    public event Action<Unit> TurnStarted;
    public event Action<Unit> TurnEnded;
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
        AdvanceToNextUnit();
    }

    public void EndCurrentTurn()
    {
        if (!started)
        {
            return;
        }

        if (CurrentUnit != null)
        {
            CurrentUnit.EndTurn();
            TurnEnded?.Invoke(CurrentUnit);
        }

        AdvanceToNextUnit();
    }

    private void AdvanceToNextUnit()
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
                TurnStarted?.Invoke(CurrentUnit);
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
}
