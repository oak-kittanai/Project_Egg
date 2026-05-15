using Fusion;
using System;
using UnityEngine;

public class Bird_Moveset : MovementCharacter
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
    [Networked] private bool IsFlying { get; set; }
    [Networked] public bool IsAlreadyFly { get; set; }
    [Networked] public bool AlreadyFloating { get; set; }
    

    [Header("Pressed")]
    [Networked] public bool _wasJumpPressed { get; set; }
    [Networked] public bool _wasXPressed { get; set; }
    [Networked] public bool _wasZPressed { get; set; }

    // Drowning
    [Networked] private TickTimer DrownTimer { get; set; }
    [SerializeField] float drowningTime = 3f;
    [Networked] public bool startTimer { get; set; }

    [Header("ThrowSystem")]
    [SerializeField] public NetworkObject throwAblePrefab;
    [Networked] public bool _prepareToThrow { get; set; }

    [SerializeField] float projectileSpeed;
    [SerializeField] Transform throwPoint;
    // Line
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] int lineCounts;
    [SerializeField] float timeIntervalinPoints = 0.01f;
    [SerializeField] float currentAimX;
    // Sweeping Aim
    [SerializeField] float aimSweepSpeed = 3f;
    [SerializeField] float maxAimAngle = 30f;

    public override void Spawned()
    {
        base.Spawned();

        if (rb2D != null)
        {
            defaultMaterial = rb2D.sharedMaterial;
        }

        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null) Debug.Log("LineRenderer Found");
    }

    protected override void OnFixedUpdateSpecific()
    {
        if (GetInput(out NetworkInputData input))
        {
            HandleFlightLogic(input);
            HandleThrowLogic(input);
        }

        if (IsGrounded)
        {
            IsAlreadyFly = false;

            if (AlreadyFloating)
            {
                StopFloating();
            }

            if (!IsFlying && rb2D != null && rb2D.sharedMaterial != defaultMaterial)
            {
                rb2D.sharedMaterial = defaultMaterial;
            }
        }

        if (IsBeingCarried)
        {
            if (AlreadyFloating)
            {
                StopFloating();
            }

            if (Runner.TryFindObject(CarrierId, out var duckObj) && duckObj.TryGetComponent<MovementCharacter>(out var duck))
            {
                if (duck.IsGrounded || duck.isWaterSurface && !IsFlying)
                {
                    IsAlreadyFly = false;
                    resetAnimation = true;
                }
                else if (duck.IsHeadUnderwater) { /* do drowning but duck carry animation */ }

                if (HasStateAuthority || HasInputAuthority)
                {
                    cAnimation.FlipX = duck.cAnimation.FlipX;
                }
            }
        }

        HandleDrowning();
    }

    private void HandleDrowning()
    {
        if (!IsBeingCarried && (isWaterSurface || stilldrowning))
        {
            isMoveAble = false;
            isOptional = true;
            optionalGravity = 0f;

            rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, -1.5f);

            if (!startTimer)
            {
                StartDrowningTimer();
            }

            if (startTimer)
            {
                if (DrownTimer.Expired(Runner))
                {
                    startTimer = false;
                    DeathMechanic_RPC();
                }
                else
                {
                    // drowning animation
                }
            }
        }
        else if (startTimer)
        {
            DrownTimer = TickTimer.None;
            startTimer = false;

            if (Runner.IsForward && localGUI != null)
            {
                localGUI.StopOxygenTracking();
            }
        }
    }

    private void StartDrowningTimer()
    {
        startTimer = true;
        DrownTimer = TickTimer.CreateFromSeconds(Runner, drowningTime);

        if (localGUI != null)
        {
            localGUI.StartOxygenTracking(DrownTimer, Runner, Mathf.CeilToInt(drowningTime));
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
    }

    #region FlyLogic
    private void HandleFlightLogic(NetworkInputData input)
    {
        bool isPressed = input.jump && !_wasJumpPressed;

        if (!IsBeingCarried)
        {
            if (isPressed && IsInAir)
            {
                if (!IsFlying && !IsAlreadyFly)
                {
                    StartFlying();
                }
            }
        }

        if (IsBeingCarried)
        {
            isMoveAble = false;
            if (isPressed)
            {
                if (!IsFlying && !IsAlreadyFly)
                {
                    StartFlying();
                }
            }
        }

        if (IsBeingCarried && IsAlreadyFly && !IsFlying)
        {
            if (Runner.TryFindObject(CarrierId, out var carrierObj))
            {
                if (carrierObj.TryGetComponent<MovementCharacter>(out var duck))
                {
                    if (duck.IsGrounded)
                    {
                        resetAnimation = true;
                    }
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
                    if (Runner.TryFindObject(CarrierId, out var carrierObj))
                    {
                        if (carrierObj.TryGetComponent<MovementCharacter>(out var duck))
                        {
                            duck.rb2D.linearVelocity = new Vector2(duck.rb2D.linearVelocity.x, stats.s_flySpeed);
                        }
                    }
                }
            }
        }

        _wasJumpPressed = input.jump;
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
        if (HasInputAuthority)
        {
            if (playerAudioSource != null && flySoundClip != null)
            {
                playerAudioSource.PlayOneShot(flySoundClip);
            }
        }

        float duration = IsBeingCarried ? carryFlyTime : normalFlyTime;
        FlightTimer = TickTimer.CreateFromSeconds(Runner, duration);

        cAnimation.FlyUpAnimation();

        if (HasInputAuthority)
        {
            if (localGUI != null)
            {
                localGUI.StartFlightBar(FlightTimer, Runner, duration);
            }
        }

        if (rb2D != null && zeroFrictionMaterial != null)
        {
            rb2D.sharedMaterial = zeroFrictionMaterial;
        }

        Debug.Log($"Bird Flying! Duration: {duration}s (Being Carried: {IsBeingCarried})");
    }

    private void StopFlying()
    {
        IsFlying = false;
        if (HasInputAuthority)
        {
            if (playerAudioSource != null && stopFlySoundClip != null)
            {
                playerAudioSource.PlayOneShot(stopFlySoundClip);
            }
        }
        FlightTimer = TickTimer.None;

        if (HasInputAuthority)
        {
            if (localGUI != null)
            {
                localGUI.StopFlightBar();
            }
        }

        if (rb2D != null)
        {
            rb2D.sharedMaterial = defaultMaterial;
        }
    }
    #endregion

    #region ThrowLogic

    public void HandleThrowLogic(NetworkInputData input)
    {
        bool isXPressed = input.Keyboard_X && !_wasXPressed;
        bool isZPressed = input.Keyboard_Z && !_wasZPressed;

        if (isXPressed)
        {
            if (_canThrowItem && !(isWaterSurface || stilldrowning))
            {
                _prepareToThrow = !_prepareToThrow;

                if (!_prepareToThrow)
                {
                    CancelThrow();
                }
            }
            else
            {
                if (_prepareToThrow)
                {
                    CancelThrow();
                }
            }
        }

        float inputAD = input.horizontal;

        if (_prepareToThrow)
        {
            isMoveAble = false;
            cAnimation.FaceTo(inputAD);
            UpdateOscillatingAim();
            IsInteractBusy = true;

            if (isZPressed)
            {
                ExecuteThrow();
            }
        }

        _wasXPressed = input.Keyboard_X;
        _wasZPressed = input.Keyboard_Z;
    }

    private void ExecuteThrow()
    {
        Vector2 throwPos = throwPoint.position;
        Vector2 direction = throwPoint.right;

        GameManager.Instance.ProjectileSpawn(throwAblePrefab, throwPos, direction, throwPoint.rotation, projectileSpeed);
        if (HasInputAuthority)
        {
            if (playerAudioSource != null && throwSoundClip != null)
            {
                playerAudioSource.PlayOneShot(throwSoundClip);
            }
        }

        _canThrowItem = false;

        CancelThrow();
    }

    private void CancelThrow()
    {
        isMoveAble = true;
        _prepareToThrow = false;
        IsInteractBusy = false;

        throwPoint.localRotation = Quaternion.identity;
    }

    private void UpdateOscillatingAim()
    {
        float time = (float)Runner.SimulationTime;

        float currentAngle = Mathf.Sin(time * aimSweepSpeed) * maxAimAngle;
        float currentFaceTo = cAnimation.FlipX ? 0f : 180f;

        currentAimX = currentAngle;

        throwPoint.localRotation = Quaternion.Euler(0, currentFaceTo, currentAngle);
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

    public override void Render()
    {
        base.Render();

        if (_prepareToThrow)
        {
            if (!lineRenderer.enabled) lineRenderer.enabled = true;
            DrawLine();
        }
        else
        {
            if (lineRenderer.enabled) lineRenderer.enabled = false;
        }
    }
}

