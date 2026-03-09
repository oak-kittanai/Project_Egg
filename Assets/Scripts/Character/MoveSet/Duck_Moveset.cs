using Fusion;
using UnityEngine;

public class Duck_Moveset : MovementCharacter
{
    [Header("Duck Setting")]
    [Networked] private TickTimer DiveCooldown { get; set; }
    [SerializeField] private float diveCooldownTimer = 2f;
    [Networked] bool ReadyToDive { get; set; }

    [Networked] bool isJumpingUp { get; set; }
    [Networked] public bool isJumpAble { get; set; }

    [SerializeField] float betweenCarryPosition = 0.63f;
    [SerializeField] float throwForceX = 4f;
    [SerializeField] float throwForceY = 4f;

    [Header("Dive Settings")]
    [SerializeField] float swimSpeed = 5f;
    [SerializeField] float swimAcceleration = 1f;
    [SerializeField] float swimDeceleration = 1f;
    [SerializeField] float swimMaxSpeed = 5f;
    [SerializeField] float divingTime = 5f;
    [SerializeField] float divePhase = 0.5f;
    [SerializeField] bool onWater;

    [SerializeField] float waterJumpForce = 12f;

    [Networked] public bool onDiving { get; set; }
    [Networked] bool onDivingControl { get; set; }
    [Networked] public bool IsAlreadyDive { get; set; }

    [Header("Emergency Setting")]
    [Networked] public bool emergencySwimBool { get; set; }

    [Networked] private TickTimer EmergencyTimer { get; set; }
    [SerializeField] private bool emergencyToggle;
    [SerializeField] private float emergencySwimTimer = 2f;

    [Networked] private TickTimer DiveTimer { get; set; }

    [Header("Etc")]
    [Networked] public bool _wasEPressed { get; set; }
    [Networked] public bool _wasFPressed { get; set; }
    [Networked] public bool _wasJumpPressed { get; set; }

    [Header("Floating Settings")]
    [SerializeField] private float floatOffset = -0.2f;

    [Header("Carry System (Duck Only)")]
    [Networked] public NetworkId CarriedFriendId { get; set; }
    [Networked] public bool IsCarrying { get; set; }

    protected override void OnFixedUpdateSpecific()
    {
        bool isJumpPressed = false;

        if (GetInput(out NetworkInputData input))
        {
            isJumpPressed = input.jump && !_wasJumpPressed;

            HandleDuckInteraction(input);
            HandleWaterLogic(input);

            if (isJumpAble)
            {
                if (input.jump) isJumpingUp = true;
                else isJumpingUp = false;
            }

            _wasEPressed = input.Keyboard_E;
            _wasFPressed = input.Keyboard_F;
            _wasJumpPressed = input.jump;
        }

        if (isWaterSurface && !onDiving)
        {
            if (!onWater)
            {
                onWater = true;
                cAnimation.UpdateGroundTypeOnDuck(true);
            }
            ReadyToDive = true;

            if (isJumpPressed)
            {
                isJumpingUp = true;
                rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, waterJumpForce);

                if (currentWater != null)
                {
                    currentWater.Splash(transform.position, rb2D.mass * waterJumpForce);
                }
            }
        }
        else
        {
            if (onWater)
            {
                onWater = false;
                cAnimation.UpdateGroundTypeOnDuck(false);
            }
        }

        if (isJumpingUp && rb2D.linearVelocity.y <= 0f)
        {
            isJumpingUp = false;
        }

        HandleBuoyancy();

