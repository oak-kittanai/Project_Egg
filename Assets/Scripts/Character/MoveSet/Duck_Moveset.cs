using Fusion;
using UnityEngine;

public class Duck_Moveset : MovementCharacter
{
    [Header("Duck Setting")]
    [Networked] private TickTimer DiveCooldown { get; set; }
    [SerializeField] private float diveCooldownTimer = 2f;
    [Networked] bool ReadyToDive { get; set; }

    [Header("Dive Settings")]
    private NetworkInteractableWater currentWater;

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
    [Networked] bool stilldrowning { get; set; }

    [Networked] bool emergencySwimBool { get; set; }

    [Networked] private TickTimer EmergencyTimer { get; set; }
    [SerializeField] private bool emergencyToggle;
    [SerializeField] private float emergencySwimTimer = 2f;

    [Networked] private TickTimer DiveTimer { get; set; }

    [Header("Etc")]
    [Networked] public bool alreadyFloating { get; set; } // Option

    [Networked] public bool _wasJumpPressed { get; set; }

    // Head
    public float headOffset = 0.2f;
    [Networked] public bool IsHeadUnderwater { get; set; }
    protected override void OnFixedUpdateSpecific()
    {
        if (GetInput(out NetworkInputData input))
        {
            HandleWaterLogic(input);
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

        CheckWaterZone();
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
            // try dive
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

                    // make the duck controll the dive
                    Vector2 inputDir = new Vector2(input.horizontal, input.vertical);

                    /*if (inputDir.sqrMagnitude > 0.1f) Need to fix
                    {
                        float targetAngle = Mathf.Atan2(inputDir.y, inputDir.x) * Mathf.Rad2Deg;

                        float nextAngle = Mathf.LerpAngle(rb2D.rotation, targetAngle, 10f * Runner.DeltaTime);

                        rb2D.MoveRotation(nextAngle);
                    }*/

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

        isMoveAble = false; // make the Movement Logic stop

        // make the character dive into the water
        rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, -swimSpeed * 1.5f);

        // Trigger the Splash Effect
        float impactForce = rb2D.mass * -swimSpeed;
        currentWater.RPC_Splash(transform.position, impactForce);

        onDiving = true;
        onDivingControl = true;
        //cAnimation.SetFlipToFalse();
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

                /*if (inputDir.sqrMagnitude > 0.1f)
                {
                    float targetAngle = Mathf.Atan2(inputDir.y, inputDir.x) * Mathf.Rad2Deg;

                    float nextAngle = Mathf.LerpAngle(rb2D.rotation, targetAngle, 10f * Runner.DeltaTime);

                    rb2D.MoveRotation(nextAngle);
                }*/

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
                    // Positive velocity for exit splash
                    float exitForce = rb2D.mass * rb2D.linearVelocity.y;
                    currentWater.RPC_Splash(transform.position, exitForce);
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
            // RIP
            EndDiveLogic();
            Debug.Log("Dead");
        }
    }

    public void CheckWaterZone()
    {
        LayerMask waterMask = LayerMask.GetMask("Water");
        Vector2 headPosition = (Vector2)transform.position + (Vector2.up * headOffset);

        Collider2D bodyCollider = Physics2D.OverlapCircle(transform.position, 0.5f, waterMask);
        IsHeadUnderwater = Physics2D.OverlapPoint(headPosition, waterMask);

        if (bodyCollider != null)
        {
            if (currentWater == null || currentWater.gameObject != bodyCollider.gameObject)
            {
                currentWater = bodyCollider.GetComponent<NetworkInteractableWater>();
                if (currentWater == null) currentWater = bodyCollider.GetComponentInParent<NetworkInteractableWater>();
            }
        }
        else
        {
            currentWater = null;
        }

        bool isBodyInWater = bodyCollider != null;

        if (isBodyInWater && !IsHeadUnderwater)
        {
            stilldrowning = false;
        }
        else if (isBodyInWater && IsHeadUnderwater)
        {
            stilldrowning = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.orange;
        Vector2 start = transform.position;
        Vector2 direction = Vector2.up * headOffset;
        Gizmos.DrawRay(start, direction);
    }
}