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

    protected override void MonsterSpecificUpdate()
    {
        if (phaseRestTimer.IsRunning && !phaseRestTimer.Expired(Runner))
        {
            rb2D.linearVelocity = Vector2.zero;
            return;
        }
        if (!delayActionTimer.ExpiredOrNotRunning(Runner))
        {
            rb2D.linearVelocity = Vector2.zero;
            return;
        }

        if (hasSpottedPlayer)
        {
            AttackDirection playerCurrentDir = CheckDirection(targetPosition);

            float distanceToPlayer = Vector2.Distance(transform.position, targetPosition);

            if (distanceToPlayer <= (attackRadius - 0.2f))
            {
                rb2D.linearVelocity = Vector2.zero;
                AttackToDirection(playerCurrentDir);
            }
            else
            {
                Vector2 moveDir = (targetPosition - (Vector2)transform.position).normalized;
                rb2D.linearVelocity = moveDir * walkSpeed;

                if (moveDir.x < 0)
                {
                    spriteRenderer.flipX = true;
                }
                else spriteRenderer.flipX = false;

                currentState = AttackDirection.None;
            }
        }
        else
        {
            rb2D.linearVelocity = Vector2.zero;
            currentState = AttackDirection.None;
        }
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