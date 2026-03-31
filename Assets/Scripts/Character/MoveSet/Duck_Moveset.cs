using Fusion;
using UnityEngine;

public class Duck_Moveset : MovementCharacter
{
    [Header("Duck Setting")]
    [Networked] bool ReadyToDive { get; set; }

    [Networked] bool isJumpingUp { get; set; }
    [Networked] public bool isJumpAble { get; set; }

    [Header("Water Jump Setting")]
    [Networked] private TickTimer WaterJumpCooldownTimer { get; set; }
    [SerializeField] private float waterJumpCooldown = 1.5f;

    [Header("Carry setting")]
    [SerializeField] public float throwForceX = 4f;
    [SerializeField] public float throwForceY = 4f;

    [Header("Dive Settings")]
    [SerializeField] float swimSpeed = 5f;
    [SerializeField] float swimAcceleration = 1f;
    [SerializeField] float swimDeceleration = 1f;
    [SerializeField] float swimMaxSpeed = 5f;
    [SerializeField] float divingTime = 5f;
    [SerializeField] float divePhase = 0.5f;

    [SerializeField] float emergencyAcceleration = 1f;
    [SerializeField] float emergencyDeceleration = 2f;

    [Networked] public bool onDiving { get; set; }
    [Networked] bool onDivingControl { get; set; }
    [Networked] bool justDive { get; set; }

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
    [Networked] public bool IsCarry { get; set; }

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
            cAnimation.UpdateGroundTypeOnDuck(true);
            ReadyToDive = true;

            if (isJumpPressed && WaterJumpCooldownTimer.ExpiredOrNotRunning(Runner))
            {
                HandleJumpOffWater();
            }
        }
        else if (!onDiving && !isJumping && IsGrounded)
        {
            cAnimation.ReturnToBlendAnimation();
        }

        if (IsHeadUnderwater && !justDive && onDiving)
        {
            justDive = true;
        }
        else if (!IsHeadUnderwater && !onDiving && isWaterSurface)
        {
            justDive = false;
        }

        if (isJumpingUp && rb2D.linearVelocity.y <= 0f)
        {
            isJumpingUp = false;
        }

        HandleBuoyancy();
    }

    private void HandleJumpOffWater()
    {
        isJumpingUp = true;

        float normalJumpForce = stats.s_jumpForce;

        rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, normalJumpForce);

        if (currentWater != null)
        {
            currentWater.Splash(transform.position, rb2D.mass * normalJumpForce);
        }

        WaterJumpCooldownTimer = TickTimer.CreateFromSeconds(Runner, waterJumpCooldown);
    }

    private void HandleDuckInteraction(NetworkInputData input)
    {
        bool isEPressed = input.Keyboard_E && !_wasEPressed;

        if (isEPressed)
        {
            if (IsCarry)
            {
                DropFriend();
                return;
            }

            Collider2D[] hitsPlayer = Physics2D.OverlapCircleAll(transform.position, playerInteractRadius);
            foreach (var hit in hitsPlayer)
            {
                if (hit.gameObject == gameObject) continue;

                MovementCharacter[] allCharacters = hit.GetComponents<MovementCharacter>();

                foreach (var character in allCharacters)
                {
                    if (character.enabled == true)
                    {
                        PickupFriend(character);
                        return;
                    }
                }
            }
        }
    }

    public void PickupFriend(MovementCharacter friend)
    {
        IsCarry = true;
        CarriedFriendId = friend.Object.Id;

        friend.localIsBeingCarriedPredict = true;
        friend.localCarrierIdPredict = Object.Id;

        friend.RPC_UpdateCarry(true, Object.Id);

        if (normalCollider != null) normalCollider.enabled = false;
        if (carryCollider != null) carryCollider.enabled = true;

        resetAnimation = true;
    }

    public void DropFriend(bool throwFriend = true)
    {
        if (Runner.TryFindObject(CarriedFriendId, out var obj))
        {
            MovementCharacter[] allCharacters = obj.GetComponents<MovementCharacter>();
            foreach (var friend in allCharacters)
            {
                if (friend.enabled)
                {
                    float throwDir = cAnimation.FlipX ? 1f : -1f;
                    friend.localIsBeingCarriedPredict = false;

                    if (throwFriend && friend.visualTransform != null)
                    {
                        friend.visualTransform.position = transform.position + new Vector3(throwDir * 1f, 1f, 0);
                    }

                    friend.RPC_UpdateCarry(false, Object.Id, throwFriend, throwDir, throwForceX, throwForceY);
                    break;
                }
            }
        }

        if (normalCollider != null) normalCollider.enabled = true;
        if (carryCollider != null) carryCollider.enabled = false;

        IsCarry = false;
        CarriedFriendId = default;

        resetAnimation = true;
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

        if (!isWaterSurface && isFPressed && onDiving && !IsGrounded)
        {
            EndDivingLogic();
        }

        if (isWaterSurface && isFPressed && ReadyToDive && !IsGrounded)
        {
            if (!IsCarry || !onDiving)
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
            else if (!IsHeadUnderwater && onDiving && !DiveTimer.Expired(Runner) && justDive)
            {
                EndDivingLogic();
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
        if (IsCarry) return;

        isMoveAble = false;
        ReadyToDive = false;

        rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, -swimSpeed * divePhase);

        float impactForce = rb2D.mass;
        currentWater.Splash(transform.position, impactForce);

        onDiving = true;
        onDivingControl = true;
        emergencyToggle = true;

        DiveTimer = TickTimer.CreateFromSeconds(Runner, divingTime);
        Debug.Log($"Duck Diving! Duration: {divingTime}s");

        if (localGUI != null)
        {
            localGUI.StartOxygenTracking(DiveTimer, Runner, Mathf.CeilToInt(divingTime));
        }
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
    }

    public void ResetDiving()
    {
        emergencyToggle = true;
        ReadyToDive = true;
        EmergencyTimer = TickTimer.None;
        DiveTimer = TickTimer.None;

        if (localGUI != null)
        {
            localGUI.StopOxygenTracking();
        }
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

                float accelRate = inputDir.sqrMagnitude > 0.01f ? emergencyAcceleration : swimDeceleration;

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
            CharacterDie();
            Debug.Log("Dead");
        }
    }

    public void HandleBuoyancy()
    {
        bool isBeingLiftedByBird = IsCarry && rb2D.linearVelocity.y > 1.5f;

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

            rb2D.gravityScale = normalGravity;
        }
    }

    public override void Render()
    {
        base.Render();
    }
}