using System;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [SerializeField] private int teamId = 0;
    [SerializeField] private bool isAlive = true;
    [SerializeField] private float moveRange = 6f;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;

    public int TeamId => teamId;
    public bool IsAlive => isAlive;
    public float MoveRange => moveRange;
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsTurnActive { get; private set; }

    public event Action<Unit> TurnStarted;
    public event Action<Unit> TurnEnded;

    private void Awake()
    {
        if (currentHealth <= 0)
        {
            currentHealth = maxHealth;
        }
    }

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

    public void ApplyDamage(int amount)
    {
        if (!isAlive || amount <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (currentHealth == 0)
        {
            SetAlive(false);
        }
    }
}
