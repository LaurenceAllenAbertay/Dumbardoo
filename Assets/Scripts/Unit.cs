using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class Unit : MonoBehaviour
{
    [SerializeField] private int teamId = 0;
    [SerializeField] private bool isAlive = true;
    [SerializeField] private float moveRange = 6f;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;
    [SerializeField] private GameObject deathVfxPrefab;
    [SerializeField] private float deathEndDelay = 2f;

    public int TeamId => teamId;
    public bool IsAlive => isAlive;
    public float MoveRange => moveRange;
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsTurnActive { get; private set; }

    /// <summary>
    /// The last enemy unit that dealt damage to this unit.
    /// Used by KillBox to attribute environmental kills to the correct attacker.
    /// </summary>
    public Unit LastAttacker { get; private set; }

    /// <summary>
    /// The action name used in the last enemy hit. Passed through to KillBox attribution.
    /// </summary>
    public string LastAttackerAction { get; private set; }

    public event Action<Unit> TurnStarted;
    public event Action<Unit> TurnEnded;
    public static event Action<Unit, Unit, int, string> DamageApplied;

    /// <summary>
    /// Fired immediately when a unit's health reaches zero, before the death
    /// sequence starts. Listeners (e.g. MatchSetupSpawner) can save the unit's
    /// state while the GameObject is still fully intact.
    /// </summary>
    public static event Action<Unit> UnitDied;

    private bool deathSequenceStarted;
    private float deathRandomTorqueImpulse = 1f;
    private TurnManager turnManager;

    private void Awake()
    {
        turnManager = FindFirstObjectByType<TurnManager>();
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

        // Clear attacker tracking at the start of this unit's own turn.
        // Knockback from the previous turn has already resolved; if the unit
        // now walks off an edge under their own control no one should get credit.
        LastAttacker = null;
        LastAttackerAction = null;

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

        if (!isAlive)
        {
            UnitDied?.Invoke(this);
            StartDeathSequence();
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

        // Track the last enemy that dealt damage so KillBox can attribute the kill.
        if (source != null && source != this && source.TeamId != teamId)
        {
            LastAttacker = source;
            LastAttackerAction = string.IsNullOrWhiteSpace(actionName) ? "UnknownAction" : actionName;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);
        string sourceName = source != null ? source.name : "Unknown";
        string actionLabel = string.IsNullOrWhiteSpace(actionName) ? "UnknownAction" : actionName;
        Debug.Log($"{sourceName} used {actionLabel} on {name} for {amount} damage.");
        DamageApplied?.Invoke(source, this, amount, actionName);

        if (currentHealth == 0)
        {
            SetAlive(false);
        }
    }

    private void StartDeathSequence()
    {
        if (deathSequenceStarted)
        {
            return;
        }

        deathSequenceStarted = true;

        if (TryGetComponent(out Rigidbody body))
        {
            body.constraints &= ~RigidbodyConstraints.FreezeRotationX;
            body.constraints &= ~RigidbodyConstraints.FreezeRotationZ;

            if (deathRandomTorqueImpulse > 0f)
            {
                float x = UnityEngine.Random.Range(-1f, 1f);
                float z = UnityEngine.Random.Range(-1f, 1f);
                Vector3 torque = new Vector3(x, 0f, z);
                if (torque.sqrMagnitude > 0.0001f)
                {
                    torque.Normalize();
                }

                body.AddTorque(torque * deathRandomTorqueImpulse, ForceMode.Impulse);
            }
        }

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        if (deathEndDelay > 0f)
        {
            yield return new WaitForSeconds(deathEndDelay);
        }

        if (turnManager != null && turnManager.CurrentUnit == this)
        {
            turnManager.EndCurrentTurn();
        }

        if (deathVfxPrefab != null)
        {
            Instantiate(deathVfxPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}