using Fusion;
using System;
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
    [SerializeField] public float detectionRadius = 15f;
    [SerializeField] public float attackRadius = 15f;

    [Networked] public Vector2 targetPosition { get; set; }
    [Networked] public NetworkBool hasSpotPlayer { get; set; }

    [Header("HitBox System")]
    [SerializeField] public Transform hitBoxPivot;
    [SerializeField] public GameObject hitBox;

    public override void Spawned()
    {
        if (rb2D == null) rb2D = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (col == null) col = GetComponent<Collider2D>();
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        if (!HasStateAuthority) return;

        PlayerRadar();
        MonsterSpecificUpdate();
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
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, playerLayer);
        bool foundPlayer = false;

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<MovementCharacter>(out _)) continue;

            targetPosition = hit.transform.position;
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

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void TriggerHitBox_RPC(bool o)
    {
        if (hitBox != null) hitBox.SetActive(o);
    }

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