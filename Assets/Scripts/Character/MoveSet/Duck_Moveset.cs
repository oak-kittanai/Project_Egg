using Fusion;
using UnityEngine;

public class Duck_Moveset : MovementCharacter, IstunAble
{
    [Header("Duck Setting")]
    bool ReadyToDive;

    //SOUND
    [SerializeField] public AudioClip drivingSoundClip;
    [SerializeField] public AudioClip stopDrivingSoundClip;
    [SerializeField] public AudioClip pickupSoundClip;
    [SerializeField] public AudioClip dropSoundClip;

    bool isJumpingUp;
    public bool isJumpAble { get; set; }

    [Header("Water Jump Setting")]
    private TickTimer WaterJumpCooldownTimer;
    [SerializeField] private float waterJumpCooldown = 1.5f;

    [Header("Carry setting")]
    [SerializeField] public float throwForceX = 4f;
    [SerializeField] public float throwForceY = 4f;
    [Tooltip("time (sec) delay carry")]
    [SerializeField] private float pickupCooldown = 1f;
    [Networked] private TickTimer PickupCooldownTimer { get; set; }

    [Header("Dive Settings")]
    [SerializeField] float swimSpeed = 5f;
    [SerializeField] float swimAcceleration = 1f;
    [SerializeField] float swimDeceleration = 1f;
    [SerializeField] float swimMaxSpeed = 5f;
    [SerializeField] float divingTime = 5f;
    [SerializeField] float divePhase = 0.5f;

    [SerializeField] float emergencyAcceleration = 1f;

    // ดำน้ำอยู่ — สถานะของเจ้าของตัว (NR-12); เสียง/แถบ oxygen ของเจ้าของเรียกตอนค่าเปลี่ยน
    public bool onDiving
    {
        get => GetMoveFlag(MoveFlags.Diving);
        set
        {
            bool changed = IsOwner && GetMoveFlag(MoveFlags.Diving) != value;
            SetMoveFlag(MoveFlags.Diving, value);
            if (changed) OnDivingStateChanged();
        }
    }
    bool onDivingControl;
    bool justDive;

    [Header("Emergency Setting")]
    public bool emergencySwimBool { get; set; }

    private TickTimer EmergencyTimer;
    public bool emergencyToggle { get; set; }
    [SerializeField] private float emergencySwimTimer = 2f;

    private TickTimer DiveTimer;
    private bool _remoteDiving; // เครื่องที่ไม่ใช่เจ้าของตัว: ค่า onDiving ล่าสุดที่แสดงแล้ว (OX-1)

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

    public override void OnRespawned()
    {
        base.OnRespawned();

        // เคลียร์ state ดำน้ำ/ลอยน้ำที่ base ไม่ได้รีเซ็ต — กันค่าค้างหลังตายแบบจมน้ำ
        // (onDiving/onDivingControl ค้าง -> HandleWaterLogic/HandleBuoyancy ใส่ค่าลอยน้ำกลับ
        //  (isSpeedoptional=true, gravityScale=0) -> เกิดใหม่แล้วยกนก เป็ดลื่น/วิ่งเร็วเหมือนยังอยู่ในน้ำ)
        isOptional = false;
        isSpeedoptional = false;

        if (IsOwner)
        {
            onDiving = false;
            onDivingControl = false;
            emergencySwimBool = false;
            emergencyToggle = true;
            ReadyToDive = true;
            DiveTimer = TickTimer.None;
            EmergencyTimer = TickTimer.None;
        }
    }

    public void TriggerStun()
    {
        ApplyStun();
    }

