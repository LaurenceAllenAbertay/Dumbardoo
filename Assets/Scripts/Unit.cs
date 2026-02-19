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

    public event Action<Unit> TurnStarted;
    public event Action<Unit> TurnEnded;
    public static event Action<Unit, Unit, int, string> DamageApplied;

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
