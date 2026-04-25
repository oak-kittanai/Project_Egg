using Fusion;
using UnityEngine;

public class BearTrapScript : NetworkBehaviour
{
    [Header("Trap Settings")]
    [SerializeField] private float delayBeforeSnap = 0.5f;
    [SerializeField] private float cooldownTime = 2.4f;
    [SerializeField] private ParticleSystem damageParticles;

    [Networked] private TickTimer CooldownTimer { get; set; }
    [Networked] private TickTimer DelayTimer { get; set; }
    [Networked] private NetworkBool IsTriggered { get; set; }
    [Networked] private NetworkBool HasPlayedFX { get; set; }

    [SerializeField] Collider2D doDamageColl2D;
    [SerializeField] private Animator trapAnimator;

    private void Awake()
    {
        if (doDamageColl2D != null) doDamageColl2D.enabled = false;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!HasStateAuthority) return;

        if (!IsTriggered && other.CompareTag("Player") && CooldownTimer.ExpiredOrNotRunning(Runner))
        {
            IsTriggered = true;
            HasPlayedFX = false;
            DelayTimer = TickTimer.CreateFromSeconds(Runner, delayBeforeSnap);
            CooldownTimer = TickTimer.CreateFromSeconds(Runner, cooldownTime);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (IsTriggered && !HasPlayedFX && DelayTimer.Expired(Runner))
        {
            HasPlayedFX = true;

            if (doDamageColl2D != null)
            {
                doDamageColl2D.enabled = true;

                ContactFilter2D filter = new ContactFilter2D();
                filter.SetLayerMask(LayerMask.GetMask("Player"));

                Collider2D[] results = new Collider2D[5];
                int hitCount = doDamageColl2D.OverlapCollider(filter, results);

                if (hitCount > 0)
                {
                    RPC_PlaySnapEffect();
                }
            }
        }

        // รีเซ็ต
        if (IsTriggered && CooldownTimer.Expired(Runner))
        {
            IsTriggered = false;
            HasPlayedFX = false;
            DelayTimer = TickTimer.None;

            if (doDamageColl2D != null)
            {
                doDamageColl2D.enabled = false;
            }

            RPC_ResetTrapVisuals();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlaySnapEffect()
    {
        if (damageParticles != null)
        {
            Instantiate(damageParticles, transform.position, Quaternion.identity);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ResetTrapVisuals()
    {
            trapAnimator.SetBool("Trigger", false);
    }

    public override void Render()
    {
            trapAnimator.SetBool("Trigger", IsTriggered);
    }
}