    // เจ้าของตัว: น้ำ/ดำน้ำ/ลอยน้ำ/กระโดดจากน้ำ, collider ตอนยก, ลอยตามนกที่ถูกยกแล้วบิน (NR-12)
    protected override void OnOwnerSimulateSpecific(NetworkInputData input, bool hasInput)
    {
        bool isJumpPressed = false;

        bool isMenuOpen = GameManager.Instance != null && GameManager.Instance.IsPaused;

        if (hasInput && !isMenuOpen)
        {
            isJumpPressed = WasPressed(input, InputButton.Jump);

            HandleWaterLogic(input);

            if (isJumpAble)
            {
                if (input.KeybindJump) isJumpingUp = true;
                else isJumpingUp = false;
            }
        }

        SyncCarryColliders();
        LiftWithFlyingFriend();

        if (isWaterSurface && !onDiving)
        {
            ReadyToDive = true;

            if (isJumpPressed && WaterJumpCooldownTimer.ExpiredOrNotRunning(Runner))
            {
                HandleJumpOffWater();
            }
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

        if (!isMenuOpen)
        {
            HandleBuoyancy();

            // ขึ้นจากน้ำแล้ว -> บอก host ให้หยุดดาเมจจมน้ำ (host เป็นคนทำดาเมจ)
            if (!stilldrowning) SetMoveFlag(MoveFlags.OxygenDepleted, false);
        }
    }

    // host: ยก/ปล่อยนก + ดาเมจจมน้ำตามธง oxygen หมดที่เจ้าของส่งมา
    protected override void OnHostRulesSpecific(NetworkInputData input, bool hasInput, bool isMenuOpen)
    {
        if (hasInput && !isMenuOpen) HandleDuckInteraction(input);

        if (!isMenuOpen)
        {
            if (!stilldrowning) StopDrowningDamage();
            else if (GetMoveFlag(MoveFlags.OxygenDepleted)) StartDrowningDamage();
            TickDrowningDamage();
        }
    }

    // collider ตอนยก: ปิดตัวปกติ เปิดตัวยก (สูงกว่า) — ทุกเครื่องทำตาม IsCarry ที่ host ตั้ง
    private void SyncCarryColliders()
    {
        bool carrying = IsCarry;

        if (normalCollider != null && normalCollider.enabled == carrying) normalCollider.enabled = !carrying;

        if (carryCollider != null && carryCollider.enabled != carrying)
        {
            carryCollider.enabled = carrying;

            if (carrying && Runner.TryFindObject(CarriedFriendId, out var fObj)
                && fObj.TryGetComponent<MovementCharacter>(out var f) && f.coll2D != null)
            {
                Physics2D.IgnoreCollision(carryCollider, f.coll2D, true);
            }
        }
    }

    // นกที่ถูกยกกดบิน -> เป็ดลอยขึ้นตาม (เดิมนกบน host สั่งความเร็วเป็ดข้ามตัว — ตอนนี้เป็ดขยับตัวเองจาก IsFlying ของนก)
    private void LiftWithFlyingFriend()
    {
        Bird_Moveset flyingBird = null;
        if (IsCarry && Runner.TryFindObject(CarriedFriendId, out var fObj)
            && fObj.TryGetComponent<Bird_Moveset>(out var bird) && bird.IsFlying)
        {
            flyingBird = bird;
        }

        if (flyingBird != null)
        {
            rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, flyingBird.stats.s_flySpeed);
        }

        if (carryCollider != null)
        {
            PhysicsMaterial2D wanted = flyingBird != null ? flyingBird.ZeroFrictionMaterial : null;
            if (carryCollider.sharedMaterial != wanted) carryCollider.sharedMaterial = wanted;
        }
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
        bool isEPressed = WasPressed(input, InputButton.Interact);

        if (isEPressed)
        {
            if (IsCarry)
            {
                DropFriend();
                return;
            }

            // กดเดียวกันไปทุบหิน/ใช้ของที่ใกล้กว่าแล้ว -> ไม่ยกนก (IN-1)
            if (InteractUsedOnWorldThisTick) return;

            Bird_Moveset birdToCarry = FindLiftableBird(out _);
            if (birdToCarry != null) PickupFriend(birdToCarry);
        }
    }

