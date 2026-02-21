using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TShirtProjectile : MonoBehaviour
{
    private Unit sourceUnit;
    private string actionName;
    private int damage;
    private float knockbackForce;
    private float knockbackUpForce;
    private LayerMask hitMask;
    private bool hasHit;
    private ThirdPersonCameraController cameraController;
    private int cameraFollowId = -1;

    public void Initialize(Unit source, string action, int dmg, float force, float upForce, LayerMask mask)
    {
        sourceUnit = source;
        actionName = action;
        damage = dmg;
        knockbackForce = force;
        knockbackUpForce = upForce;
        hitMask = mask;
    }

    public void SetCameraFollow(ThirdPersonCameraController controller, int followId)
    {
        cameraController = controller;
        cameraFollowId = followId;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit)
        {
            return;
        }

        hasHit = true;

        if (cameraController != null && cameraFollowId >= 0)
        {
            cameraController.EndTemporaryFollow(cameraFollowId, 1f);
        }

        int colliderLayer = 1 << collision.gameObject.layer;
        if ((hitMask.value & colliderLayer) != 0)
        {
            Unit target = collision.collider.GetComponentInParent<Unit>();
            if (target != null && target.IsAlive)
            {
                target.ApplyDamage(damage, sourceUnit, actionName);

                Rigidbody body = collision.rigidbody ?? target.GetComponent<Rigidbody>();
                if (body != null)
                {
                    Vector3 direction = (target.transform.position - transform.position).normalized;
                    direction.y = 0f;
                    if (direction.sqrMagnitude < 0.001f)
                    {
                        direction = transform.forward;
                    }
                    body.AddForce(direction * knockbackForce + Vector3.up * knockbackUpForce, ForceMode.VelocityChange);
                }
            }
        }

        Destroy(gameObject);
    }
}