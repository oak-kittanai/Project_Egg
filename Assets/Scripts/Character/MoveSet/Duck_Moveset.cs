using Fusion;
using UnityEngine;

public class Duck_Moveset : MovementCharacter
{
    [Header("Duck Setting")]
    [Networked] bool ReadyToDive { get; set; }

    //SOUND
    [SerializeField] public AudioClip drivingSoundClip;
    [SerializeField] public AudioClip stopDrivingSoundClip;
    [SerializeField] public AudioClip pickupSoundClip;
    [SerializeField] public AudioClip dropSoundClip;

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

    [Networked, OnChangedRender(nameof(OnDivingStateChanged))]
    public NetworkBool onDiving { get; set; }
    [Networked] bool onDivingControl { get; set; }
    [Networked] bool justDive { get; set; }

    [Header("Emergency Setting")]
    [Networked] public bool emergencySwimBool { get; set; }

    [Networked] private TickTimer EmergencyTimer { get; set; }
    [Networked] public bool emergencyToggle { get; set; }
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
    [Networked, OnChangedRender(nameof(OnCarryStateChanged))]
    public NetworkBool IsCarry { get; set; }

    [Header("Unlockable Skills")]
    [Networked, OnChangedRender(nameof(OnSkillStateChanged))] public NetworkBool isDiveUnlocked { get; set; }
    [Networked, OnChangedRender(nameof(OnSkillStateChanged))] public NetworkBool isSmashUnlocked { get; set; }

    public override void Spawned()
    {
        base.Spawned();
    }

