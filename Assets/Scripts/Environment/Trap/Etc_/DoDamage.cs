using Fusion;
using UnityEngine;

public class DoDamage : NetworkBehaviour
{
    [Header("Stat")]
    [SerializeField] private float cooldownTime = 1.5f;
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float knockbackForce = 5f;
    [Networked] private TickTimer CooldownTimer { get; set; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!CooldownTimer.ExpiredOrNotRunning(Runner)) return;

        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                Vector2 knockbackDir = (other.transform.position - transform.position).normalized;
                knockbackDir.y = 1f;

                damageable.TakeDamage(damageAmount, knockbackForce, knockbackDir.normalized);

                CooldownTimer = TickTimer.CreateFromSeconds(Runner, cooldownTime);
            }
        }
    }

}
