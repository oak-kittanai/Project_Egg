using Fusion;
using System;
using UnityEngine;

public class Bird_Moveset : MovementCharacter, IstunAble
{
    [Header("Bird Settings")]
    [SerializeField] float normalFlyTime = 5f;
    [SerializeField] float carryFlyTime = 3f;
    [SerializeField] float floatingGravity = 0.1f;

    //SOUND
    [SerializeField] public AudioClip flySoundClip;
    [SerializeField] public AudioClip stopFlySoundClip;
    [SerializeField] public AudioClip throwSoundClip;

    [Header("Physics Materials")]
    [SerializeField] PhysicsMaterial2D zeroFrictionMaterial;
    private PhysicsMaterial2D defaultMaterial;

    [Header("Bird State")]
    // สถานะบิน/ลอยเป็นของเจ้าของตัว (NR-12) — เสียง/แถบบินของเจ้าของเรียกตอนค่าเปลี่ยน
    private TickTimer FlightTimer;
    public bool IsFlying
    {
        get => GetMoveFlag(MoveFlags.Flying);
        set
        {
            bool changed = IsOwner && GetMoveFlag(MoveFlags.Flying) != value;
            SetMoveFlag(MoveFlags.Flying, value);
            if (changed) OnFlyingStateChanged();
        }
    }
    public bool IsAlreadyFly { get => GetMoveFlag(MoveFlags.AlreadyFly); set => SetMoveFlag(MoveFlags.AlreadyFly, value); }
    public bool AlreadyFloating { get => GetMoveFlag(MoveFlags.AlreadyFloating); set => SetMoveFlag(MoveFlags.AlreadyFloating, value); }

    // เป็ดที่ยกนกอยู่ใช้ตอนนกบิน (Duck_Moveset.LiftWithFlyingFriend)
    public PhysicsMaterial2D ZeroFrictionMaterial => zeroFrictionMaterial;

    // กำลังเล็ง/เหวี่ยงปา (host ตัดสิน) -> เจ้าของล็อกการเดิน
    protected override bool HostMovementLocked => base.HostMovementLocked || _prepareToThrow || _isReleasingThrow;

    private bool _wasReleasingThrow;


    // Drowning
    [Networked] private TickTimer DrownTimer { get; set; }
    [SerializeField] float drowningTime = 3f;
    [Networked, OnChangedRender(nameof(OnDrownTimerStateChanged))]
    public NetworkBool startTimer { get; set; }

    [Header("ThrowSystem")]
    [SerializeField] public NetworkObject throwAblePrefab;
    [Networked] public bool _prepareToThrow { get; set; }

    // หน่วงปล่อยหินให้ตรงจังหวะเหวี่ยงของ animation (deterministic ผ่าน TickTimer ไม่ใช่ Animation Event)
    [Networked] bool _isReleasingThrow { get; set; }
    // ท่า "Throwing" ของทุกเครื่องอ่านจากค่านี้ (CharacterAnimation) แทน RPC
    public bool IsReleasingThrow => _isReleasingThrow;
    [Networked] TickTimer ThrowReleaseTimer { get; set; }
    [Tooltip("หน่วงเวลา (วินาที) ตั้งแต่กดยืนยันปา จนหินหลุดมือ ให้ตรงเฟรมเหวี่ยงของ animation")]
    [SerializeField] float throwReleaseDelay = 0.35f;