        if (IsCarrying)
        {
            UpdateCarriedFriendPosition();
        }
    }

    private void HandleDuckInteraction(NetworkInputData input)
    {
        // ★ ยกเพื่อน / วางเพื่อน ใช้ปุ่ม E
        bool isEPressed = input.Keyboard_E && !_wasEPressed;

        if (isEPressed)
        {
            if (IsCarrying)
            {
                DropFriend();
                return;
            }

            Collider2D[] hitsPlayer = Physics2D.OverlapCircleAll(transform.position, playerInteractRadius);
            foreach (var hit in hitsPlayer)
            {
                if (hit.gameObject == gameObject) continue;
                if (hit.TryGetComponent<MovementCharacter>(out var otherPlayer))
                {
                    PickupFriend(otherPlayer);
                    return;
                }
            }
        }
    }

    public void PickupFriend(MovementCharacter friend)
    {
        IsCarrying = true;
        CarriedFriendId = friend.Object.Id;
        friend.SetCarriedState(true, Object.Id);

        if (IsGrounded) cAnimation.ReturnToBlendAnimation();
        else if (isWaterSurface) cAnimation.UpdateGroundTypeOnDuck(true);

        Debug.Log("Duck picked up a friend!");
    }

    public void DropFriend(bool throwFriend = true)
    {
        if (Runner.TryFindObject(CarriedFriendId, out var obj))
        {
            if (obj.TryGetComponent<MovementCharacter>(out var friend))
            {
                friend.SetCarriedState(false, default);
                if (throwFriend)
                {
                    float throwDir = cAnimation.FlipX ? 1f : -1f;

                    friend.rb2D.AddForce(new Vector2(throwDir * throwForceX, throwForceY), ForceMode2D.Impulse);
                }
            }
        }
        IsCarrying = false;
        CarriedFriendId = default;

        if (IsGrounded) cAnimation.ReturnToBlendAnimation();
        else if (isWaterSurface) cAnimation.UpdateGroundTypeOnDuck(true);
    }

    private void UpdateCarriedFriendPosition()
    {
        if (Runner.TryFindObject(CarriedFriendId, out var obj))
        {
            obj.transform.position = transform.position + Vector3.up * betweenCarryPosition;
        }
    }

    public void HandleWaterLogic(NetworkInputData input)
    {
        if (IsBeingCarried)
        {
            if (onDiving) EndDiveLogic();
            isOptional = false;
            isSpeedoptional = false;
            return;
        }

        bool isFPressed = input.Keyboard_F && !_wasFPressed;

        if (!isWaterSurface && isFPressed && onDiving && !IsGrounded && IsAlreadyDive)
        {
            EndDivingLogic();
        }

        if (isWaterSurface && isFPressed && ReadyToDive && !IsGrounded && !IsAlreadyDive)
        {
            if (!IsCarrying)
            {
                StartDiveLogic();
            }
            else
            {
                Debug.Log("can't dive because carried bird");
            }
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
                if (onDivingControl)
                {
                    optionalGravity = 0f;
                    isOptional = true;
                    isSpeedoptional = true;

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
            }
        }
        else if (emergencySwimBool && onDiving)
        {
            EmergencySwimup();
        }
    }

    public void StartDiveLogic()
    {
        if (currentWater == null) return;

        if (IsCarrying) return;

        isMoveAble = false;

        rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, -swimSpeed * divePhase);

        float impactForce = rb2D.mass;
        currentWater.Splash(transform.position, impactForce);

        onDiving = true;
        onDivingControl = true;
        emergencyToggle = true;

        DiveTimer = TickTimer.CreateFromSeconds(Runner, divingTime);
        Debug.Log($"Duck Diving! Duration: {divingTime}s");
    }

    public void EndDivingLogic()
    {
        if (stilldrowning)
        {
            emergencySwimBool = true;
            if (emergencyToggle)
            {
                EmergencyTimer = TickTimer.CreateFromSeconds(Runner, emergencySwimTimer);
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
        isSpeedoptional = false;
        onDivingControl = false;
        rb2D.linearDamping = 0f;
        ResetDiving();
        IsAlreadyDive = false;
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
        bool isBeingLiftedByBird = IsCarrying && rb2D.linearVelocity.y > 1.5f;

        if (IsBodyOnWater && currentWater != null && !onDiving && !isJumpingUp && !isBeingLiftedByBird)
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
        else if (currentWater == null || isJumpingUp || (!IsBodyOnWater && onDiving) || isBeingLiftedByBird)
        {
            isOptional = false;
            isSpeedoptional = false;
        }
    }
}