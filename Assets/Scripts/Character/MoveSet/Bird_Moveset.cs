using Fusion;
using System;
using UnityEngine;

public class Bird_Moveset : MovementCharacter, IstunAble
{
    [Header("Bird Settings")]
    [SerializeField] float normalFlyTime = 5f;
    [SerializeField] float carryFlyTime = 3f;
    [SerializeField] float floatingGravity = 0.1f;

    //SOUND
    [SerializeField] public AudioClip flySoundClip;
    [SerializeField] public AudioClip stopFlySoundClip;
    [SerializeField] public AudioClip throwSoundClip;

    [Header("Physics Materials")]
    [SerializeField] PhysicsMaterial2D zeroFrictionMaterial;
    private PhysicsMaterial2D defaultMaterial;

    [Header("Bird State")]
    [Networked] private TickTimer FlightTimer { get; set; }
    [Networked, OnChangedRender(nameof(OnFlyingStateChanged))]
    public NetworkBool IsFlying { get; set; }
    [Networked] public bool IsAlreadyFly { get; set; }
    [Networked] public bool AlreadyFloating { get; set; }


    [Header("Pressed")]
    [Networked] public bool _wasJumpPressed { get; set; }
    [Networked] public bool _wasPrepareThrowPressed { get; set; }
    [Networked] public bool _wasisThrowItemPressed { get; set; }

    // Drowning
    [Networked] private TickTimer DrownTimer { get; set; }
    [SerializeField] float drowningTime = 3f;
    [Networked, OnChangedRender(nameof(OnDrownTimerStateChanged))]
    public NetworkBool startTimer { get; set; }

    [Header("ThrowSystem")]
    [SerializeField] public NetworkObject throwAblePrefab;
    [Networked] public bool _prepareToThrow { get; set; }