    protected override void OnFixedUpdateSpecific()
    {
        bool isJumpPressed = false;

        bool isMenuOpen = MenuController.Instance != null && MenuController.Instance.Object != null && MenuController.Instance.Object.IsValid && MenuController.Instance.IsMenuOpen;

        if (GetInput(out NetworkInputData input))
        {
            if (!isMenuOpen)
            {
                isJumpPressed = input.KeybindJump && !_wasJumpPressed;

                HandleDuckInteraction(input);
                HandleWaterLogic(input);

                if (isJumpAble)
                {
                    if (input.KeybindJump) isJumpingUp = true;
                    else isJumpingUp = false;
                }
            }

            _wasEPressed = input.KeybindInteract;
            _wasFPressed = input.Keyboard_F;
            _wasJumpPressed = input.KeybindJump;
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
        bool isEPressed = input.KeybindInteract && !_wasEPressed;

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

        if (friend.rb2D != null) friend.rb2D.bodyType = RigidbodyType2D.Kinematic;
        if (friend.coll2D != null) friend.coll2D.isTrigger = true;

        if (carryCollider != null && friend.coll2D != null) Physics2D.IgnoreCollision(carryCollider, friend.coll2D, true);
        if (normalCollider != null && friend.coll2D != null) Physics2D.IgnoreCollision(normalCollider, friend.coll2D, true);

        if (normalCollider != null) normalCollider.enabled = false;
        if (carryCollider != null) carryCollider.enabled = true;

        friend.RPC_UpdateCarry(true, Object.Id);
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

                    Vector2 throwSpawnPos = (Vector2)friend.transform.position;

                    friend.localIsBeingCarriedPredict = false;
                    if (friend.rb2D != null) friend.rb2D.bodyType = RigidbodyType2D.Dynamic;
                    if (friend.coll2D != null) friend.coll2D.isTrigger = false;
                    if (carryCollider != null && friend.coll2D != null) Physics2D.IgnoreCollision(carryCollider, friend.coll2D, false);
                    if (normalCollider != null && friend.coll2D != null) Physics2D.IgnoreCollision(normalCollider, friend.coll2D, false);

                    friend.RPC_UpdateCarry(false, Object.Id, throwFriend, throwDir, throwForceX, throwForceY, throwSpawnPos);
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

    public void OnCarryStateChanged()
    {
        if (Object.InputAuthority == Runner.LocalPlayer)
        {
            if (IsCarry)
            {
                if (playerAudioSource != null && pickupSoundClip != null) playerAudioSource.PlayOneShot(pickupSoundClip);
            }
            else
            {
                if (playerAudioSource != null && dropSoundClip != null) playerAudioSource.PlayOneShot(dropSoundClip);
            }
        }
    }

    public void HandleWaterLogic(NetworkInputData input)
    {
        if (!isDiveUnlocked) return;

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
                    rb2D.gravityScale = 0f;

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
    }

    public void OnDivingStateChanged()
    {
        if (Object.InputAuthority == Runner.LocalPlayer)
        {
            if (onDiving)
            {
                if (playerAudioSource != null && drivingSoundClip != null) playerAudioSource.PlayOneShot(drivingSoundClip);
                if (localGUI != null) localGUI.StartOxygenTracking(DiveTimer, Runner, Mathf.CeilToInt(divingTime));
            }
            else
            {
                if (playerAudioSource != null && stopDrivingSoundClip != null) playerAudioSource.PlayOneShot(stopDrivingSoundClip);
                if (localGUI != null) localGUI.StopOxygenTracking();
            }
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
    }

    public void EmergencySwimup()
    {
        if (EmergencyTimer.Expired(Runner))
        {
            TimeUp();
        }
        else
        {
            if (!stilldrowning)
            {
                EndDiveLogic();
                return;
            }

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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void PlayHitAnimation_RPC()
    {
        if (cAnimation != null)
        {
            cAnimation.SmashAnimation();
            Debug.Log("Try Smash Animation");
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
            DeathMechanic_RPC(true);
            Debug.Log("Dead");
        }
    }

    public void HandleBuoyancy()
    {
        bool isBeingLiftedByBird = false;
        if (IsCarry && Runner.TryFindObject(CarriedFriendId, out var friendObj) && friendObj.TryGetComponent<Bird_Moveset>(out var bird))
        {
            isBeingLiftedByBird = bird.IsFlying;
        }

        bool canApplyBuoyancy = IsBodyOnWater && currentWater != null && !onDiving && !isJumpingUp && !isBeingLiftedByBird && !stilldrowning;

        if (canApplyBuoyancy)
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
        else if (currentWater == null || isJumpingUp || (!IsBodyOnWater && onDiving) || isBeingLiftedByBird || stilldrowning)
        {
            if (!onDiving)
            {
                isOptional = false;
                isSpeedoptional = false;
                rb2D.gravityScale = normalGravity;
            }
        }
    }

    #region Skill

    public void UnlockDiveSkill()
    {
        isDiveUnlocked = true;
        PlayerInterface.Instance?.UnlockDuckDive();
    }

    public void UnlockSmashSkill()
    {
        isSmashUnlocked = true;
        PlayerInterface.Instance?.UnlockDuckSmash();
    }

    public void OnSkillStateChanged() { SyncSkillUI(); }

    public override void SyncSkillUI()
    {
        if (!HasInputAuthority || PlayerInterface.Instance == null) return;

        if (isDiveUnlocked && PlayerInterface.Instance.spawnedDuckDive != null)
        {
            PlayerInterface.Instance.spawnedDuckDive.UnlockSkill();
            PlayerInterface._duckDiveUnlocked = true;
        }

        if (isSmashUnlocked && PlayerInterface.Instance.spawnedDuckSmash != null)
        {
            PlayerInterface.Instance.spawnedDuckSmash.UnlockSkill();
            PlayerInterface._duckSmashUnlocked = true;
        }
    }

    #endregion

    public override void Render()
    {
        base.Render();

        if (!HasInputAuthority || PlayerInterface.Instance == null) return;

        if (isDiveUnlocked && PlayerInterface.Instance.spawnedDuckDive != null)
        {
            PlayerInterface.Instance.spawnedDuckDive.SetPressed(onDiving);

            PlayerInterface.Instance.spawnedDuckDive.SetUsable(isWaterSurface);
        }

        if (isSmashUnlocked && PlayerInterface.Instance.spawnedDuckSmash != null)
        {
            PlayerInterface.Instance.spawnedDuckSmash.SetPressed(_isEPressed);

            PlayerInterface.Instance.spawnedDuckSmash.SetUsable(isNearBreakableRock);
        }
    }
}