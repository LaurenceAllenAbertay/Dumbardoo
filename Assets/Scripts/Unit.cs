using System;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [SerializeField] private int teamId = 0;
    [SerializeField] private bool isAlive = true;
    [SerializeField] private float moveRange = 6f;

    public int TeamId => teamId;
    public bool IsAlive => isAlive;
    public float MoveRange => moveRange;
    public bool IsTurnActive { get; private set; }

    public event Action<Unit> TurnStarted;
    public event Action<Unit> TurnEnded;

    public void BeginTurn()
    {
        if (!isAlive)
        {
            return;
        }

        IsTurnActive = true;
        TurnStarted?.Invoke(this);
    }

    public void EndTurn()
    {
        if (!IsTurnActive)
        {
            return;
        }

        IsTurnActive = false;
        TurnEnded?.Invoke(this);
    }

    public void SetAlive(bool alive)
    {
        isAlive = alive;
        if (!isAlive && IsTurnActive)
        {
            EndTurn();
        }
    }
}
