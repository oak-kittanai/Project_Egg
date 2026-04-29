using Fusion;
using System;
using UnityEngine;

public enum AttackDirection
{
    None,
    Right45Degree,
    Right90Degree,
    Right135Degree,
    Down,
    Left215Degree,
    Left260Degree,
    Left305Degree,
    Up
}

public enum MonState
{
    Idle,
    Walk,
    Attack
}

[System.Serializable]
public class AttackPattern
{
    public int attackNumber;
    public float delayBetweenActionOption; // if 0 isDelayBetweenActionOption will be false and use base Delay
    public int attackPerPhase;
}

public class BaseMonster : NetworkBehaviour
{
    [Header("Ref")]
    public Rigidbody2D rb2D;
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    [Header("Session State")]
    [Networked, OnChangedRender(nameof(OnStateChangedCallback))] public AttackDirection currentAttackDirectionState { get; set; }
    public event Action<AttackDirection> OnStateChanged;

    [Networked, OnChangedRender(nameof(OnMonStateChangedCallback))] public MonState currentState { get; set; }
    public event Action<MonState> OnMonsterStateChanged;

    [Header("Setting")]
    [SerializeField] Vector3 spawnPos;
    [SerializeField] public float maxReachDistance = 10f; // make sure monster didn't go any further

    [Header("Networked etc")]
    // Stun
    [Networked] public NetworkBool isStun { get; set; }
    [Networked] public TickTimer stunTimer { get; set; }
    [SerializeField] float stunTimeAmount = 3f;

    [Networked] public NetworkBool isStunAble { get; set; }
    [Networked] public TickTimer stunAbleAgainTimer { get; set; }
    [SerializeField] public float stunAbleCooldown = 3f;

    [Header("Damage Settings")]
    [SerializeField] public int damage = 1;
    [SerializeField] public float knockbackForce = 12f;

    [Header("Detect")]
    // Detect Player
    [SerializeField] public LayerMask targetLayer;
    [SerializeField] public float detectionRadius = 15f;
    [SerializeField] public Vector2 defaultDashDirection = Vector2.right;

    //
    [SerializeField] public float attackRadius = 15f;

    [Networked] public NetworkBool isReturningToSpawn { get; set; }
    [SerializeField] float returnSpeed = 8f;

    [SerializeField] LayerMask playerLayer;
    [Networked] public Vector2 targetPosition { get; set; }
    [Networked] public NetworkBool hasSpottedPlayer { get; set; }

    [Header("AttackState_Setting")]
    [Networked] public NetworkBool isPreparing { get; set; }

    [Networked] public int currentDashCount { get; set; }
    [Networked] public int setDashCount { get; set; }

    public AttackDirection nextState;

    [Networked] public TickTimer delayActionTimer { get; set; }
    [Networked] public NetworkBool isDelayBetweenActionOption { get; set; }
    [SerializeField] public float delayBetweenActionOption;
    [SerializeField] public float delayBetweenAction = 1f;

    [Header("HitBox System")]
    [SerializeField] public Transform hitBoxPivot;

    [SerializeField] public GameObject hitBox;

    [Header("Attack Patterns Data")]
    public System.Collections.Generic.List<AttackPattern> attackPatterns = new System.Collections.Generic.List<AttackPattern>();

    [Header("Pattern Tracking (Networked)")]
    [Networked] public int currentPatternIndex { get; set; }
    [Networked] public int currentAttacksLeftInPhase { get; set; }
    [Networked] public TickTimer phaseRestTimer { get; set; }
    [SerializeField] public float restTimeAfterPhase = 2f;

