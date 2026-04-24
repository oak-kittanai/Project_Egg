using UnityEngine;

public class Enemy_KnightJay : BaseMonster
{
    [Header("Stats")]
    [SerializeField] float walkSpeed = 10f;



    protected override void MonsterSpecificUpdate()
    {
        if (phaseRestTimer.IsRunning && !phaseRestTimer.Expired(Runner)) return;
        if (!delayActionTimer.ExpiredOrNotRunning(Runner)) return;

        if (hasSpottedPlayer)
        {
            AttackDirection targetAttack = attackPatterns[currentPatternIndex].nextState;

            AttackDirection playerCurrentDir = CheckDirection(targetPosition);

            if (playerCurrentDir == targetAttack && Vector2.Distance(transform.position, targetPosition) <= detectionRadius)
            {
                rb2D.linearVelocity = Vector2.zero;
                AttackToDirection();
            }
            else
            {
                Vector2 moveDir = (targetPosition - (Vector2)transform.position).normalized;

                rb2D.linearVelocity = moveDir * walkSpeed;
                currentState = AttackDirection.None;
            }
        }
    }

    public override void Render()
    {
        base.Render();
    }
}
