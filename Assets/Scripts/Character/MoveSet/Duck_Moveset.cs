using Fusion;
using UnityEngine;

public class Duck_Moveset : MovementCharacter
{
    [Header("Duck Setting")]
    [Networked] private TickTimer DiveCooldown { get; set; }
    [SerializeField] private float diveCooldownTimer = 2f;
    [Networked] bool ReadyToDive { get; set; }

    [Networked] bool isJumpingUp { get; set; }

    [Header("Dive Settings")]
    [SerializeField] float swimSpeed = 5f;

    [SerializeField] float swimAcceleration = 1f;
    [SerializeField] float swimDeceleration = 1f;
    [SerializeField] float swimMaxSpeed = 5f;

    [SerializeField] float divingTime = 5f;
    [SerializeField] float holdBreathTime = 5f;

    [SerializeField] bool onWater;
    [SerializeField] bool drive;
    [SerializeField] bool alreadyDive;

    [Networked] public bool onDiving { get; set; }
    [Networked] bool onDivingControl { get; set; }

    [SerializeField] float nomalSwimSpeed;
    [SerializeField] float fastSwimSpeed; // can't be turn

    [Header("Emergency Setting")]
    [Networked] bool emergencySwimBool { get; set; }

    [Networked] private TickTimer EmergencyTimer { get; set; }
    [SerializeField] private bool emergencyToggle;
    [SerializeField] private float emergencySwimTimer = 2f;

    [Networked] private TickTimer DiveTimer { get; set; }

    [Header("Etc")]
    [Networked] public bool alreadyFloating { get; set; } // Option
    [Networked] public bool _wasJumpPressed { get; set; }

    [Header("Buoyancy (Floating) Settings")]
    [SerializeField] private float buoyancyForce = 20f;
    [SerializeField] private float waterDamping = 10f;
    [SerializeField] private float floatOffset = -0.2f;

    protected override void OnFixedUpdateSpecific()
    {
        if (GetInput(out NetworkInputData input))
        {
            HandleWaterLogic(input);

            if (input.jump)
            {
                isJumpingUp = true;
            }
            else
            {
                isJumpingUp = false;
            }
        }

        if (isWaterSurface && !onDiving)
        {
            onWater = true;
            cAnimation.UpdateGroundTypeOnDuck(onWater);
            ReadyToDive = true;
        }
        else
        {
            onWater = false;
        }
        
        if (IsBodyOnWater)
        {
            HandleBuoyancy();
        }
    }

    public void HandleWaterLogic(NetworkInputData input)
    {
        bool isPressed = input.Keyboard_E && !_wasJumpPressed;

        if (!isWaterSurface && isPressed && onDiving && !IsGrounded)
        {
            EndDivingLogic();
        }

        if (isWaterSurface && isPressed && ReadyToDive)
        {
            StartDiveLogic();
        }

        if (onDiving && !emergencySwimBool)
        {
            if (DiveTimer.Expired(Runner))
            {
                EndDivingLogic();
                onDivingControl = false;
            }
            else
            {
                if (onDivingControl && stilldrowning)
                {
                    optionalGravity = 0f;
                    isOptional = true;

                    Vector2 inputDir = new Vector2(input.horizontal, input.vertical);

                    if (inputDir.sqrMagnitude > 1)
                        inputDir.Normalize();

                    Vector2 targetVel = inputDir * swimMaxSpeed;
                    Vector2 currentVel = rb2D.linearVelocity;
                    Vector2 speedDif = targetVel - currentVel;

                    float accelRate = inputDir.sqrMagnitude > 0.01f ? swimAcceleration : swimDeceleration;

                    rb2D.AddForce(speedDif * accelRate);

                    cAnimation.SwimAnimation();
                    cAnimation.UpdateSwimFlip(new Vector2(input.horizontal, input.vertical));
                    rb2D.linearDamping = 5f;
                }
                else if (!stilldrowning)
                {
                    Debug.Log("not in water");
                }
            }
        }
        else if (emergencySwimBool && onDiving)
        {
            EmergencySwimup();
        }

        _wasJumpPressed = input.jump;
    }

    public void StartDiveLogic()
    {
        if (currentWater == null) return;

        isMoveAble = false;

        rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, -swimSpeed * 1.5f);

        float impactForce = rb2D.mass * -swimSpeed;
        currentWater.Splash(transform.position, impactForce);

        onDiving = true;
        onDivingControl = true;
        emergencyToggle = true;

        float duration = divingTime;
        DiveTimer = TickTimer.CreateFromSeconds(Runner, duration);
        Debug.Log($"Duck Diving! Duration: {duration}s");
    }

    public void EndDivingLogic()
    {
        if (stilldrowning)
        {
            emergencySwimBool = true;
            if (emergencyToggle)
            {
                float duration = emergencySwimTimer;
                EmergencyTimer = TickTimer.CreateFromSeconds(Runner, duration);
                emergencyToggle = false;
            }
        }
        else
        {
            emergencySwimBool = false;
            EndDiveLogic();
        }
    }

    public void EndDiveLogic()
    {
        emergencySwimBool = false;
        isMoveAble = true;
        onDiving = false;
        isOptional = false;
        onDivingControl = false;
        rb2D.linearDamping = 0f;
        ResetDiving();
    }

    public void ResetDiving()
    {
        emergencyToggle = true;
        EmergencyTimer = TickTimer.None;
        DiveTimer = TickTimer.None;

        float duration = diveCooldownTimer;
        DiveCooldown = TickTimer.CreateFromSeconds(Runner, duration);
    }

    public void EmergencySwimup()
    {
        if (EmergencyTimer.Expired(Runner))
        {
            TimeUp();
        }
        else
        {
            if (stilldrowning)
            {
                Vector2 inputDir = new Vector2(0f, 1f);

                if (inputDir.sqrMagnitude > 1)
                    inputDir.Normalize();

                Vector2 targetVel = inputDir * swimMaxSpeed;
                Vector2 currentVel = rb2D.linearVelocity;
                Vector2 speedDif = targetVel - currentVel;

                float accelRate = inputDir.sqrMagnitude > 0.01f ? swimAcceleration : swimDeceleration;

                rb2D.AddForce(speedDif * accelRate);

                cAnimation.SwimAnimation();
            }
            else
            {
                if (onDiving && currentWater != null)
                {
                    float exitForce = rb2D.mass * rb2D.linearVelocity.y;
                    currentWater.Splash(transform.position, exitForce);
                }

                EndDiveLogic();
                Debug.Log("Reach the surface");
            }
        }
    }

    public void TimeUp()
    {
        if (!stilldrowning)
        {
            EndDiveLogic();
        }
        else
        {
            EndDiveLogic();
            Debug.Log("Dead");
        }
    }

    public void HandleBuoyancy()
    {
        if (IsBodyOnWater)
        {
            if (currentWater != null && !onDiving && !isJumpingUp)
            {
                isOptional = true;
                optionalGravity = 0f;
                rb2D.gravityScale = 0f;

                isSpeedoptional = true;

                float surfaceY = currentWater.transform.position.y;
                float targetY = surfaceY + floatOffset;
                float difference = targetY - transform.position.y;

                rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, difference * 10f);
            }
        }
        else if (currentWater == null || onDiving || isJumpingUp)
        {
            isOptional = false;
            isSpeedoptional = false;
        }
    }
}