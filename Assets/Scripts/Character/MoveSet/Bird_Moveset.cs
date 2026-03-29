using Fusion;
using UnityEngine;

public class Bird_Moveset : MovementCharacter
{
    [Header("Bird Settings")]
    [SerializeField] float normalFlyTime = 5f;
    [SerializeField] float carryFlyTime = 3f;
    [SerializeField] float floatingGravity = 0.1f;

    [Header("Physics Materials")]
    [SerializeField] PhysicsMaterial2D zeroFrictionMaterial;
    private PhysicsMaterial2D defaultMaterial;

    [Header("Bird State")]
    [Networked] private TickTimer FlightTimer { get; set; }
    [Networked] private bool IsFlying { get; set; }
    [Networked] public bool IsAlreadyFly { get; set; }
    [Networked] public bool AlreadyFloating { get; set; }
    [Networked] public bool _wasJumpPressed { get; set; }

    // Drowning
    [Networked] private TickTimer DrownTimer { get; set; }
    [SerializeField] float drowningTime = 3f;
    [SerializeField] bool startTimer;

    public override void Spawned()
    {
        base.Spawned();

        if (rb2D != null)
        {
            defaultMaterial = rb2D.sharedMaterial;
        }
    }

    protected override void OnFixedUpdateSpecific()
    {
        if (GetInput(out NetworkInputData input))
        {
            HandleFlightLogic(input);
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
                    CharacterDie();
                }
                else
                {
                    // drowning animation
                }
            }
        }
        else
        {
            startTimer = false;
        }
    }

    private void StartDrowningTimer()
    {
        startTimer = true;
        DrownTimer = TickTimer.CreateFromSeconds(Runner, drowningTime);
    }

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
                            if (duck.HasStateAuthority)
                            {
                                duck.rb2D.linearVelocity = new Vector2(duck.rb2D.linearVelocity.x, stats.s_flySpeed);
                            }
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
}