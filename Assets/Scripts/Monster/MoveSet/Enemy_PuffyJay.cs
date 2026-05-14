using Fusion;
using UnityEngine;

public class Enemy_PuffyJay : BaseMonster
{
    [Header("Chase Setting")]
    [SerializeField] float walkSpeed = 2f;
    [Tooltip("เวลาเดินไล่")]
    [SerializeField] float chaseTime = 5f;

    [Header("Boom Timing")]
    [Tooltip("หน่วงเวลาเปิด hitbox")]
    [SerializeField] float hitboxDelay = 0.5f;
    [SerializeField] float despawnDelay = 0.3f;

    [Header("Respawn Tick")]
    [SerializeField] bool canRespawn = true;
    [SerializeField] float respawnTime = 5f;

    [Networked] private NetworkBool isChasing { get; set; }
    [Networked] private NetworkBool isPreparingBoom { get; set; }
    [Networked] private NetworkBool hasBoomed { get; set; }
    [Networked] private NetworkBool isFlip { get; set; }

    [Networked] private Vector2 startPosition { get; set; }
    [Networked] private NetworkBool isHidden { get; set; }
    [Networked] private TickTimer actionTimer { get; set; }

    [Networked] private Vector2 netPos { get; set; }

    public override void Spawned()
    {
        base.Spawned();
        OnMonsterStateChanged += HandlePuffyAnimations;

        if (HasStateAuthority)
        {
            startPosition = transform.position;
            ResetMonster();
        }
    }
    private void ResetMonster()
    {
        isChasing = false;
        isPreparingBoom = false;
        hasBoomed = false;
        isHidden = false;
        isFlip = false;
        currentState = MonState.Idle;

        transform.position = startPosition;
        netPos = startPosition;

        if (rb2D != null) rb2D.linearVelocity = Vector2.zero;
        SetVisuals_RPC(true);
        TriggerHitBox_RPC(false);
    }

    private void HandlePuffyAnimations(MonState state)
    {
        if (state == MonState.Attack) PlayAnimation("Puffy_Boom");
        else PlayAnimation("Puffy_Idle");
    }
    protected override void MonsterSpecificUpdate()
    {
        if (!HasStateAuthority) return;

        if (isHidden)
        {
            if (actionTimer.Expired(Runner)) ResetMonster();
            return;
        }

        if (hasBoomed)
        {
            if (rb2D != null) rb2D.linearVelocity = Vector2.zero;

            if (actionTimer.Expired(Runner))
            {
                if (canRespawn)
                {
                    isHidden = true;
                    SetVisuals_RPC(false);

                    transform.position = startPosition;
                    netPos = startPosition;

                    actionTimer = TickTimer.CreateFromSeconds(Runner, respawnTime);
                }
                else
                {
                    Runner.Despawn(Object);
                }
            }
            return;
        }

        if (isPreparingBoom)
        {
            if (rb2D != null) rb2D.linearVelocity = Vector2.zero;
            netPos = transform.position;

            if (actionTimer.Expired(Runner))
            {
                isPreparingBoom = false;
                hasBoomed = true;

                TriggerHitBox_RPC(true);

                actionTimer = TickTimer.CreateFromSeconds(Runner, despawnDelay);
            }
        }
        else if (isChasing)
        {
            if (actionTimer.Expired(Runner))
            {
                isChasing = false;
                isPreparingBoom = true;

                if (rb2D != null) rb2D.linearVelocity = Vector2.zero;
                currentState = MonState.Attack;
                actionTimer = TickTimer.CreateFromSeconds(Runner, hitboxDelay);
                return;
            }
            
            if (rb2D != null) rb2D.linearVelocity = Vector2.zero;
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, walkSpeed * Runner.DeltaTime);

            netPos = transform.position;
            isFlip = targetPosition.x < transform.position.x;
        }
        else if (hasSpotPlayer)
        {
            isChasing = true;
            actionTimer = TickTimer.CreateFromSeconds(Runner, chaseTime);
            currentState = MonState.Walk;
        }

        else
        {
            if (rb2D != null) rb2D.linearVelocity = Vector2.zero;
            netPos = transform.position;
            currentState = MonState.Idle;
        }
    }

    public override void Render()
    {
        if (!HasStateAuthority)
        {
            transform.position = Vector2.Lerp(transform.position, netPos, Time.deltaTime * 15f);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = isFlip;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void SetVisuals_RPC(bool show)
    {
        if (spriteRenderer != null) spriteRenderer.enabled = show;
        if (col != null) col.enabled = show;
        if (!show) TriggerHitBox_RPC(false);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        OnMonsterStateChanged -= HandlePuffyAnimations;
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (isChasing)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetPosition);
        }
        else if (isPreparingBoom)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 1.2f);
        }
    }
}