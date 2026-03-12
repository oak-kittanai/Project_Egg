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

        if (cAnimation != null)
        {
            cAnimation.FlyAnimation(IsFlying);
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
                if (duck.IsGrounded || duck.isWaterSurface)
                {
                    IsAlreadyFly = false;
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

            rb2D.linearVelocity = new Vector2(0f, -1.5f);

            if (!startTimer)
            {
                StartDrowningTimer();
            }

            if (startTimer)
            {
                if (DrownTimer.Expired(Runner))
                {
                    startTimer = false;
                    Die();
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
        bool isNearPeak = Mathf.Abs(rb2D.linearVelocity.y) < 3f;

        if (!IsBeingCarried)
        {
            if (isPressed && IsInAir)
            {
                if (!IsFlying && !IsAlreadyFly && isNearPeak)
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

        if (isPressed && IsAlreadyFly)
        {
            if (AlreadyFloating) StopFloating();
            else if (!AlreadyFloating) StartFloating();
        }

        if (IsFlying)
        {
            if (FlightTimer.Expired(Runner))
            {
                StopFlying();
                IsAlreadyFly = true;

                if (IsBeingCarried)
                {
                    cAnimation.ReturnToBlendAnimation();
                }
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
        cAnimation.UpdateFloatingOnBird(true);
        rb2D.linearDamping = 5f;
    }

    private void StopFloating()
    {
        FallingBusy = false;
        AlreadyFloating = false;
        isOptional = false;
        rb2D.linearDamping = 0f;
        rb2D.gravityScale = normalGravity;
        cAnimation.UpdateFloatingOnBird(false);
    }

    private void StartFlying()
    {
        IsFlying = true;
        float duration = IsBeingCarried ? carryFlyTime : normalFlyTime;
        FlightTimer = TickTimer.CreateFromSeconds(Runner, duration);

        if (HasInputAuthority || (HasStateAuthority && Runner.LocalPlayer == Object.StateAuthority))
        {
            if (PlayerGUI.Instance != null)
            {
                PlayerGUI.Instance.StartFlightBar(FlightTimer, Runner, duration);
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

        if (HasInputAuthority || (HasStateAuthority && Runner.LocalPlayer == Object.StateAuthority))
        {
            if (PlayerGUI.Instance != null)
            {
                PlayerGUI.Instance.StopFlightBar();
            }
        }

        if (rb2D != null)
        {
            rb2D.sharedMaterial = defaultMaterial;
        }
    }
}