    // นกในระยะยกที่ใกล้ที่สุด — ติด cooldown หลังพึ่งปล่อยนกถือว่ายังยกไม่ได้ (แต่ปล่อยข้างบนไม่ติด cooldown)
    private Bird_Moveset FindLiftableBird(out float distance)
    {
        distance = float.MaxValue;
        if (!PickupCooldownTimer.ExpiredOrNotRunning(Runner)) return null;

        Bird_Moveset closest = null;
        Collider2D[] hitsPlayer = Physics2D.OverlapCircleAll(transform.position, playerInteractRadius);
        foreach (var hit in hitsPlayer)
        {
            if (hit.gameObject == gameObject) continue;
            if (!hit.TryGetComponent<Bird_Moveset>(out var bird)) continue;

            float dist = Vector2.Distance(transform.position, bird.transform.position);
            if (dist < distance)
            {
                distance = dist;
                closest = bird;
            }
        }
        return closest;
    }

    // แบกอยู่ = กด E โยนนกอย่างเดียว; ไม่แบก = นกชนะเมื่อใกล้กว่าของที่ใกล้สุด (IN-1)
    protected override bool FriendInteractWins(float interactDist)
    {
        if (IsCarry) return true;
        return FindLiftableBird(out float birdDist) != null && birdDist < interactDist;
    }

    // host: ยกนก — ตัดสินที่ host ที่เดียว สองตัวทำตามจากค่าที่ sync (เดิม client ทำนายไปก่อนแล้วไม่ถูกย้อน)
    public void PickupFriend(MovementCharacter friend)
    {
        if (!HasStateAuthority || friend == null) return;

        IsCarry = true;
        CarriedFriendId = friend.Object.Id;
        friend.HostSetCarried(Object.Id);
    }

    // host: ปล่อย/โยนนก — นกวาร์ปไปเหนือหัวเป็ดแล้วรับแรงโยนผ่าน event ของเจ้าของนก
    public void DropFriend(bool throwFriend = true)
    {
        if (!HasStateAuthority) return;

        if (Runner.TryFindObject(CarriedFriendId, out var obj) && obj.TryGetComponent<MovementCharacter>(out var friend))
        {
            float throwDir = cAnimation.FlipX ? 1f : -1f;
            Vector2 dropPos = (Vector2)transform.position + Vector2.up * friend.betweenCarryPosition;
            Vector2 impulse = throwFriend ? new Vector2(throwDir * throwForceX, throwForceY) : Vector2.zero;

            friend.HostReleaseCarried(dropPos, impulse);
        }

        IsCarry = false;
        CarriedFriendId = default;

        PickupCooldownTimer = TickTimer.CreateFromSeconds(Runner, pickupCooldown);
    }

    public void OnCarryStateChanged()
    {
        if (Object.InputAuthority == Runner.LocalPlayer)
        {
            if (IsCarry)
            {
                if (pickupSoundClip != null) AudioManager.Instance?.PlayClipAtPosition(pickupSoundClip, transform.position);
            }
            else
            {
                if (dropSoundClip != null) AudioManager.Instance?.PlayClipAtPosition(dropSoundClip, transform.position);
            }
        }
    }

