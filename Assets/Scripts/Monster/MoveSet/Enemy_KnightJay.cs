using Fusion;
using UnityEngine;

public class Enemy_KnightJay : BaseMonster
{
    [Header("Dash Setting")]
    [Tooltip("หน่วงเวลาเก็บ Pos")]
    [SerializeField] float prepareDelay = 0.6f;
    [SerializeField] float dashSpeed = 15f;
    [SerializeField] float despawnDelay = 0.3f;

    [Header("Respawn Setting")]
    [SerializeField] bool canRespawn = true;
    [SerializeField] float respawnTime = 5f;

    [Networked] private NetworkBool isPreparing { get; set; }
    [Networked] private NetworkBool isDashing { get; set; }
    [Networked] private NetworkBool hasFinishedDash { get; set; }
    [Networked] private Vector2 lockedTargetPos { get; set; }

    [Networked] private TickTimer actionTimer { get; set; }
    [Networked] private TickTimer dashFailsafeTimer { get; set; }

    //หันหน้า
    [Networked] private NetworkBool isFlip { get; set; }

    [Networked] private Vector2 startPosition { get; set; }
    [Networked] private NetworkBool isHidden { get; set; }

    [Networked] private Vector2 netPos { get; set; }

    public override void Spawned()
    {
        base.Spawned();
        OnStateChanged += HandleJayAnimations;

        if (HasStateAuthority)
        {
            startPosition = transform.position;
            ResetMonster();
        }
    }

    private void ResetMonster()
    {
        isPreparing = false;
        isDashing = false;
        hasFinishedDash = false;
        isFlip = false;
        isHidden = false;
        currentAttackDirectionState = AttackDirection.None;

        transform.position = startPosition;
        netPos = startPosition;

        if (rb2D != null) rb2D.linearVelocity = Vector2.zero;
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

        if (isHidden)
        {
            if (actionTimer.Expired(Runner)) ResetMonster();
            return;
        }

        if (hasFinishedDash)
        {
            if (actionTimer.Expired(Runner))
            {
                if (canRespawn)
                {
                    isHidden = true;
                    transform.position = startPosition;
                    netPos = startPosition;
                    SetHitBoxActive(false);
                    actionTimer = TickTimer.CreateFromSeconds(Runner, respawnTime);
                }
                else
                {
                    Runner.Despawn(Object);
                }
            }
            return;
        }

        if (isDashing)
        {
            if (rb2D != null) rb2D.linearVelocity = Vector2.zero;

            transform.position = Vector2.MoveTowards(transform.position, lockedTargetPos, dashSpeed * Runner.DeltaTime);
            netPos = transform.position;

            float distance = Vector2.Distance(transform.position, lockedTargetPos);

            if (distance <= 0.1f || dashFailsafeTimer.Expired(Runner))
            {
                transform.position = lockedTargetPos;
                netPos = transform.position;

                SetHitBoxActive(false);

                hasFinishedDash = true;
                actionTimer = TickTimer.CreateFromSeconds(Runner, despawnDelay);
            }
        }
        else if (isPreparing)
        {
            if (rb2D != null) rb2D.linearVelocity = Vector2.zero;
            netPos = transform.position;

            isFlip = targetPosition.x < transform.position.x;

            if (actionTimer.Expired(Runner))
            {
                lockedTargetPos = targetPosition;
                isPreparing = false;
                isDashing = true;

                dashFailsafeTimer = TickTimer.CreateFromSeconds(Runner, 2f);

                isFlip = lockedTargetPos.x < transform.position.x;

                AttackDirection dashDir = CheckDirection(lockedTargetPos);
                RotateHitBoxToDirection(dashDir);
                currentAttackDirectionState = dashDir;

                SetHitBoxActive(true);
            }
        }
        else if (hasSpotPlayer)
        {
            isPreparing = true;
            actionTimer = TickTimer.CreateFromSeconds(Runner, prepareDelay);

            if (rb2D != null) rb2D.linearVelocity = Vector2.zero;
            netPos = transform.position;
            currentAttackDirectionState = AttackDirection.None;

            isFlip = targetPosition.x < transform.position.x;
        }
        else
        {
            if (rb2D != null) rb2D.linearVelocity = Vector2.zero;
            netPos = transform.position;
            currentAttackDirectionState = AttackDirection.None;
        }
    }

    public override void Render()
    {
        if (!HasStateAuthority)
        {
            // ใช้ตำแหน่งที่ Fusion interpolate ระหว่าง 2 snapshot (เดิมไล่ค่าล่าสุดที่ได้รับ -> ขยับเป็นจังหวะตอน packet มาไม่สม่ำเสมอ)
            var interpolator = new NetworkBehaviourBufferInterpolator(this);
            transform.position = interpolator ? interpolator.Vector2(nameof(netPos)) : netPos;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = isFlip;
        }

        ApplyVisualState();
    }

    // ตัวมอนชนผู้เล่นเฉพาะตอนยืนเตรียมพุ่ง (FB2-7) — ยืนเฉยเดินทะลุได้, ตอนพุ่งตัวทะลุแต่ hitbox ตี + กระเด็น
    // (ตอนพุ่งมอนย้ายตำแหน่งทีละ tick ถ้าตัวชนผู้เล่นจะวาร์ปซ้อนแล้วดันผู้เล่นทะลุพื้นแบบ FB2-3)
    protected override bool BlocksPlayers => isPreparing && !isDashing && !isHidden && !hasFinishedDash;

    // ซ่อนตอนพุ่งจบ/รอเกิดใหม่ และปิด collider ตอนพุ่ง — ทุกเครื่องเหมือนกัน
    // (เดิมปิด collider ตอนพุ่งเฉพาะบน host ทำให้ client ชนตัวมอนที่กำลังพุ่งแต่ host ไม่ชน — NJ-8)
    protected override void ApplyVisualState()
    {
        bool visible = !isHidden && !hasFinishedDash;
        if (spriteRenderer != null) spriteRenderer.enabled = visible;
        if (col != null) col.enabled = visible && !isDashing;

        UpdatePlayerCollision();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        OnStateChanged -= HandleJayAnimations;
    }

    private void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        if (!Application.isPlaying || Object == null || !Object.IsValid)
        {
            return;
        }
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