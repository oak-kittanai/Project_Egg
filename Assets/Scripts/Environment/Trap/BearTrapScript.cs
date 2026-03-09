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
            IsTriggered = true;

            CooldownTimer = TickTimer.CreateFromSeconds(Runner, cooldownTime);
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
