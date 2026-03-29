using Fusion;
using UnityEngine;

public class BearTrapScript : NetworkBehaviour
{
    [Header("Trap Settings")]
    [SerializeField] private float cooldownTime = 2.4f;
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float knockbackForce = 5f;
    [Networked] private TickTimer CooldownTimer { get; set; }

    [SerializeField] Collider2D doDamageColl2D;

    [Header("Visuals")]
    [SerializeField] private Animator trapAnimator;
    [Networked] private NetworkBool IsTriggered { get; set; }

    private bool localTriggerPredict;
    private float localPredictTimer;

    private void Awake()
    {
        if (doDamageColl2D != null) doDamageColl2D.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {

        if (other.CompareTag("Player") && other.TryGetComponent<MovementCharacter>(out var player))
        {
            if (player.HasInputAuthority && !IsTriggered)
            {
                localTriggerPredict = true;
                localPredictTimer = Time.time + cooldownTime;
            }

            if (HasStateAuthority)
            {
                if (!CooldownTimer.ExpiredOrNotRunning(Runner)) return;

                IsTriggered = true;
                CooldownTimer = TickTimer.CreateFromSeconds(Runner, cooldownTime);

                if (doDamageColl2D != null)
                {
                    doDamageColl2D.enabled = true;
                }

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

            if (doDamageColl2D != null) doDamageColl2D.enabled = false;
        }
    }

    public override void Render()
    {
        if (IsTriggered || Time.time > localPredictTimer)
        {
            localTriggerPredict = false;
        }

        if (trapAnimator != null)
        {
            trapAnimator.SetBool("Trigger", IsTriggered || localTriggerPredict);
        }
    }
}