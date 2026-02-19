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

        if (!other.TryGetComponent(out Unit unit))
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
