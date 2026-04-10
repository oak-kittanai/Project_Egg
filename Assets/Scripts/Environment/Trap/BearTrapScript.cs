using Fusion;
using UnityEngine;

public class BearTrapScript : NetworkBehaviour
{
    [Header("Trap Settings")]
    [SerializeField] private float cooldownTime = 2.4f;
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
        if (other.CompareTag("Player"))
        {
            MovementCharacter[] allCharacterMovement = other.GetComponents<MovementCharacter>();

            foreach (var character in allCharacterMovement)
            {
                if (character.enabled)
                {
                    if (character.HasInputAuthority && !IsTriggered)
                    {
                        localTriggerPredict = true;
                        localPredictTimer = Time.time + cooldownTime;
                    }

                    if (HasStateAuthority)
                    {
                        if (!CooldownTimer.ExpiredOrNotRunning(Runner)) return;

                        IsTriggered = true;
                        CooldownTimer = TickTimer.CreateFromSeconds(Runner, cooldownTime);

                        if (doDamageColl2D != null) doDamageColl2D.enabled = false;
                    }
                }
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