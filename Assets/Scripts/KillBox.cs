using UnityEngine;

[RequireComponent(typeof(Collider))]
public class KillBox : MonoBehaviour
{
    private Collider triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null && !triggerCollider.isTrigger)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
        {
            return;
        }

        // TryGetComponent only checks the exact GameObject the collider is on.
        // Units frequently have their physics colliders on child objects, so we
        // must walk up the hierarchy with GetComponentInParent.
        Unit unit = other.GetComponentInParent<Unit>();
        if (unit == null)
        {
            return;
        }

        if (!unit.IsAlive)
        {
            return;
        }

        unit.ApplyDamage(unit.CurrentHealth, null, "KillBox");
    }
}