using UnityEngine;

[CreateAssetMenu(menuName = "StickWarfare/Actions/Gun")]
public class GunAction : UnitAction
{
    [SerializeField] private float maxRange = 30f;
    [SerializeField] private int damage = 8;
    [SerializeField] private LayerMask hitMask = ~0;

    protected override void Execute(Unit unit, TurnManager turnManager)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRange, hitMask, QueryTriggerInteraction.Ignore))
        {
            Unit target = hit.collider.GetComponentInParent<Unit>();
            if (target != null && target.IsAlive)
            {
                target.ApplyDamage(damage, unit, ActionName);
                return;
            }
        }

        Debug.Log($"{unit.name} used {ActionName} but hit nothing.");
    }
}
