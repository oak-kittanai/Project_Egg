using Fusion;
using UnityEngine;
using static Unity.Collections.Unicode;

public class BearTrapScript : NetworkBehaviour
{
    [Header("Trap Settings")]
    [SerializeField] private float cooldownTime = 1.5f;
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float knockbackForce = 5f;
    [Networked] private TickTimer CooldownTimer { get; set; }

    [Header("Visuals")]
    [SerializeField] private Animator trapAnimator;
    [Networked] private NetworkBool IsTriggered { get; set; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!HasStateAuthority) return;

        if (!CooldownTimer.ExpiredOrNotRunning(Runner)) return;

        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                Vector2 knockbackDir = (other.transform.position - transform.position).normalized;
                knockbackDir.y = 1f;

                damageable.TakeDamage(damageAmount, knockbackForce, knockbackDir.normalized);

                CooldownTimer = TickTimer.CreateFromSeconds(Runner, cooldownTime);

                IsTriggered = true;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (IsTriggered && CooldownTimer.Expired(Runner))
        {
            IsTriggered = false;
        }
    }

    public override void Render()
    {
        if (trapAnimator != null)
        {
            trapAnimator.SetBool("Trigger", IsTriggered);
        }
    }
}
