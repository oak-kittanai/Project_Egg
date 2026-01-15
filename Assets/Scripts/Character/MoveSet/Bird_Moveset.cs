using Fusion;
using UnityEngine;

public class Bird_Moveset : MovementCharacter
{
    [Header("Bird Settings")]
    [SerializeField] float normalFlyTime = 5f;
    [SerializeField] float carryFlyTime = 3f;

    [Header("Bird State")]
    [Networked] private TickTimer FlightTimer { get; set; }
    [Networked] private bool IsFlying { get; set; }
    [Networked] public bool IsAlreadyFly {  get; set; }

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
        if (input.jump && IsInAir && !IsFlying && FlightTimer.ExpiredOrNotRunning(Runner) && !IsAlreadyFly)
        {
            StartFlying();
        }

        if (IsFlying)
        {
            if (FlightTimer.Expired(Runner) || !input.jump)
            {
                StopFlying();
            }
            else
            {
                Rigidbody2D rb = GetComponent<Rigidbody2D>();
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, stats.s_flySpeed);

                stats.ReduceStamina(5f);
            }
        }
    }

    private void StartFlying()
    {
        IsAlreadyFly = true;
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