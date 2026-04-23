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

[System.Serializable]
public class AttackPattern
{
    public int attackNumber;
    public AttackDirection nextState;
    public float delayBetweenActionOption; // if 0 isDelayBetweenActionOption will be false and use base Delay
    public int attackPerPhase;
}

public class BaseMonster : NetworkBehaviour
{
    [Header("Ref")]
    public Rigidbody2D rb2D;

    [Header("Session State")]
    [Networked, OnChangedRender(nameof(OnStateChangedCallback))] public AttackDirection currentState { get; set; }
    public event Action<AttackDirection> OnStateChanged;

    [Header("Setting")]
    [Networked] Vector3 spawnPos { get; set; }
    [SerializeField] float maxReachDistance = 10f; // make sure monster didn't go any further

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
    [SerializeField] public LayerMask targetLayer;
    [SerializeField] public float detectionRadius = 15f;
    [SerializeField] public Vector2 defaultDashDirection = Vector2.right;

    [Networked] public Vector2 targetPosition { get; set; }
    [Networked] public NetworkBool hasSpottedPlayer { get; set; }

    [Header("AttackState_Setting")]
    [Networked] public NetworkBool isPreparing { get; set; }

    [Networked] public int currentDashCount { get; set; }
    [Networked] public int setDashCount { get; set; }

    [Networked] public TickTimer delayActionTimer { get; set; }
    [Networked] public NetworkBool isDelayBetweenActionOption { get; set; }
    [SerializeField] public float delayBetweenActionOption;
    [SerializeField] public float delayBetweenAction = 1f;

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

        spawnPos = transform.position;
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        MonsterSpecificUpdate();
    }

    private void OnStateChangedCallback()
    {

        OnStateChanged?.Invoke(currentState);
    }

    #region AttackMechanic

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

    public void AttackToDirection()
    {
        if (attackPatterns == null || attackPatterns.Count == 0) return;

        AttackPattern currentPattern = attackPatterns[currentPatternIndex];

        if (currentAttacksLeftInPhase <= 0 && !phaseRestTimer.IsRunning)
        {
            currentAttacksLeftInPhase = currentPattern.attackPerPhase;
        }

        currentState = currentPattern.nextState;
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
            currentState = AttackDirection.None;

            currentPatternIndex++;

            if (currentPatternIndex >= attackPatterns.Count)
            {
                currentPatternIndex = 0;
            }
        }
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(this.transform.position, detectionRadius);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(spawnPos, maxReachDistance);
    }
}
