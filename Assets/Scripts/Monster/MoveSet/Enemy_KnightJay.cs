using Fusion;
using UnityEngine;

public class Enemy_KnightJay : BaseMonster
{
    [Header("Dash Setting")]
    [Tooltip("หน่วงเวลาเก็บ Pos")]
    [SerializeField] float prepareDelay = 0.6f;
    [SerializeField] float dashSpeed = 15f;
    [SerializeField] float despawnDelay = 0.3f;

    [Networked] private NetworkBool isPreparing { get; set; }
    [Networked] private NetworkBool isDashing { get; set; }
    [Networked] private NetworkBool hasFinishedDash { get; set; }
    [Networked] private Vector2 lockedTargetPos { get; set; }

    [Networked] private TickTimer actionTimer { get; set; }
    [Networked] private TickTimer dashFailsafeTimer { get; set; }

    //หันหน้า
    [Networked] private NetworkBool isFlip { get; set; }

    public override void Spawned()
    {
        base.Spawned();
        OnStateChanged += HandleJayAnimations;

        if (HasStateAuthority)
        {
            isPreparing = false;
            isDashing = false;
            hasFinishedDash = false;
            isFlip = false;
        }
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
        if (!HasStateAuthority) return;

        if (hasFinishedDash)
        {
            if (actionTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
            }
            return;
        }

        if (isDashing)
        {
            if (rb2D != null) rb2D.linearVelocity = Vector2.zero;

            transform.position = Vector2.MoveTowards(transform.position, lockedTargetPos, dashSpeed * Runner.DeltaTime);

            float distance = Vector2.Distance(transform.position, lockedTargetPos);

            if (distance <= 0.1f || dashFailsafeTimer.Expired(Runner))
            {
                transform.position = lockedTargetPos;

                TriggerHitBox_RPC(true);
                HideVisuals_RPC();

                hasFinishedDash = true;
                actionTimer = TickTimer.CreateFromSeconds(Runner, despawnDelay);
            }
        }
        else if (isPreparing)
        {
            if (rb2D != null) rb2D.linearVelocity = Vector2.zero;

            isFlip = targetPosition.x < transform.position.x;

            if (actionTimer.Expired(Runner))
            {
                lockedTargetPos = targetPosition;
                isPreparing = false;
                isDashing = true;

                dashFailsafeTimer = TickTimer.CreateFromSeconds(Runner, 2f);

                if (col != null) col.enabled = false;

                isFlip = lockedTargetPos.x < transform.position.x;

                AttackDirection dashDir = CheckDirection(lockedTargetPos);
                RotateHitBoxToDirection(dashDir);
                currentAttackDirectionState = dashDir;
            }
        }
        else if (hasSpotPlayer)
        {
            isPreparing = true;
            actionTimer = TickTimer.CreateFromSeconds(Runner, prepareDelay);

            if (rb2D != null) rb2D.linearVelocity = Vector2.zero;
            currentAttackDirectionState = AttackDirection.None;

            isFlip = targetPosition.x < transform.position.x;
        }
        else
        {
            if (rb2D != null) rb2D.linearVelocity = Vector2.zero;
            currentAttackDirectionState = AttackDirection.None;
        }
    }

    public override void Render()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = isFlip;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void HideVisuals_RPC()
    {
        if (spriteRenderer != null) spriteRenderer.enabled = false;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        OnStateChanged -= HandleJayAnimations;
    }

    private void OnDrawGizmos()
    {
        if (isDashing || isPreparing)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, lockedTargetPos);
            Gizmos.DrawWireSphere(lockedTargetPos, 0.5f);
        }

        if (hasSpotPlayer && !isDashing)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }
}