    [SerializeField] float projectileSpeed;
    [SerializeField] public Transform throwPoint;
    // Line
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] int lineCounts;
    [SerializeField] float timeIntervalinPoints = 0.01f;
    [SerializeField] float currentAimX;
    // Sweeping Aim
    [SerializeField] float aimSweepSpeed = 3f;
    [SerializeField] float maxAimAngle = 30f;

    [Header("Unlockable Skills")]
    [Networked, OnChangedRender(nameof(OnSkillStateChanged))] public NetworkBool isFlyUnlocked { get; set; }
    [Networked, OnChangedRender(nameof(OnSkillStateChanged))] public NetworkBool isThrowUnlocked { get; set; }

    public override void Spawned()
    {
        base.Spawned();

        if (rb2D != null) defaultMaterial = rb2D.sharedMaterial;
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
    }

    public void TriggerStun()
    {
        ApplyStun();
    }

    // เจ้าของตัว: บิน/ลอย, หันตามเป็ดที่ยกอยู่, จมน้ำส่วนการเคลื่อนที่ (NR-12)
    protected override void OnOwnerSimulateSpecific(NetworkInputData input, bool hasInput)
    {
        bool isMenuOpen = GameManager.Instance != null && GameManager.Instance.IsPaused;

        if (hasInput && !isMenuOpen)
        {
            HandleFlightLogic(input);
        }

        if (IsGrounded)
        {
            IsAlreadyFly = false;

            if (AlreadyFloating) StopFloating();

            if (!IsFlying && rb2D != null && rb2D.sharedMaterial != defaultMaterial)
            {
                rb2D.sharedMaterial = defaultMaterial;
            }
        }

        if (IsBeingCarried)
        {
            if (AlreadyFloating) StopFloating();

            if (Runner.TryFindObject(CarrierId, out var duckObj) && duckObj.TryGetComponent<MovementCharacter>(out var duck))
            {
                if (!IsFlying && (duck.IsGrounded || duck.isWaterSurface))
                {
                    IsAlreadyFly = false;
                }

                cAnimation.FlipX = duck.cAnimation.FlipX;
            }
        }

        if (!isMenuOpen) OwnerDrowningMovement();
    }

    // host: ปาหิน (spawn บน host) + จับเวลา oxygen / ดาเมจจมน้ำ
    protected override void OnHostRulesSpecific(NetworkInputData input, bool hasInput, bool isMenuOpen)
    {
        if (hasInput)
        {
            if (!isMenuOpen) HandleThrowLogic(input);
            else if (_prepareToThrow) CancelThrow();
        }

        if (!isMenuOpen) HostDrowningTimer();
    }

    // จมน้ำเอง หรือถูกเป็ดที่กำลังดำน้ำยกอยู่
    private bool IsBirdDrowning()
    {
        if (IsBeingCarried)
        {
            return Runner.TryFindObject(CarrierId, out var duckObj)
                && duckObj.TryGetComponent<Duck_Moveset>(out var duck)
                && (duck.onDiving || duck.IsHeadUnderwater);
        }

        return stilldrowning;
    }

    // เจ้าของ: จมน้ำแล้วค่อย ๆ จมลง
    private void OwnerDrowningMovement()
    {
        if (IsBirdDrowning())
        {
            if (!IsBeingCarried)
            {
                isMoveAble = false;
                isOptional = true;
                optionalGravity = 0f;
                rb2D.linearVelocity = new Vector2(0f, -1.5f);
                rb2D.linearDamping = 3f;
            }
        }
        else if (!AlreadyFloating && !IsFlying && rb2D != null && rb2D.linearDamping != 0f)
        {
            rb2D.linearDamping = 0f;
        }
    }

    // host: เริ่มจับเวลา oxygen ครั้งเดียวตอนเข้าจมน้ำ — พอ oxygen หมดเข้าเฟส damage แล้วอย่า restart timer อีก
    // (ไม่งั้น oxygen visual จะเด้งกลับมาเต็มทั้งที่ยังจมอยู่)
    private void HostDrowningTimer()
    {
        if (IsBirdDrowning())
        {
            if (!startTimer && !isDrowningDamageActive)
            {
                StartDrowningTimer();
            }
            else if (startTimer && DrownTimer.Expired(Runner))
            {
                startTimer = false;
                StartDrowningDamage();
            }
        }
        else
        {
            if (startTimer)
            {
                DrownTimer = TickTimer.None;
                startTimer = false;
            }

            StopDrowningDamage();
        }

        TickDrowningDamage();
    }

    private void StartDrowningTimer()
    {
        startTimer = true;
        DrownTimer = TickTimer.CreateFromSeconds(Runner, drowningTime);
    }

    public void OnDrownTimerStateChanged()
    {
        if (HasInputAuthority && localGUI != null)
        {
            if (startTimer)
            {
                localGUI.StartOxygenTracking(DrownTimer, Runner, Mathf.CeilToInt(drowningTime));
            }
            else
            {
                localGUI.StopOxygenTracking();
            }
        }
    }

    public override void OnDroppedEvent()
    {
        base.OnDroppedEvent();

        ForceCancelFlight();

        FallingBusy = false;
        isOptional = false;
        IsAlreadyFly = false;

        if (HasInputAuthority && localGUI != null)
        {
            localGUI.StopFlightBar();
        }

        if (rb2D != null)
        {
            rb2D.sharedMaterial = defaultMaterial;
            rb2D.linearDamping = 0f;
            rb2D.gravityScale = normalGravity;
        }

        if (HasStateAuthority)
        {
            startTimer = false;
            DrownTimer = TickTimer.None;
        }
    }

    public override void OnRespawned()
    {
        base.OnRespawned();

        IsFlying = false;
        IsAlreadyFly = false;
        AlreadyFloating = false;
        FallingBusy = false;
        FlightTimer = TickTimer.None;

        if (HasStateAuthority)
        {
            startTimer = false;
            DrownTimer = TickTimer.None;

            _prepareToThrow = false;
            _isReleasingThrow = false;
            ThrowReleaseTimer = TickTimer.None;
        }

        if (rb2D != null)
        {
            rb2D.sharedMaterial = defaultMaterial;
        }

        if (HasInputAuthority && localGUI != null)
            localGUI.StopOxygenTracking();
    }

    #region FlyLogic
    private void HandleFlightLogic(NetworkInputData input)
    {
        if (!isFlyUnlocked) return;

        bool isPressed = WasPressed(input, InputButton.Jump);

        if (!IsBeingCarried)
        {
            if (isPressed && IsInAir)
            {
                if (!IsFlying && !IsAlreadyFly && !stilldrowning)
                {
                    StartFlying();
                }
            }
        }
        else
        {
            isMoveAble = false;

            if (isPressed)
            {
                bool isDuckDiving = false;
                if (Runner.TryFindObject(CarrierId, out var duckObj) && duckObj.TryGetComponent<Duck_Moveset>(out var duck))
                {
                    isDuckDiving = duck.onDiving || duck.IsHeadUnderwater;
                }

                if (!IsFlying && !IsAlreadyFly && !isDuckDiving)
                {
                    StartFlying();
                }
            }
        }

        if (isPressed && IsAlreadyFly && !IsFlying)
        {
            if (AlreadyFloating) StopFloating();
            else StartFloating();
        }

        if (IsFlying)
        {
            if (FlightTimer.Expired(Runner))
            {
                StopFlying();
                IsAlreadyFly = true;
                StartFloating();
            }
            else
            {
                // ถูกยกอยู่: เป็ดลอยตัวเองขึ้นตาม IsFlying ของนก (Duck_Moveset.LiftWithFlyingFriend)
                if (!IsBeingCarried)
                {
                    rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, stats.s_flySpeed);
                }
            }
        }
    }

    public void OnFlyingStateChanged()
    {
        if (Object.InputAuthority == Runner.LocalPlayer)
        {
            if (IsFlying)
            {
                float duration = IsBeingCarried ? carryFlyTime : normalFlyTime;
                if (flySoundClip != null) AudioManager.Instance?.PlayClipAtPosition(flySoundClip, transform.position);

                if (localGUI != null) localGUI.StartFlightBar(FlightTimer, Runner, duration);
            }
            else
            {
                if (stopFlySoundClip != null) AudioManager.Instance?.PlayClipAtPosition(stopFlySoundClip, transform.position);

                if (localGUI != null) localGUI.StopFlightBar();
            }
        }
    }

    private void StartFloating()
    {
        FallingBusy = true;
        AlreadyFloating = true;
        optionalGravity = floatingGravity;
        isOptional = true;

        rb2D.linearDamping = 5f;
    }

    private void StopFloating()
    {
        FallingBusy = false;
        AlreadyFloating = false;
        isOptional = false;
        rb2D.linearDamping = 0f;
        rb2D.gravityScale = normalGravity;
    }

    // host และ client ที่คุมนกตัวนี้ (input authority) เริ่มบินพร้อมกันได้ — ค่าที่เขียนเป็น networked ทั้งหมด
    // (เดิม return ถ้าไม่ใช่ host ทำให้ฝั่ง client บินช้ากว่ากดแล้วดีดตามทีหลัง — NJ-8)
    private void StartFlying()
    {
        // ตั้ง timer ก่อน — IsFlying เปลี่ยนแล้วแถบบินของเจ้าของอ่าน timer ทันที
        float duration = IsBeingCarried ? carryFlyTime : normalFlyTime;
        FlightTimer = TickTimer.CreateFromSeconds(Runner, duration);

        IsFlying = true;
        isJumping = false;

        if (rb2D != null && zeroFrictionMaterial != null)
        {
            rb2D.sharedMaterial = zeroFrictionMaterial;
        }
    }

    private void StopFlying()
    {
        IsFlying = false;
        FlightTimer = TickTimer.None;

        if (rb2D != null)
        {
            rb2D.sharedMaterial = defaultMaterial;
        }
    }

    public void ForceCancelFlight()
    {
        if (IsFlying) StopFlying();
        if (AlreadyFloating) StopFloating();
        IsAlreadyFly = false;
    }
    #endregion

    #region ThrowLogic

    public void HandleThrowLogic(NetworkInputData input)
    {
        bool isPrepareThrowPressed = WasPressed(input, InputButton.Throw);

        // อยู่ช่วง windup (กดยืนยันแล้ว กำลังรอหินหลุดมือ) — ล็อกการเคลื่อนที่ รอ timer แล้วค่อย spawn
        if (_isReleasingThrow)
        {
            isMoveAble = false;
            IsInteractBusy = true;

            if (ThrowReleaseTimer.Expired(Runner))
            {
                ExecuteThrow();
            }

            return;
        }

        if (_prepareToThrow)
        {
            if (Mathf.Abs(input.horizontal) > 0.1f || input.KeybindJump)
            {
                CancelThrow();
            }
        }

        if (isPrepareThrowPressed)
        {
            if (!isThrowUnlocked) return;

            if (_canThrowItem && !(isWaterSurface || stilldrowning))
            {
                if (!_prepareToThrow)
                {
                    _prepareToThrow = true;
                }
                else
                {
                    StartThrowRelease();
                }
            }
            else
            {
                if (_prepareToThrow) CancelThrow();
            }
        }

        if (_prepareToThrow)
        {
            isMoveAble = false;
            IsInteractBusy = true;

            UpdateOscillatingAim();
        }
    }

    // กดยืนยันปา: เริ่มเล่น animation เหวี่ยง + ล็อกทิศเล็ง แล้วหน่วง spawn ด้วย TickTimer
    // (หินจะหลุดมือใน ExecuteThrow เมื่อ timer หมด — ดู windup ใน HandleThrowLogic)
    private void StartThrowRelease()
    {
        _prepareToThrow = false;
        _isReleasingThrow = true;
        ThrowReleaseTimer = TickTimer.CreateFromSeconds(Runner, throwReleaseDelay);
    }

    public void ExecuteThrow()
    {
        Vector2 throwPos = throwPoint.position;
        Vector2 direction = throwPoint.right;

        NetworkObject spawnedRock = GameManager.Instance.ProjectileSpawn(throwAblePrefab, throwPos, direction, throwPoint.rotation, projectileSpeed);
        if (spawnedRock != null && spawnedRock.TryGetComponent<RockObject>(out var rockObj))
        {
            rockObj.ThrowerId = Object.Id;

            // กันหินชนตัวคนปาเองทางฟิสิกส์ตรงๆ (แค่ ThrowerId เช็คใน OnCollisionEnter2D ไม่พอ
            // เพราะ solid collider ยังชนกันได้อยู่ดี ทำให้รู้สึกโดนตัวเอง/มี knockback)
            if (coll2D != null && spawnedRock.TryGetComponent<Collider2D>(out var rockCollider))
            {
                Physics2D.IgnoreCollision(rockCollider, coll2D, true);
            }
        }

        if (HasInputAuthority && throwSoundClip != null)
        {
            AudioManager.Instance?.PlayClipAtPosition(throwSoundClip, transform.position);
        }

        _canThrowItem = false;
        HeldItemName = "";

        CancelThrow();
    }

    private void CancelThrow()
    {
        isMoveAble = true;
        _prepareToThrow = false;
        _isReleasingThrow = false;
        ThrowReleaseTimer = TickTimer.None;
        IsInteractBusy = false;

        throwPoint.localRotation = Quaternion.identity;
    }

    private float GetAimAngle()
    {
        return Mathf.Sin((float)Runner.SimulationTime * aimSweepSpeed) * maxAimAngle;
    }

    private void ApplyAimRotation()
    {
        if (throwPoint == null || cAnimation == null) return;
        float currentFaceTo = cAnimation.FlipX ? 0f : 180f;
        throwPoint.localRotation = Quaternion.Euler(0, currentFaceTo, GetAimAngle());
    }

    private void UpdateOscillatingAim()
    {
        ApplyAimRotation();
        currentAimX = GetAimAngle();
    }

    public void DrawLine()
    {
        Vector3 originPos = throwPoint.position;

        Vector3 initialVelocity = projectileSpeed * throwPoint.right;

        lineRenderer.positionCount = lineCounts;
        float time = 0;

        for (int i = 0; i < lineCounts; i++)
        {
            var x = (initialVelocity.x * time) + (Physics2D.gravity.x / 2f * time * time);
            var y = (initialVelocity.y * time) + (Physics2D.gravity.y / 2f * time * time);

            Vector3 point = new Vector3(x, y, 0);

            lineRenderer.SetPosition(i, originPos + point);

            time += timeIntervalinPoints;
        }
    }

    #endregion

    #region Skill

    public void OnSkillStateChanged() { SyncSkillUI(); }

    public override void SyncSkillUI()
    {
        if (!HasInputAuthority || PlayerInterface.Instance == null) return;

        if (isFlyUnlocked && PlayerInterface.Instance.spawnedBirdFly != null)
        {
            PlayerInterface.Instance.spawnedBirdFly.UnlockSkill();
            PlayerInterface._birdFlyUnlocked = true;
        }

        if (isThrowUnlocked && PlayerInterface.Instance.spawnedBirdThrow != null)
        {
            PlayerInterface.Instance.spawnedBirdThrow.UnlockSkill();
            PlayerInterface._birdThrowUnlocked = true;
        }
    }

    #endregion

    public override void Render()
    {
        base.Render();

        // เสียงปาของนกฝั่ง client — หินถูก spawn บน host (เดิมเล่นจากการทำนายของ client)
        if (HasInputAuthority && !HasStateAuthority && _wasReleasingThrow && !_isReleasingThrow && throwSoundClip != null)
        {
            AudioManager.Instance?.PlayClipAtPosition(throwSoundClip, transform.position);
        }
        _wasReleasingThrow = _isReleasingThrow;

        if (_prepareToThrow)
        {
            ApplyAimRotation();
            if (!lineRenderer.enabled) lineRenderer.enabled = true;
            DrawLine();
        }
        else
        {
            if (lineRenderer.enabled) lineRenderer.enabled = false;
        }

        if (!HasInputAuthority || PlayerInterface.Instance == null) return;

        if (isFlyUnlocked && PlayerInterface.Instance.spawnedBirdFly != null)
        {
            PlayerInterface.Instance.spawnedBirdFly.SetPressed(IsFlying);

            PlayerInterface.Instance.spawnedBirdFly.UpdateCooldown(FlightTimer, Runner);

            PlayerInterface.Instance.spawnedBirdFly.SetUsable(!IsAlreadyFly && !stilldrowning);
        }

        if (isThrowUnlocked && PlayerInterface.Instance.spawnedBirdThrow != null)
        {
            PlayerInterface.Instance.spawnedBirdThrow.SetPressed(_prepareToThrow);

            PlayerInterface.Instance.spawnedBirdThrow.SetUsable(_canThrowItem);
        }
    }
}