    public void HandleWaterLogic(NetworkInputData input)
    {
        if (!isDiveUnlocked) return;

        if (onDiving && currentWater == null)
        {
            EndDiveLogic();
            return;
        }

        if (onDiving && !IsBodyOnWater)
        {
            EndDiveLogic();
            return;
        }

        if (IsBeingCarried)
        {
            if (onDiving) EndDiveLogic();
            isOptional = false;
            isSpeedoptional = false;
            return;
        }

        bool isFPressed = WasPressed(input, InputButton.F);

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

                    cAnimation.UpdateSwimFlip(new Vector2(input.horizontal, input.vertical));
                    rb2D.linearDamping = 5f;
                }
            }
        }
        else if (emergencySwimBool && onDiving)
        {
            EmergencySwimup(input);
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

        // ต้องสร้างก่อนตั้ง onDiving: setter เรียก OnDivingStateChanged ทันที แล้วส่ง DiveTimer ให้แถบฟอง (DV-1)
        // — เดิมสร้างทีหลัง แถบฟองได้ timer ว่าง ขึ้นฟองหมดทั้งแถวตั้งแต่เริ่มดำ
        DiveTimer = TickTimer.CreateFromSeconds(Runner, divingTime);

        onDiving = true;
        onDivingControl = true;
        emergencyToggle = true;
    }

    public void OnDivingStateChanged()
    {
        if (Object.InputAuthority == Runner.LocalPlayer)
        {
            if (onDiving)
            {
                if (drivingSoundClip != null) AudioManager.Instance?.PlayClipAtPosition(drivingSoundClip, transform.position);

                if (localGUI != null) localGUI.StartOxygenTracking(DiveTimer, Runner, Mathf.CeilToInt(divingTime));
            }
            else
            {
                if (stopDrivingSoundClip != null) AudioManager.Instance?.PlayClipAtPosition(stopDrivingSoundClip, transform.position);

                if (localGUI != null) localGUI.StopOxygenTracking();
            }
        }
    }

    // แถบ oxygen บนเครื่องที่ไม่ใช่เจ้าของตัว (OX-1): DiveTimer อยู่แค่เครื่องเจ้าของ จึงเริ่มนับเองตอนเห็นธงดำน้ำ
    // ช้ากว่าเจ้าของตามเวลาเน็ต แต่ฟองลดทีละวินาทีจึงต่างกันไม่มาก
    private void RenderRemoteOxygen()
    {
        if (IsOwner || localGUI == null) return;

        bool diving = onDiving;
        if (diving == _remoteDiving) return;
        _remoteDiving = diving;

        if (diving) localGUI.StartOxygenTracking(TickTimer.CreateFromSeconds(Runner, divingTime), Runner, Mathf.CeilToInt(divingTime));
        else localGUI.StopOxygenTracking();
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

    // จบดำน้ำทั้งที่หัวยังอยู่ใต้น้ำ -> ถูกดันขึ้นจนกว่าหัวพ้นน้ำ (DV-2)
    // เดิมดันแค่ emergencySwimTimer แล้วจบดำน้ำใต้น้ำ: gravity ค้าง 0 ไม่มีแรงขึ้น หัวติดรากไม้แล้วท่ากลายเป็นท่าเดิน
    // เกินเวลา = อากาศหมด: host เริ่มดาเมจจมน้ำ และเลี้ยวซ้าย/ขวาหนีออกจากใต้สิ่งที่บังได้ (ยังเป็นท่าว่าย)
    public void EmergencySwimup(NetworkInputData input)
    {
        if (!stilldrowning)
        {
            EndDiveLogic();
            return;
        }

        bool outOfAir = EmergencyTimer.Expired(Runner);

        // oxygen หมด: host เห็นธงนี้แล้วเริ่มดาเมจจมน้ำ (NR-12)
        if (outOfAir) SetMoveFlag(MoveFlags.OxygenDepleted, true);

        float steer = outOfAir ? input.horizontal : 0f;
        Vector2 inputDir = new Vector2(steer, 1f);

        if (inputDir.sqrMagnitude > 1)
            inputDir.Normalize();

        Vector2 targetVel = inputDir * swimMaxSpeed;
        Vector2 speedDif = targetVel - rb2D.linearVelocity;

        rb2D.AddForce(speedDif * emergencyAcceleration);

        if (outOfAir) cAnimation.UpdateSwimFlip(new Vector2(steer, 0f));
    }

    public void HandleBuoyancy()
    {
        if (isClimbing) return;

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
        else if (!onDiving)
        {
            isOptional = false;
            isSpeedoptional = false;
            if (!IsInAir) rb2D.gravityScale = normalGravity;
        }
    }

    #region Skill

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

        SyncCarryColliders();
        RenderRemoteOxygen();

        if (!HasInputAuthority || PlayerInterface.Instance == null) return;

        if (isDiveUnlocked && PlayerInterface.Instance.spawnedDuckDive != null)
        {
            PlayerInterface.Instance.spawnedDuckDive.SetPressed(onDiving);

            PlayerInterface.Instance.spawnedDuckDive.SetUsable(isWaterSurface);
        }

        if (isSmashUnlocked && PlayerInterface.Instance.spawnedDuckSmash != null)
        {
            PlayerInterface.Instance.spawnedDuckSmash.SetPressed(IsInteractHeld);

            PlayerInterface.Instance.spawnedDuckSmash.SetUsable(isNearBreakableRock);
        }
    }
}