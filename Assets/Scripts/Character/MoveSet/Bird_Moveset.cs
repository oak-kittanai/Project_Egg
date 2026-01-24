using Fusion;
using UnityEngine;

public class Bird_Moveset : MovementCharacter
{
    [Header("Bird Settings")]
    [SerializeField] float normalFlyTime = 5f;
    [SerializeField] float carryFlyTime = 3f;

    [SerializeField] float floatingGravity = 0.1f;

    [Header("Bird State")]
    [Networked] private TickTimer FlightTimer { get; set; }
    [Networked] private bool IsFlying { get; set; }
    [Networked] public bool IsAlreadyFly {  get; set; }
    [Networked] public bool AlreadyFloating { get; set; }
    [Networked] public bool _wasJumpPressed { get; set; }

    protected override void OnFixedUpdateSpecific()
    {
        if (GetInput(out NetworkInputData input))
        {
            HandleFlightLogic(input);
        }

        // Update Animation
        if (cAnimation != null)
        {
            cAnimation.FlyAnimation(IsFlying);
        }

        if (IsGrounded)
        {
            IsAlreadyFly = false;
        }
    }

    private void HandleFlightLogic(NetworkInputData input)
    {
        bool isPressed = input.jump && !_wasJumpPressed;
        bool isNearPeak = Mathf.Abs(rb2D.linearVelocity.y) < 3f;

        if (isPressed && IsInAir)
        {
            if (!IsFlying && !IsAlreadyFly && isNearPeak)
            {
                StartFlying();
            }
        }

        if (isPressed)
        {
            if (AlreadyFloating)
            {
                StopFloating();
            }
            else if (IsAlreadyFly && !AlreadyFloating)
            {
                StartFloating();
            }
        }

        if (IsFlying)
        {
            if (FlightTimer.Expired(Runner))
            {
                StopFlying();
                IsAlreadyFly = true;
            }
            else
            {
                rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, stats.s_flySpeed);
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

        float duration = IsCarrying ? carryFlyTime : normalFlyTime;

        FlightTimer = TickTimer.CreateFromSeconds(Runner, duration);
        Debug.Log($"Bird Flying! Duration: {duration}s (Carrying: {IsCarrying})");
    }

    private void StopFlying()
    {
        IsFlying = false;
        FlightTimer = TickTimer.None;

    }
}