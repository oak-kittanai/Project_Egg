using Fusion;
using UnityEngine;

public class BearTrapScript : NetworkBehaviour
{
    [Header("Trap Settings")]
    [SerializeField] private float cooldownTime = 2.4f;
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
            IsTriggered = true;
            CooldownTimer = TickTimer.CreateFromSeconds(Runner, cooldownTime);

            if (other.TryGetComponent<MovementCharacter>(out var player))
            {
                float pushDirectionX = Mathf.Sign(other.transform.position.x - transform.position.x);
                Vector2 knockbackDirection = new Vector2(pushDirectionX, 1f).normalized;

                player.TakeDamage(damageAmount, knockbackForce, knockbackDirection);

                Debug.Log($"Do damage To {player.name}: -{damageAmount} hp");
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