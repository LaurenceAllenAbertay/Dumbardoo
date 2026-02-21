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

    [Tooltip("The visual model root to hide immediately on death. " +
             "If left empty, all Renderer components on this GameObject and its " +
             "children are disabled instead.")]
    [SerializeField] private GameObject modelRoot;

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
    private bool deathVfxSpawned;
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

    public void Heal(int amount)
    {
        if (!isAlive || amount <= 0)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
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

    /// <summary>
    /// Spawns the death VFX prefab immediately and launches each Rigidbody part
    /// outward from <paramref name="origin"/> using <paramref name="force"/>.
    /// Marks the VFX as already spawned so StartDeathSequence does not duplicate it.
    /// </summary>
    public void SpawnAndLaunchDeathVfx(Vector3 origin, float force)
    {
        if (deathVfxPrefab == null || deathVfxSpawned)
        {
            return;
        }

        deathVfxSpawned = true;
        GameObject vfx = Instantiate(deathVfxPrefab, transform.position, Quaternion.identity);
        foreach (Rigidbody part in vfx.GetComponentsInChildren<Rigidbody>())
        {
            Vector3 dir = part.position - origin;
            Vector3 launchDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : UnityEngine.Random.insideUnitSphere.normalized;
            launchDir = (launchDir + UnityEngine.Random.insideUnitSphere * 0.3f).normalized;
            part.linearVelocity = launchDir * force;
            part.angularVelocity = UnityEngine.Random.insideUnitSphere * force;
        }
    }

    private void StartDeathSequence()
    {
        if (deathSequenceStarted)
        {
            return;
        }

        deathSequenceStarted = true;

        // Spawn VFX immediately so it plays right as the unit dies.
        if (deathVfxPrefab != null && !deathVfxSpawned)
        {
            deathVfxSpawned = true;
            GameObject vfx = Instantiate(deathVfxPrefab, transform.position, Quaternion.identity);

            // Inherit the unit's current velocity so the VFX parts continue
            // moving in the same direction the unit was travelling at death.
            Vector3 deathVelocity = TryGetComponent(out Rigidbody unitBody)
                ? unitBody.linearVelocity
                : Vector3.zero;

            foreach (Rigidbody part in vfx.GetComponentsInChildren<Rigidbody>())
            {
                Vector3 randomBoost = new Vector3(
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f));
                part.linearVelocity += deathVelocity + randomBoost;
            }
        }

        // Hide the visual model immediately.
        HideModel();

        StartCoroutine(DeathSequence());
    }

    /// <summary>
    /// Hides the unit's visual representation immediately on death.
    /// Uses <see cref="modelRoot"/> if assigned; otherwise disables every
    /// Renderer found on this GameObject and its children.
    /// </summary>
    private void HideModel()
    {
        if (modelRoot != null)
        {
            modelRoot.SetActive(false);
        }
        else
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = false;
            }
        }
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

        Destroy(gameObject);
    }
}