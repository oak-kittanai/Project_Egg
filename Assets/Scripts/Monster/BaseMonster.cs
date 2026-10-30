using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum AttackDirection
{
    None, Right45Degree, Right90Degree, Right135Degree,
    Down, Left215Degree, Left260Degree, Left305Degree, Up
}

public enum MonState
{
    Idle, Walk, Attack
}

public class BaseMonster : NetworkBehaviour
{
    [Header("Ref")]
    public Rigidbody2D rb2D;
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public Collider2D col;

    [Header("Session State")]
    [Networked, OnChangedRender(nameof(OnStateChangedCallback))] public AttackDirection currentAttackDirectionState { get; set; }
    public event Action<AttackDirection> OnStateChanged;

    [Networked, OnChangedRender(nameof(OnMonStateChangedCallback))] public MonState currentState { get; set; }
    public event Action<MonState> OnMonsterStateChanged;

    [Header("Damage Settings")]
    [SerializeField] public int damage = 1;
    [SerializeField] public float knockbackForce = 12f;

    [Header("Detect")]
    [SerializeField] public LayerMask playerLayer;
    [SerializeField] public LayerMask obstacleLayer;
    [SerializeField] public float detectionRadius = 15f;
    [SerializeField] public float attackRadius = 15f;

    [Networked] public Vector2 targetPosition { get; set; }
    [Networked] public NetworkBool hasSpotPlayer { get; set; }

    private readonly HashSet<MovementCharacter> playersInRange = new HashSet<MovementCharacter>();

    // collider ผู้เล่นที่ตัวมอนชนอยู่ตอนนี้ (นอกนั้นทะลุ — ค่าเริ่มต้นจาก FB2-3)
    private readonly HashSet<Collider2D> solidWithPlayers = new HashSet<Collider2D>();

    [Header("HitBox System")]
    [SerializeField] public Transform hitBoxPivot;
    [SerializeField] public GameObject hitBox;

    public override void Spawned()
    {
        if (rb2D == null) rb2D = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (col == null) col = GetComponent<Collider2D>();

        foreach (var player in FindObjectsByType<MovementCharacter>(FindObjectsSortMode.None))
            IgnorePlayerCollision(this, player);

        // ฝั่ง client ตำแหน่งมอนมาจาก netPos ใน Render() — ไม่ให้ physics ในเครื่องขยับมอนเอง (โดนผู้เล่นชนแล้วไหลค้าง)
        if (!HasStateAuthority && rb2D != null)
        {
            rb2D.bodyType = RigidbodyType2D.Kinematic;
            rb2D.linearVelocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
        }
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        if (!HasStateAuthority) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameplayFrozen) return; // freeze ตอน pause/dialogue

        PlayerRadar();
        MonsterSpecificUpdate();

