using Fusion;
using UnityEngine;

public class Bird_Moveset : MovementCharacter
{
    [Header("Bird Settings")]
    [SerializeField] float normalFlyTime = 5f;
    [SerializeField] float carryFlyTime = 3f;

    [SerializeField] float floatingGravity = 0.5f;

    [Header("Bird State")]
    [Networked] private TickTimer FlightTimer { get; set; }
    [Networked] private bool IsFlying { get; set; }
    [Networked] public bool IsAlreadyFly {  get; set; }
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
        bool isFreshPress = input.jump && !_wasJumpPressed;

        bool isNearPeak = Mathf.Abs(rb2D.linearVelocity.y) < 3f;

        if (isFreshPress && IsInAir)
        {
            if (!IsFlying && !IsAlreadyFly && isNearPeak)
            {
                StartFlying();
            }
        }

        if (IsAlreadyFly && input.jump)
        {
            StartFloating();
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

        optionalGravity = floatingGravity;
        isOptional = true;

        cAnimation.UpdateFloatingOnBird(true);
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