    [SerializeField] float projectileSpeed;
    [SerializeField] public Transform throwPoint;
    // Line
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] int lineCounts;
    [SerializeField] float timeIntervalinPoints = 0.01f;
    [SerializeField] float currentAimX;
    // Sweeping Aim
    [SerializeField] float aimSweepSpeed = 3f;
    [SerializeField] float maxAimAngle = 30f;

    [Header("Unlockable Skills")]
    [Networked, OnChangedRender(nameof(OnSkillStateChanged))] public NetworkBool isFlyUnlocked { get; set; }
    [Networked, OnChangedRender(nameof(OnSkillStateChanged))] public NetworkBool isThrowUnlocked { get; set; }

    public override void Spawned()
    {
        base.Spawned();

        if (rb2D != null) defaultMaterial = rb2D.sharedMaterial;
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
    }

    public void TriggerStun()
    {
        ApplyStun();
    }

    protected override void OnFixedUpdateSpecific()
    {
        bool isMenuOpen = GameManager.Instance != null && GameManager.Instance.IsPaused;

        if (GetInput(out NetworkInputData input))
        {
            if (!isMenuOpen)
            {
                HandleFlightLogic(input);
                HandleThrowLogic(input);
            }
            else
            {
                if (_prepareToThrow) CancelThrow();

                _wasJumpPressed = input.KeybindJump;
                _wasisThrowItemPressed = input.KeybindThrowItem;
            }
        }

        if (IsGrounded)
        {
            IsAlreadyFly = false;

            if (AlreadyFloating) StopFloating();

            if (!IsFlying && rb2D != null && rb2D.sharedMaterial != defaultMaterial)
            {
                rb2D.sharedMaterial = defaultMaterial;
            }
        }

        bool effectivelyCarried = IsBeingCarried || localIsBeingCarriedPredict;
        NetworkId effectiveCarrierId = IsBeingCarried ? CarrierId : localCarrierIdPredict;

        if (effectivelyCarried)
        {
            if (AlreadyFloating) StopFloating();

            if (Runner.TryFindObject(effectiveCarrierId, out var duckObj) && duckObj.TryGetComponent<MovementCharacter>(out var duck))
            {
                if (!IsFlying && (duck.IsGrounded || duck.isWaterSurface))
                {
                    IsAlreadyFly = false;
                    resetAnimation = true;

                    if (cAnimation != null) cAnimation.ReturnToBlendAnimation();
                }
                else if (duck.IsHeadUnderwater) { /* do drowning but duck carry animation */ }

                if (HasStateAuthority || HasInputAuthority)
                {
                    cAnimation.FlipX = duck.cAnimation.FlipX;
                }
            }
        }

        if (!isMenuOpen) HandleDrowning();
    }

    private void HandleDrowning()
    {
        bool effectivelyCarried = IsBeingCarried || localIsBeingCarriedPredict;
        NetworkId effectiveCarrierId = IsBeingCarried ? CarrierId : localCarrierIdPredict;

        bool isBirdDrowning = false;

        if (effectivelyCarried)
        {
            if (Runner.TryFindObject(effectiveCarrierId, out var duckObj) && duckObj.TryGetComponent<Duck_Moveset>(out var duck))
            {
                if (duck.onDiving || duck.IsHeadUnderwater)
                    isBirdDrowning = true;
            }
        }
        else
        {
            if (stilldrowning)
                isBirdDrowning = true;
        }

        if (isBirdDrowning)
        {
            if (!effectivelyCarried)
            {
                isMoveAble = false;
                isOptional = true;
                optionalGravity = 0f;
                rb2D.linearVelocity = new Vector2(0f, -1.5f);
                rb2D.linearDamping = 3f;
            }

            if (!startTimer)
            {
                if (HasStateAuthority) StartDrowningTimer();
            }
            else
            {
                if (DrownTimer.Expired(Runner) && HasStateAuthority)
                {
                    startTimer = false;
                    StartDrowningDamage();
                }
            }
        }
        else
        {
            if (!AlreadyFloating && !IsFlying && rb2D != null && rb2D.linearDamping != 0f)
                rb2D.linearDamping = 0f;

            if (startTimer && HasStateAuthority)
            {
                DrownTimer = TickTimer.None;
                startTimer = false;
            }

            StopDrowningDamage();
        }

        TickDrowningDamage();
    }

    private void StartDrowningTimer()
    {
        startTimer = true;
        DrownTimer = TickTimer.CreateFromSeconds(Runner, drowningTime);
    }

    public void OnDrownTimerStateChanged()
    {
        if (HasInputAuthority && localGUI != null)
        {
            if (startTimer)
            {
                localGUI.StartOxygenTracking(DrownTimer, Runner, Mathf.CeilToInt(drowningTime));
            }
            else
            {
                localGUI.StopOxygenTracking();
            }
        }
    }

    public override void OnDroppedEvent()
    {
        base.OnDroppedEvent();

        if (HasStateAuthority)
        {
            IsFlying = false;
            FlightTimer = TickTimer.None;

            FallingBusy = false;
            AlreadyFloating = false;
            isOptional = false;
            IsAlreadyFly = false;
        }

        if (HasInputAuthority && localGUI != null)
        {
            localGUI.StopFlightBar();
        }

        if (rb2D != null)
        {
            rb2D.sharedMaterial = defaultMaterial;
            rb2D.linearDamping = 0f;
            rb2D.gravityScale = normalGravity;
        }

        if (cAnimation != null)
        {
            cAnimation.FallingAndFloatAnimation(true, false);
        }

        startTimer = false;
        DrownTimer = TickTimer.None;
    }

    public override void RPC_OnRespawned()
    {
        base.RPC_OnRespawned();

        IsFlying = false;
        IsAlreadyFly = false;
        AlreadyFloating = false;
        FallingBusy = false;
        startTimer = false;
        DrownTimer = TickTimer.None;

        if (rb2D != null)
        {
            rb2D.sharedMaterial = defaultMaterial;
        }

        if (HasInputAuthority && localGUI != null)
            localGUI.StopOxygenTracking();
    }

    #region FlyLogic
    private void HandleFlightLogic(NetworkInputData input)
    {
        if (!isFlyUnlocked) return;

        bool isPressed = input.KeybindJump && !_wasJumpPressed;

        if (!IsBeingCarried)
        {
            if (isPressed && IsInAir)
            {
                if (!IsFlying && !IsAlreadyFly && !stilldrowning)
                {
                    StartFlying();
                }
            }
        }
        else
        {
            isMoveAble = false;

            if (isPressed)
            {
                bool isDuckDiving = false;
                if (Runner.TryFindObject(CarrierId, out var duckObj) && duckObj.TryGetComponent<Duck_Moveset>(out var duck))
                {
                    isDuckDiving = duck.onDiving || duck.IsHeadUnderwater;
                }

                if (!IsFlying && !IsAlreadyFly && !isDuckDiving)
                {
                    StartFlying();
                }
            }
        }

        if (isPressed && IsAlreadyFly && !IsFlying)
        {
            if (AlreadyFloating) StopFloating();
            else StartFloating();
        }

        if (IsFlying)
        {
            if (FlightTimer.Expired(Runner))
            {
                StopFlying();
                IsAlreadyFly = true;
                StartFloating();
            }
            else
            {
                if (!IsBeingCarried)
                {
                    rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, stats.s_flySpeed);
                }
                else
                {
                    if (Runner.TryFindObject(CarrierId, out var carrierObj) && carrierObj.TryGetComponent<MovementCharacter>(out var duck))
                    {
                        duck.rb2D.linearVelocity = new Vector2(duck.rb2D.linearVelocity.x, stats.s_flySpeed);
                    }
                }
            }
        }

        _wasJumpPressed = input.KeybindJump;
    }

    public void OnFlyingStateChanged()
    {
        if (Object.InputAuthority == Runner.LocalPlayer)
        {
            if (IsFlying)
            {
                float duration = IsBeingCarried ? carryFlyTime : normalFlyTime;
                if (flySoundClip != null) AudioManager.Instance?.PlayClipAtPosition(flySoundClip, transform.position);

                if (localGUI != null) localGUI.StartFlightBar(FlightTimer, Runner, duration);
            }
            else
            {
                if (stopFlySoundClip != null) AudioManager.Instance?.PlayClipAtPosition(stopFlySoundClip, transform.position);

                if (localGUI != null) localGUI.StopFlightBar();
            }
        }
    }

    private void StartFloating()
    {
        FallingBusy = true;
        AlreadyFloating = true;
        optionalGravity = floatingGravity;
        isOptional = true;

        cAnimation.FlyFloatAnimation();

        rb2D.linearDamping = 5f;
    }

    private void StopFloating()
    {
        FallingBusy = false;
        AlreadyFloating = false;
        isOptional = false;
        rb2D.linearDamping = 0f;
        rb2D.gravityScale = normalGravity;

        resetAnimation = true;
    }

    private void StartFlying()
    {
        IsFlying = true;
        isJumping = false;

        float duration = IsBeingCarried ? carryFlyTime : normalFlyTime;
        FlightTimer = TickTimer.CreateFromSeconds(Runner, duration);

        if (cAnimation != null) cAnimation.FlyUpAnimation();

        if (rb2D != null && zeroFrictionMaterial != null)
        {
            rb2D.sharedMaterial = zeroFrictionMaterial;
        }

        if (IsBeingCarried && Runner.TryFindObject(CarrierId, out var carrierObj))
        {
            if (carrierObj.TryGetComponent<Duck_Moveset>(out var duck))
            {
                if (duck.carryCollider != null) duck.carryCollider.sharedMaterial = zeroFrictionMaterial;
            }
        }

        Debug.Log($"Bird Flying! Duration: {duration}s");
    }

    private void StopFlying()
    {
        IsFlying = false;
        FlightTimer = TickTimer.None;

        if (rb2D != null)
        {
            rb2D.sharedMaterial = defaultMaterial;
        }

        if (IsBeingCarried && Runner.TryFindObject(CarrierId, out var carrierObj))
        {
            if (carrierObj.TryGetComponent<Duck_Moveset>(out var duck))
            {
                if (duck.carryCollider != null) duck.carryCollider.sharedMaterial = null;
            }
        }
    }

    public void ForceCancelFlight()
    {
        if (IsFlying) StopFlying();
        if (AlreadyFloating) StopFloating();
        IsAlreadyFly = false;
    }
    #endregion

    #region ThrowLogic

    public bool testMock_CanThrowItem = false;

    public void MockSetup_CanThrowItem(bool canThrow)
    {
        testMock_CanThrowItem = canThrow;
    }

    public void HandleThrowLogic(NetworkInputData input)
    {
        bool isPrepareThrowPressed = input.KeybindThrowItem && !_wasisThrowItemPressed;

        if (_prepareToThrow)
        {
            if (Mathf.Abs(input.horizontal) > 0.1f || input.KeybindJump)
            {
                CancelThrow();
            }
        }

        if (isPrepareThrowPressed)
        {
            if (!isThrowUnlocked) return;

            if (_canThrowItem && !(isWaterSurface || stilldrowning))
            {
                if (!_prepareToThrow)
                {
                    _prepareToThrow = true;
                }
                else
                {
                    PlayThrowAnimation_RPC();
                    ExecuteThrow();
                }
            }
            else
            {
                if (_prepareToThrow) CancelThrow();
            }
        }

        if (_prepareToThrow)
        {
            isMoveAble = false;
            IsInteractBusy = true;

            cAnimation.AimAnimation();
            UpdateOscillatingAim();
        }

        _wasisThrowItemPressed = input.KeybindThrowItem;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void PlayThrowAnimation_RPC()
    {
        if (cAnimation != null)
        {
            cAnimation.ThrowAnimation();
            Debug.Log("Try Smash Animation");
        }
    }

    public void ExecuteThrow()
    {
        Vector2 throwPos = throwPoint.position;
        Vector2 direction = throwPoint.right;

        NetworkObject spawnedRock = GameManager.Instance.ProjectileSpawn(throwAblePrefab, throwPos, direction, throwPoint.rotation, projectileSpeed);
        if (spawnedRock != null && spawnedRock.TryGetComponent<RockObject>(out var rockObj))
        {
            rockObj.ThrowerId = Object.Id;
        }

        if (HasInputAuthority && throwSoundClip != null)
        {
            AudioManager.Instance?.PlayClipAtPosition(throwSoundClip, transform.position);
        }

        _canThrowItem = false;
        HeldItemName = "";

        CancelThrow();
    }

    private void CancelThrow()
    {
        isMoveAble = true;
        _prepareToThrow = false;
        IsInteractBusy = false;

        if (cAnimation != null) cAnimation.ReturnToBlendAnimation();

        throwPoint.localRotation = Quaternion.identity;
    }

    private float GetAimAngle()
    {
        return Mathf.Sin((float)Runner.SimulationTime * aimSweepSpeed) * maxAimAngle;
    }

    private void ApplyAimRotation()
    {
        if (throwPoint == null || cAnimation == null) return;
        float currentFaceTo = cAnimation.FlipX ? 0f : 180f;
        throwPoint.localRotation = Quaternion.Euler(0, currentFaceTo, GetAimAngle());
    }

    private void UpdateOscillatingAim()
    {
        ApplyAimRotation();
        currentAimX = GetAimAngle();
    }

    public void DrawLine()
    {
        Vector3 originPos = throwPoint.position;

        Vector3 initialVelocity = projectileSpeed * throwPoint.right;

        lineRenderer.positionCount = lineCounts;
        float time = 0;

        for (int i = 0; i < lineCounts; i++)
        {
            var x = (initialVelocity.x * time) + (Physics2D.gravity.x / 2f * time * time);
            var y = (initialVelocity.y * time) + (Physics2D.gravity.y / 2f * time * time);

            Vector3 point = new Vector3(x, y, 0);

            lineRenderer.SetPosition(i, originPos + point);

            time += timeIntervalinPoints;
        }
    }

    #endregion

    #region Skill

    public void OnSkillStateChanged() { SyncSkillUI(); }

    public override void SyncSkillUI()
    {
        if (!HasInputAuthority || PlayerInterface.Instance == null) return;

        if (isFlyUnlocked && PlayerInterface.Instance.spawnedBirdFly != null)
        {
            PlayerInterface.Instance.spawnedBirdFly.UnlockSkill();
            PlayerInterface._birdFlyUnlocked = true;
        }

        if (isThrowUnlocked && PlayerInterface.Instance.spawnedBirdThrow != null)
        {
            PlayerInterface.Instance.spawnedBirdThrow.UnlockSkill();
            PlayerInterface._birdThrowUnlocked = true;
        }
    }

    #endregion

    public override void Render()
    {
        base.Render();

        if (_prepareToThrow)
        {
            ApplyAimRotation();
            if (!lineRenderer.enabled) lineRenderer.enabled = true;
            DrawLine();
        }
        else
        {
            if (lineRenderer.enabled) lineRenderer.enabled = false;
        }

        if (!HasInputAuthority || PlayerInterface.Instance == null) return;

        if (isFlyUnlocked && PlayerInterface.Instance.spawnedBirdFly != null)
        {
            PlayerInterface.Instance.spawnedBirdFly.SetPressed(IsFlying);

            PlayerInterface.Instance.spawnedBirdFly.UpdateCooldown(FlightTimer, Runner);

            PlayerInterface.Instance.spawnedBirdFly.SetUsable(!IsAlreadyFly && !stilldrowning);
        }

        if (isThrowUnlocked && PlayerInterface.Instance.spawnedBirdThrow != null)
        {
            PlayerInterface.Instance.spawnedBirdThrow.SetPressed(_prepareToThrow);

            PlayerInterface.Instance.spawnedBirdThrow.SetUsable(_canThrowItem);
        }
    }
}