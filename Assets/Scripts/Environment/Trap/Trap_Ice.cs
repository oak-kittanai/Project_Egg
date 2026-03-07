using Fusion;
using System.Collections;
using UnityEngine;

public class Trap_Ice : NetworkBehaviour
{
    [Header("Trap Settings")]
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float knockbackForce = 5f;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!HasStateAuthority) return;

        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                Vector2 knockbackDir = (other.transform.position - transform.position).normalized;
                knockbackDir.y = 1f;

                damageable.TakeDamage(damageAmount, knockbackForce, knockbackDir.normalized);
            }
        }
    }
}