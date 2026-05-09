using Fusion;
using UnityEngine;

public class Enemy_KnightJay : BaseMonster
{
    [Header("Stats")]
    [SerializeField] float walkSpeed = 1.3f;

    public override void Spawned()
    {
        base.Spawned();
        OnStateChanged += HandleJayAnimations;
    }

    private void HandleJayAnimations(AttackDirection state)
    {
        if (state == AttackDirection.None)
        {
            PlayAnimation("Jay_Idle_Animation");
        }
        else
        {
            PlayAnimation("Jay_Attack_Animation");
        }
    }

    private void HanleJayStateCheck()
    {
        if (!HasStateAuthority) return;

        if (currentState == MonState.Attack)
        {
            TriggerHitBox_RPC(true);
        }
        else
        {
            TriggerHitBox_RPC(false);
        }
    }

    protected override void MonsterSpecificUpdate()
    {
        if (phaseRestTimer.IsRunning && !phaseRestTimer.Expired(Runner)) return;
        if (!delayActionTimer.ExpiredOrNotRunning(Runner)) return;

        if (hasSpottedPlayer)
        {
            AttackDirection playerCurrentDir = CheckDirection(targetPosition);
            float distanceToPlayer = Vector2.Distance(transform.position, targetPosition);

            if (distanceToPlayer <= attackRadius)
            {
                rb2D.linearVelocity = Vector2.zero;

                RotateHitBoxToDirection(playerCurrentDir);
                AttackToDirection(playerCurrentDir);

                currentState = MonState.Attack;
            }
            else
            {
                Vector2 moveDir = (targetPosition - (Vector2)transform.position).normalized;
                rb2D.linearVelocity = moveDir * walkSpeed;

                currentState = MonState.Walk;
            }
        }
        else
        {
            rb2D.linearVelocity = Vector2.zero;

            currentState = MonState.Idle;
        }

        HanleJayStateCheck();
    }

    public override void Render()
    {
        base.Render();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        OnStateChanged -= HandleJayAnimations;
    }
}