        // host ปรับ collider ใน tick เดียวกับที่ state เปลี่ยน (ทุกเครื่องเรียกซ้ำใน Render)
        ApplyVisualState();
    }

    public void NotifyRadarTriggerEnter(Collider2D other)
    {
        if (!HasStateAuthority) return;
        if (other.TryGetComponent<MovementCharacter>(out var player))
            playersInRange.Add(player);
    }

    public void NotifyRadarTriggerExit(Collider2D other)
    {
        if (!HasStateAuthority) return;
        if (other.TryGetComponent<MovementCharacter>(out var player))
            playersInRange.Remove(player);
    }

    private void OnStateChangedCallback()
    {
        OnStateChanged?.Invoke(currentAttackDirectionState);
    }

    private void OnMonStateChangedCallback()
    {
        OnMonsterStateChanged?.Invoke(currentState);
    }

    public void PlayAnimation(string animationName)
    {
        if (animator != null) animator.Play(animationName);
    }

    public void PlayerRadar()
    {
        playersInRange.RemoveWhere(p => p == null || !p.enabled);

        bool foundPlayer = false;

        foreach (var player in playersInRange)
        {
            Vector2 dirToPlayer = (player.transform.position - transform.position).normalized;
            float distToPlayer = Vector2.Distance(transform.position, player.transform.position);
            RaycastHit2D hitObstacle = Physics2D.Raycast(transform.position, dirToPlayer, distToPlayer, obstacleLayer);
            if (hitObstacle.collider != null) continue;
            targetPosition = player.transform.position;
            hasSpotPlayer = true;
            foundPlayer = true;
            break;
        }

        if (!foundPlayer && hasSpotPlayer)
        {
            hasSpotPlayer = false;
            currentAttackDirectionState = AttackDirection.None;
        }
    }

    public void RotateHitBoxToDirection(AttackDirection dir)
    {
        if (hitBoxPivot == null) return;
        float zRotation = 0f;

        switch (dir)
        {
            case AttackDirection.Right90Degree: zRotation = 0f; break;
            case AttackDirection.Up: zRotation = 90f; break;
            case AttackDirection.Left260Degree: zRotation = 180f; break;
            case AttackDirection.Down: zRotation = -90f; break;
            case AttackDirection.Right45Degree: zRotation = 45f; break;
            case AttackDirection.Right135Degree: zRotation = -45f; break;
            case AttackDirection.Left305Degree: zRotation = 135f; break;
            case AttackDirection.Left215Degree: zRotation = -135f; break;
            case AttackDirection.None: return;
        }

        hitBoxPivot.rotation = Quaternion.Euler(0, 0, zRotation);
    }

    public AttackDirection CheckDirection(Vector2 playerPosition)
    {
        Vector2 dir = (playerPosition - (Vector2)transform.position).normalized;
        float angle = 90f - (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        if (angle < 0f) angle += 360f;

        float snappedAngle = Mathf.Round(angle / 45f) * 45f;
        if (snappedAngle >= 360f) snappedAngle = 0f;

        switch (snappedAngle)
        {
            case 0f: return AttackDirection.Up;
            case 45f: return AttackDirection.Right45Degree;
            case 90f: return AttackDirection.Right90Degree;
            case 135f: return AttackDirection.Right135Degree;
            case 180f: return AttackDirection.Down;
            case 225f: return AttackDirection.Left215Degree;
            case 270f: return AttackDirection.Left260Degree;
            case 315f: return AttackDirection.Left305Degree;
            default: return AttackDirection.None;
        }
    }

    // ผู้เล่นเดินทะลุตัวมอนได้ (FB2-3): Puffy/Knight Jay ขยับด้วย transform จึงวาร์ปซ้อนตัวผู้เล่น
    // แล้วฟิสิกส์ดันผู้เล่นออกทางที่สั้นที่สุด บางทีทะลุพื้นออกนอกแมพ — ดาเมจยังมาจาก hitbox (trigger) เหมือนเดิม
    // หินที่ปายังชนตัวมอนได้ (ไม่ได้ปิดกับ layer อื่น)
    public static void IgnorePlayerCollision(BaseMonster monster, MovementCharacter player)
    {
        if (monster == null || player == null) return;

        Collider2D monsterCol = monster.col != null ? monster.col : monster.GetComponent<Collider2D>();
        if (monsterCol == null) return;

        if (player.coll2D != null) Physics2D.IgnoreCollision(monsterCol, player.coll2D, true);
        if (player.normalCollider != null) Physics2D.IgnoreCollision(monsterCol, player.normalCollider, true);
        if (player.carryCollider != null) Physics2D.IgnoreCollision(monsterCol, player.carryCollider, true);
    }

    // ช่วงที่ตัวมอนชนผู้เล่นได้ (FB2-7) — ค่าเริ่มต้นทะลุตลอด มอนแต่ละแบบกำหนดเองจาก state ที่ sync อยู่แล้ว
    protected virtual bool BlocksPlayers => false;

    // เรียกทุก tick บน host และทุกเฟรมใน Render ทุกเครื่อง (ผ่าน ApplyVisualState) ให้ผลเหมือนกันทุกเครื่อง
    protected void UpdatePlayerCollision()
    {
        if (col == null || GameManager.Instance == null) return;

        bool block = BlocksPlayers && col.enabled;

        foreach (var player in GameManager.Instance.activePlayers)
        {
            if (player == null) continue;

            SetSolidWith(player.coll2D, block);
            SetSolidWith(player.normalCollider, block);
            SetSolidWith(player.carryCollider, block);
        }
    }

    private void SetSolidWith(Collider2D playerCol, bool block)
    {
        if (playerCol == null) return;
        if (block == solidWithPlayers.Contains(playerCol)) return;

        // ผู้เล่นยืนซ้อนตัวมอนอยู่ตอนเปลี่ยนเป็นตัน -> ฟิสิกส์จะดีดออก (อาจทะลุพื้นแบบ FB2-3) — ให้ทะลุต่อจนกว่าจะเดินออก
        if (block && (!playerCol.isActiveAndEnabled || col.Distance(playerCol).isOverlapped)) return;

        Physics2D.IgnoreCollision(col, playerCol, !block);
        if (block) solidWithPlayers.Add(playerCol);
        else solidWithPlayers.Remove(playerCol);
    }

    // hitbox ตัดสินการโดนบน host เท่านั้น — เดิมเป็น RPC ที่ฝั่ง client ก็ส่งมาหา host โดยไม่จำเป็น
    public void SetHitBoxActive(bool active)
    {
        if (!HasStateAuthority) return;
        if (hitBox != null) hitBox.SetActive(active);
    }

    // การแสดงผล/collider ของมอนคำนวณจาก state ที่ sync อยู่แล้ว ทุกเครื่องได้ผลเดียวกัน (แทน SetVisuals_RPC)
    protected virtual void ApplyVisualState() { }

    public void InstantKill()
    {
        if (HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }

    protected virtual void MonsterSpecificUpdate() { }

    //Giz Good
    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        if (!Application.isPlaying || Object == null || !Object.IsValid) return;
    }
}