    public override void Spawned()
    {
        if (rb2D == null) rb2D = GetComponent<Rigidbody2D>();
        if (rb2D != null) Debug.Log($"{this.name} Has Rb2D");
        else Debug.Log($"{this.name} can't find Rb2D");

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) Debug.Log($"{this.name} Has Animator");
        else Debug.Log($"{this.name} can't find Animator");

        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) Debug.Log($"{this.name} Has spriteRenderer");
        else Debug.Log($"{this.name} can't find spriteRenderer");

        spawnPos = transform.position;
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        if (!HasStateAuthority) return;

        HandleStunLogic();

        PlayerRadar();

        if (isStun)
        {
            if (rb2D != null) rb2D.linearVelocity = Vector2.zero;
            return;
        }

        float distFromSpawn = Vector2.Distance(transform.position, spawnPos);

        if (distFromSpawn > maxReachDistance && !isReturningToSpawn)
        {
            isReturningToSpawn = true;
            hasSpottedPlayer = false;
            currentAttackDirectionState = AttackDirection.None;
        }

        if (isReturningToSpawn)
        {
            Vector2 returnDir = ((Vector2)spawnPos - (Vector2)transform.position).normalized;
            rb2D.linearVelocity = returnDir * returnSpeed;

            if (distFromSpawn < 0.5f)
            {
                isReturningToSpawn = false;
                rb2D.linearVelocity = Vector2.zero;
            }

            return;
        }

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
        animator.Play(animationName);
    }

    #region AttackMechanic

    public void PlayerRadar()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, playerLayer);

        bool foundPlayer = false;

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<MovementCharacter>(out _)) continue;

            targetPosition = hit.transform.position;
            hasSpottedPlayer = true;
            foundPlayer = true;

            break;
        }

        if (!foundPlayer && hasSpottedPlayer)
        {
            hasSpottedPlayer = false;
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

    public void AttackToDirection(AttackDirection targetDir)
    {
        if (attackPatterns == null || attackPatterns.Count == 0) return;

        AttackPattern currentPattern = attackPatterns[currentPatternIndex];

        if (currentAttacksLeftInPhase <= 0 && !phaseRestTimer.IsRunning)
        {
            currentAttacksLeftInPhase = currentPattern.attackPerPhase;
        }

        currentAttackDirectionState = targetDir;
        currentAttacksLeftInPhase--;

        if (currentPattern.delayBetweenActionOption > 0f)
        {
            delayActionTimer = TickTimer.CreateFromSeconds(Runner, currentPattern.delayBetweenActionOption);
        }
        else
        {
            delayActionTimer = TickTimer.CreateFromSeconds(Runner, delayBetweenAction);
        }

        if (currentAttacksLeftInPhase <= 0)
        {
            phaseRestTimer = TickTimer.CreateFromSeconds(Runner, restTimeAfterPhase);
            currentAttackDirectionState = AttackDirection.None;

            currentPatternIndex++;

            if (currentPatternIndex >= attackPatterns.Count)
            {
                currentPatternIndex = 0;
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void TriggerHitBox_RPC(bool o)
    {
        hitBox.SetActive(false);
    }

    #endregion

    #region StunFunction

    public void HandleStunLogic()
    {
        if (isStun)
        {
            if (stunTimer.Expired(Runner))
            {
                StunExpired();
            }
        }
        else if (!isStunAble)
        {
            if (stunAbleAgainTimer.ExpiredOrNotRunning(Runner))
            {
                isStunAble = true;
            }
        }
    }

    public void TakeDamage()
    {
        if (isStunAble && !isStun)
        {
            isStun = true;
            isStunAble = false;
            SetStunTimer();
        }
        else Debug.Log("can't Stun, do nothing");
    }

    public void StunExpired()
    {
        isStun = false;
        SetStunAbleTimer();
    }

    #endregion

    #region SetTimer

    private void SetStunAbleTimer()
    {
        stunAbleAgainTimer = TickTimer.CreateFromSeconds(Runner,stunAbleCooldown);
    }

    private void SetStunTimer()
    {
        stunTimer = TickTimer.CreateFromSeconds(Runner, stunTimeAmount);
    }

    private void SetDelayActionTimer()
    {
        if (isDelayBetweenActionOption) delayActionTimer = TickTimer.CreateFromSeconds(Runner, delayBetweenActionOption);
        else delayActionTimer = TickTimer.CreateFromSeconds(Runner, delayBetweenAction);
    }

    #endregion

    protected virtual void MonsterSpecificUpdate()
    {

    }


    public override void Render()
    {
        
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 selfPos = transform.position;
        Vector3 spawnpointPos = spawnPos;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(selfPos, detectionRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(spawnpointPos, maxReachDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(selfPos, attackRadius);
    }
}
