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

    public void SetTeamId(int id)
    {
        teamId = id;
    }

    public void SetUnitName(string newName)
    {
        if (!string.IsNullOrWhiteSpace(newName))
        {
            name = newName;
        }
    }

    public void ApplyDamage(int amount)
    {
        ApplyDamage(amount, null, null);
    }

    public void ApplyDamage(int amount, Unit source, string actionName)
    {
        if (!isAlive || amount <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);
        string sourceName = source != null ? source.name : "Unknown";
        string actionLabel = string.IsNullOrWhiteSpace(actionName) ? "UnknownAction" : actionName;
        Debug.Log($"{sourceName} used {actionLabel} on {name} for {amount} damage.");

        if (currentHealth == 0)
        {
            SetAlive(false);
        }
    }
}
