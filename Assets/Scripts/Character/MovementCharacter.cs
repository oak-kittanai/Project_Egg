using Fusion;
using UnityEngine;
using Fusion.Addons.Physics;

public class MovementCharacter : NetworkBehaviour, IDamageable, IBeforeTick, IAfterTick
{
    #region References
    [Header("References")]
    [SerializeField] public CharacterStats stats;
    [SerializeField] public CharacterAnimation cAnimation;
    [SerializeField] public Rigidbody2D rb2D;
    [SerializeField] public Collider2D coll2D;
    [SerializeField] public PlayerGUI localGUI;
    [SerializeField] public SpriteRenderer spriteRenderer;
    #endregion

    #region Audio
    [Header("Audio System")]
    [SerializeField] public AudioClip jumpSoundClip;
    [SerializeField] public AudioClip landingSoundClip;
    [SerializeField] public AudioClip dmgSoundClip;
    [SerializeField] public AudioClip dieSoundClip;
    [SerializeField] public AudioClip respawnSoundClip;

    [Header("Movement Audio (Local Loop)")]
    [SerializeField] public AudioSource movementAudioSource;
    [SerializeField] public AudioClip walkSoundClip;
    [SerializeField] public AudioClip swimSoundClip;
    #endregion

    #region Identity
    [Networked, OnChangedRender(nameof(OnCharacterTypeChanged))]
    public bool isBird { get; set; }
    #endregion

    #region Visual
    [Header("Visual")]
    [SerializeField] public Transform visualTransform;
    private int originalSortingOrder;
    #endregion

    #region Movement State
    [Header("Movement Settings")]
    // สถานะการเคลื่อนที่เป็นของเจ้าของตัว (NR-12): เจ้าของอ่าน/เขียนในเครื่อง, เครื่องอื่นอ่านจาก RepFlags ที่ host sync
    // (ดู region "Owner Movement") — ค่าที่เขียนจากเครื่องที่ไม่ใช่เจ้าของถูกเมิน
    public bool IsGrounded { get => GetMoveFlag(MoveFlags.Grounded); set => SetMoveFlag(MoveFlags.Grounded, value); }
    public bool isWaterSurface { get => GetMoveFlag(MoveFlags.WaterSurface); set => SetMoveFlag(MoveFlags.WaterSurface, value); }
    public bool IsInAir { get => GetMoveFlag(MoveFlags.InAir); set => SetMoveFlag(MoveFlags.InAir, value); }
    public bool isFloating { get => GetMoveFlag(MoveFlags.Floating); set => SetMoveFlag(MoveFlags.Floating, value); }
    // ใช้แค่ในเครื่องเจ้าของ — ล็อกที่ host ตัดสิน (มึน/ถูกยก/เล็งปา) ดู HostMovementLocked
    public bool isMoveAble { get; set; }

    public bool isJumping { get => GetMoveFlag(MoveFlags.Jumping); set => SetMoveFlag(MoveFlags.Jumping, value); }
    #endregion

    #region Jump Physics
    private TickTimer JumpCooldown;

    [Header("Jump - Cut")]
    [Tooltip("ตัวคูณ gravity ตอนปล่อยปุ่มกระโดดเร็ว (ระหว่างลอยขึ้น ไม่ใกล้ apex)")]
    [SerializeField] private float jumpCutMultiplier = 2f;

    [Header("Jump - Apex Hang")]
    [Tooltip("ช่วง velocity.y (บวก/ลบ) ที่ถือว่าใกล้จุดสูงสุดของอาร์คกระโดด")]
    [SerializeField] private float apexThreshold = 1f;
    [Tooltip("ตัวคูณ gravity ตอนอยู่ในช่วงใกล้ apex (ค่าน้อย = ลอยค้างนานขึ้น)")]
    [SerializeField] private float apexGravityMultiplier = 0.5f;

    [Header("Jump - Head Bump")]
    [Tooltip("ระยะ raycast ขึ้นด้านบนเพื่อเช็คว่าหัวชนเพดาน")]
    [SerializeField] private float headBumpCheckDistance = 0.3f;
    [Tooltip("velocity.y ที่บังคับใส่ทันทีเมื่อหัวชนเพดาน (ค่าลบ = ร่วงทันที)")]
    [SerializeField] private float headBumpDownwardVelocity = -2f;
    #endregion

    #region Movement Feel (MV)
    // การบังคับแบบ platformer ทั่วไป (Hollow Knight / Celeste): ตั้งความเร็วตรงแทนการใช้แรง + ตัวช่วยกระโดด
    // คำนวณในเครื่องเจ้าของตัวเท่านั้น (NR-12) — ค่าในหัวนี้จูนสดได้ใน Play Mode
    [Header("Movement Feel (MV)")]
    [Tooltip("ความเร็วเดิน/วิ่งบนพื้นและกลางอากาศ (MV-6) — แยกจาก CharacterStats.maxSpeed ที่สูตรเดิมใช้ในน้ำ/ร่อน/ลม/กระเด็น")]
    [SerializeField] private float runSpeed = 3.5f;
    [Tooltip("เวลา (วินาที) จากหยุดนิ่งจนถึง runSpeed บนพื้น — น้อย = ออกตัวไว")]
    [SerializeField] private float groundAccelTime = 0.06f;
    [Tooltip("เวลา (วินาที) จาก runSpeed จนหยุดบนพื้นหลังปล่อยปุ่ม — น้อย = หยุดไว")]
    [SerializeField] private float groundDecelTime = 0.05f;
    [Tooltip("สัดส่วนการเร่ง/เบรกกลางอากาศเทียบกับบนพื้น (1 = เท่าบนพื้น)")]
    [Range(0f, 1f)][SerializeField] private float airControl = 0.65f;
    [Tooltip("ตัวคูณการลดความเร็วตอนเร็วเกิน maxSpeed ไปทางเดียวกับที่กด (เช่นหลุดจากลม) — น้อย = เก็บโมเมนตัมนานขึ้น")]
    [Range(0f, 1f)][SerializeField] private float overSpeedReduce = 0.4f;
    [Tooltip("เดินพ้นขอบไปแล้วยังกดกระโดดได้อีกกี่วินาที (coyote time)")]
    [SerializeField] private float coyoteTime = 0.1f;
    [Tooltip("กดกระโดดก่อนแตะพื้นได้ล่วงหน้ากี่วินาที (jump buffer)")]
    [SerializeField] private float jumpBufferTime = 0.1f;
    [Tooltip("เวลาขั้นต่ำระหว่างการกระโดดสองครั้ง (วินาที) — เดิม 1 วินาที ทำให้ลงพื้นแล้วกดกระโดดซ้ำไม่ติด")]
    [SerializeField] private float jumpCooldownTime = 0.1f;
    [Tooltip("หลังโดนแรงกระเด็น/ถูกโยน ใช้การคุมแบบเดิม (ใช้แรง) อย่างน้อยกี่วินาที และต่อไปจนลงพื้น — ระยะกระเด็น/โยนเท่าเดิม")]
    [SerializeField] private float externalForceTime = 0.25f;
    [Tooltip("ความกว้างที่ใช้เช็คพื้นเทียบกับความกว้างตัว (ยิง ray ซ้าย/กลาง/ขวา) — ยืนเกินขอบแล้วยังกระโดดได้")]
    [Range(0f, 1f)][SerializeField] private float groundCheckWidth = 0.8f;

    private const int NoTick = int.MinValue / 2;
    private int _lastGroundedTick = NoTick;
    private int _jumpPressedTick = NoTick;
    private int _externalForceUntilTick = NoTick;
    private bool _externalForceUntilLanding;
    // กระโดดใน tick นี้ — นกใช้กันเริ่มบินซ้อนกับ coyote jump จากขอบ
    protected bool JumpedThisTick { get; private set; }
    private static PhysicsMaterial2D s_airMaterial;
    #endregion

    #region Gravity & Falling
    public bool isSpeedoptional { get; set; }
    [Header("Gravity Settings")]
    [SerializeField] public float normalGravity = 3.5f;
    [SerializeField] public float heavyGravity = 6.5f;
    [SerializeField] public float maxGravity = 19f;

    private bool hasSetInitialPosition = false;

    // Falling
    public bool IsFalling { get; set; }
    public bool FallingBusy { get => GetMoveFlag(MoveFlags.FallingBusy); set => SetMoveFlag(MoveFlags.FallingBusy, value); }
    public bool isOptional { get; set; }
    public float optionalGravity { get; set; }
    [SerializeField] public float accelerationSpeedOptional = 0.6f;
    [SerializeField] public float decelerationSpeedOptional = 3f;
    [SerializeField] public float optionalMaxSpeed = 1f;
    #endregion

    #region Health & Respawn
    [Header("Health & Respawn")]
    [Networked] public int characterMaxHealth { get; set; }
    [Networked, OnChangedRender(nameof(OnHealthChanged))] public int currentHealth { get; set; }
    [Networked] public bool isDead { get; set; }

    // ตาย/เกิดใหม่ sync ด้วย state แทน RPC (LP-3): host เพิ่มตัวนับ แล้วทุกเครื่องทำภาพ เสียง UI และฟิสิกส์ของตัวเองตอนค่าเปลี่ยน
    [Networked] public NetworkBool IsPrimaryDeath { get; set; }
    [Networked, OnChangedRender(nameof(OnDeathCountChanged))] private byte DeathCount { get; set; }
    [Networked, OnChangedRender(nameof(OnRespawnCountChanged))] private byte RespawnCount { get; set; }
    [Networked] private NetworkBool LastRespawnWasReset { get; set; }

    [Networked] TickTimer respawnTimer { get; set; }
    [SerializeField] float respawnCooldown = 3f;
    [SerializeField] bool canbeRespawn = true;
    #endregion

    #region Drowning Damage
    [Header("Drowning Damage")]
    [SerializeField] private int drowningDamagePerTick = 1;
    [SerializeField] private float drowningDamageInterval = 1f;
    [Networked] private TickTimer DrowningDamageTimer { get; set; }
    [Networked] protected bool isDrowningDamageActive { get; set; }
    #endregion

    #region Stun
    [Header("Stun")]
    [SerializeField] private float stunDuration = 1.5f;
    [Networked] private TickTimer StunTimer { get; set; }
    [Networked, OnChangedRender(nameof(OnStunStateChanged))] public bool isStunned { get; set; }
    #endregion

    #region Water Settings
    [Header("Water Setting")]
    public bool IsHeadUnderwater { get => GetMoveFlag(MoveFlags.HeadUnderwater); set => SetMoveFlag(MoveFlags.HeadUnderwater, value); }
    public bool IsBodyOnWater { get => GetMoveFlag(MoveFlags.BodyOnWater); set => SetMoveFlag(MoveFlags.BodyOnWater, value); }
    [SerializeField] public float headOffset = 0.2f;
    [SerializeField] public float bodyOffset = -0.2f;
    [SerializeField] public float nearGroundDistance = 0.63f;
    [SerializeField] public NetworkInteractableWater currentWater;
    public bool stilldrowning { get => GetMoveFlag(MoveFlags.StillDrowning); set => SetMoveFlag(MoveFlags.StillDrowning, value); }
    #endregion

    #region Passenger / Carry System
    [Header("Passenger System")]
    [Networked] public NetworkId CarrierId { get; set; }
    [Networked] public bool IsBeingCarried { get; set; }
    [SerializeField] public bool isCarrying => this is Duck_Moveset duck && duck.IsCarry;
    [Networked] public bool IsInteractBusy { get; set; }
    [SerializeField] public float betweenCarryPosition = 0.65f;

    [Header("Carry Colliders")]
    public Collider2D normalCollider;
    public Collider2D carryCollider;

    [SerializeField] PhysicsMaterial2D zeroFiction;

    private bool _wasVisuallyCarried;
    // เจ้าของตัว: สถานะถูกยกของ tick ก่อน (จับจังหวะเพิ่งถูกยก/เพิ่งถูกปล่อย)
    private bool _ownerWasCarried;

    #endregion

    #region Platform Interaction
    // moving platform ที่ยืนอยู่ตอนนี้ + ตำแหน่งล่าสุดของมัน (ไว้คำนวณ delta แล้ว follow แนวนอน)
    private Transform currentPlatform;
    private Vector3 lastPlatformPos;
    #endregion

    #region Interaction & Physics
    [Header("Interaction & Physics")]
    public float rayDistance = 1.2f;
    public float interactRadius = 1.5f;
    public float playerInteractRadius = 1f;

    // host: กด E ใน tick นี้ไปใช้กับของแล้ว (ทุบหิน/เก็บของ) — เป็ดจะไม่ยกนกซ้ำในกดเดียวกัน (IN-1)
    protected bool InteractUsedOnWorldThisTick;
    #endregion

    #region I-Frames & Effects
    [Header("I-Frames & Effects")]
    [Networked] private TickTimer InvincibleTimer { get; set; }
    [SerializeField] private float invincibleDuration = 1.5f;
    private bool _wasReadingTutorial; // host: ค่า tick ก่อน ไว้จับจังหวะปิดหน้าสอนเล่น (TU-1)
    [SerializeField] private DamageFlash _damageFlash;
    [SerializeField] public Color duck_Color;
    [SerializeField] public Color bird_Color;
    #endregion

    #region Climbing
    [Header("Climbing")]
    [SerializeField] float climbSpeed = 2f;
    [SerializeField] LayerMask climbableMask;
    public bool isInClimbZone { get; set; }
    public bool isClimbing { get => GetMoveFlag(MoveFlags.Climbing); set => SetMoveFlag(MoveFlags.Climbing, value); }
    public bool jumpedFromClimb { get; set; }

    private bool wasMenuOpenLastTick;
    #endregion

    #region Skills
    // UnlockableSkills
    public enum SkillType { None, Duck_Dive, Duck_Smash, Bird_Fly, Bird_Throw }
    #endregion

    #region Inventory & Drop
    [Header("Inventory System (1 Slot)")]
    [Networked, OnChangedRender(nameof(OnHeldItemChanged))]
    public NetworkString<_32> HeldItemName { get; set; }

    [HideInInspector] public bool isNearBreakableRock;

    [Header("Drop Settings")]
    [SerializeField] private float dropFrontOffset = 0.6f;
    [SerializeField] private float dropUpOffset = 0.5f;
    [SerializeField] private LayerMask dropBlockLayer;

    // Throw System
    [Networked] public bool _canThrowItem { get; set; }
    #endregion

    #region Etc
    [Header("Etc")]
    // ปุ่มของ tick ก่อนหน้า — networked จึงถูกย้อนตอน resimulate (แทน _isEPressed / _wasEscPressed ฯลฯ เดิม)
    [Networked] public NetworkButtons PreviousButtons { get; set; }

    protected bool WasPressed(NetworkInputData input, InputButton button) => input.Buttons.WasPressed(PreviousButtons, button);

    // E ที่กดค้างอยู่ใน tick ล่าสุด (ไอคอนสกิลใช้)
    public bool IsInteractHeld => PreviousButtons.IsSet(InputButton.Interact);
    #endregion

    #region Owner Movement (NR-12)
    // เจ้าของตัว (input authority) จำลองการเคลื่อนที่ของตัวเองเฉพาะ tick ใหม่ ไม่ถูก host แก้/ย้อน
    // client ส่งตำแหน่ง/ความเร็ว/สถานะใน input ทุก tick -> host วางตัวนั้น (kinematic) ตามแล้ว sync ให้อีกเครื่องดู
    // host ยังตัดสินโลกทั้งหมด (ดาเมจ, มึน, ตาย/เกิด, ยก/ปล่อย, ไอเท็ม) — ผลที่ต้องกระทบตัวเจ้าของส่งเป็น event (วาร์ป/แรง)
    [System.Flags]
    public enum MoveFlags : uint
    {
        None = 0,
        Grounded = 1u << 0,
        WaterSurface = 1u << 1,
        InAir = 1u << 2,
        Jumping = 1u << 3,
        Climbing = 1u << 4,
        FallingBusy = 1u << 5,
        HeadUnderwater = 1u << 6,
        BodyOnWater = 1u << 7,
        StillDrowning = 1u << 8,
        Floating = 1u << 9,
        FacingRight = 1u << 10,
        OxygenDepleted = 1u << 11,   // เป็ด: oxygen หมดแล้ว (host เริ่มดาเมจจมน้ำ)
        ReadingTutorial = 1u << 12,  // เปิดหน้าสอนเล่นอยู่ (host ไม่ให้โดนดาเมจ/มึน)
        Diving = 1u << 16,           // เป็ด
        Flying = 1u << 20,           // นก
        AlreadyFloating = 1u << 21,
        AlreadyFly = 1u << 22,
        ReportValid = 1u << 31,      // input: มีตัวละครของเครื่องนั้นแล้ว
    }

    // ตัวละครของเครื่องนี้ (OnInput อ่านไปส่งให้ host)
    public static MovementCharacter Local { get; private set; }

    private MoveFlags _ownerFlags;
    [Networked] public uint RepFlags { get; set; }
    [Networked] public Vector2 RepPosition { get; set; }
    [Networked] public Vector2 RepVelocity { get; set; }
    // แพเลื่อนที่ตัวของ host ยืนอยู่ — จอ client วาดตัวนี้ย้อนหลังแต่วาดแพตามเวลาทำนาย จึงต้องรู้ว่าติดแพไหน (P-2)
    [Networked] public NetworkId RepPlatformId { get; set; }

    private bool IsSpawnedObject => Object != null && Object.IsValid;
    public bool IsOwner => IsSpawnedObject && HasInputAuthority;
    // host กับตัวของ client
    private bool IsMirror => IsSpawnedObject && HasStateAuthority && !HasInputAuthority;
    // client กับตัวของตัวเอง
    private bool IsClientOwner => IsSpawnedObject && HasInputAuthority && !HasStateAuthority;

    public MoveFlags OwnerFlags => _ownerFlags;

    public bool GetMoveFlag(MoveFlags flag)
    {
        MoveFlags flags = (!IsSpawnedObject || HasInputAuthority) ? _ownerFlags : (MoveFlags)RepFlags;
        return (flags & flag) != 0;
    }

    // หน้าสอนเล่นเปิดเฉพาะจอคนอ่านและโลกไม่หยุด (TU-1) — host กันดาเมจ/มึนจากธงนี้แทนการ freeze
    public bool IsReadingTutorial => GetMoveFlag(MoveFlags.ReadingTutorial);

    public void SetMoveFlag(MoveFlags flag, bool on)
    {
        if (IsSpawnedObject && !HasInputAuthority) return;
        if (on) _ownerFlags |= flag;
        else _ownerFlags &= ~flag;
    }

    // ความเร็วที่เครื่องนี้รู้ของตัวนี้: เจ้าของใช้ของจริง, เครื่องอื่นใช้ค่าที่ sync
    public Vector2 CurrentVelocity => IsOwner ? rb2D.linearVelocity : RepVelocity;

    // ล็อกการเคลื่อนที่ที่ host ตัดสิน — เจ้าของอ่านอย่างเดียว
    protected virtual bool HostMovementLocked => isStunned || IsBeingCarried;

    // ---- event จาก host -> เจ้าของ (วาร์ป / แรงกระแทก) ----
    [Networked] public byte EventSeq { get; set; }
    [Networked] private Vector2 EventPosition { get; set; }
    [Networked] private Vector2 EventImpulse { get; set; }
    [Networked] private NetworkBool EventTeleport { get; set; }
    [Networked] private NetworkBool EventResetVelocity { get; set; }

    private byte _appliedEventSeq;   // เจ้าของ: seq ล่าสุดที่ทำแล้ว (ส่งกลับใน input)
    private byte _ownerAckedSeq;     // host: seq ล่าสุดที่ client ยืนยันว่าทำแล้ว
    public byte AppliedEventSeq => _appliedEventSeq;

    private const float TeleportSnapDistance = 2f;

    // host: ให้เจ้าของวาร์ป/ใส่แรงกับตัวเอง — ถ้ามี event ค้างที่เจ้าของยังไม่ทำ รวมเข้าด้วยกัน (วาร์ปล่าสุดชนะ, แรงบวกกัน)
    public void RaiseOwnerEvent(Vector2? teleportTo, Vector2 impulse, bool resetVelocity)
    {
        if (!HasStateAuthority) return;

        byte acked = HasInputAuthority ? _appliedEventSeq : _ownerAckedSeq;
        if (EventSeq == acked)
        {
            EventTeleport = false;
            EventImpulse = Vector2.zero;
            EventResetVelocity = false;
        }

        if (teleportTo.HasValue)
        {
            EventTeleport = true;
            EventPosition = teleportTo.Value;
            EventImpulse = Vector2.zero;
        }

        EventImpulse += impulse;
        EventResetVelocity = EventResetVelocity || resetVelocity || teleportTo.HasValue;
        EventSeq = (byte)(EventSeq == byte.MaxValue ? 1 : EventSeq + 1);

        // ฝั่ง host เห็นตัวของ client ที่จุดวาร์ปทันที (ไม่รับตำแหน่งจาก input จนกว่า client จะยืนยัน)
        if (teleportTo.HasValue && IsMirror)
        {
            RepPosition = teleportTo.Value;
            RepVelocity = Vector2.zero;
            rb2D.position = teleportTo.Value;
            transform.position = teleportTo.Value;
        }
    }

    // เจ้าของ: ทำ event ที่ยังไม่ได้ทำ (ครั้งเดียวต่อ seq)
    private void ApplyPendingOwnerEvent()
    {
        byte seq = EventSeq;
        if (seq == _appliedEventSeq) return;
        _appliedEventSeq = seq;

        if (EventResetVelocity) rb2D.linearVelocity = Vector2.zero;

        if (EventTeleport)
        {
            Vector2 pos = EventPosition;
            rb2D.position = pos;
            transform.position = pos;
            SnapOwnerVisual(pos);
        }

        Vector2 impulse = EventImpulse;
        if (impulse != Vector2.zero)
        {
            if (rb2D.IsSleeping()) rb2D.WakeUp();
            rb2D.AddForce(impulse, ForceMode2D.Impulse);

            // แรงกระเด็น/ถูกโยน: ใช้การคุมแบบเดิมจนลงพื้น ไม่ให้การตั้งความเร็วตรงทับแรงนี้ (MV-5)
            _externalForceUntilTick = Runner.Tick.Raw + SecondsToTicks(externalForceTime);
            _externalForceUntilLanding = true;
        }
    }

    // host กับตัวของ client: วางตามตำแหน่งที่ client คำนวณเองแล้วส่งมา แล้ว sync ให้เครื่องอื่น
    private void ApplyOwnerReport(NetworkInputData input)
    {
        if ((input.OwnerFlags & (uint)MoveFlags.ReportValid) == 0) return;

        _ownerAckedSeq = input.OwnerAckSeq;

        MakeRemoteBody();

        RepFlags = input.OwnerFlags;

        // client ยังไม่ได้วาร์ปตาม event ล่าสุด -> ตำแหน่งที่ส่งมายังเป็นของก่อนวาร์ป ไม่ใช้
        if (EventTeleport && input.OwnerAckSeq != EventSeq) return;

        Vector2 target = input.OwnerPosition;

        // ถูกยกอยู่: ฝั่ง host ติดหัวผู้ยกตามตำแหน่งที่ host เห็น (client ติดตามภาพผู้ยกที่ช้ากว่า)
        if (IsBeingCarried && Runner.TryFindObject(CarrierId, out var carrierObj)
            && carrierObj.TryGetComponent<MovementCharacter>(out var carrier))
        {
            target = (Vector2)carrier.transform.position + Vector2.up * betweenCarryPosition;
        }

        if (((Vector2)rb2D.position - target).sqrMagnitude > TeleportSnapDistance * TeleportSnapDistance)
        {
            rb2D.position = target;
            transform.position = target;
        }
        else
        {
            rb2D.MovePosition(target);
        }

        RepPosition = target;
        RepVelocity = input.OwnerVelocity;
    }

    // host กับตัวของ host เอง: sync ให้ client
    private void WriteReplicaFromOwner()
    {
        RepPosition = rb2D.position;
        RepVelocity = rb2D.linearVelocity;
        RepFlags = (uint)_ownerFlags;

        NetworkObject platform = null;
        if (IsGrounded && currentPlatform != null) currentPlatform.TryGetComponent(out platform);
        RepPlatformId = platform != null ? platform.Id : default;
    }

    // ---- body ของเจ้าของฝั่ง client ----
    // NetworkRigidbody2D ใน prefab ตั้ง body กลับเป็นค่าจาก host ทุกครั้งที่ resimulate (รวมถึงเป็น kinematic ตามตัวบน host)
    // -> เก็บค่าของเราเองหลังทุก tick ใหม่ แล้วตั้งกลับก่อนทุก tick
    private struct OwnerBodyState
    {
        public bool Valid;
        public Vector2 Position;
        public float Rotation;
        public Vector2 Velocity;
        public float AngularVelocity;
        public float GravityScale;
        public float LinearDamping;
        public float AngularDamping;
        public RigidbodyConstraints2D Constraints;
    }
    private OwnerBodyState _ownerBody;

    private void CaptureOwnerBody()
    {
        if (rb2D == null) return;
        _ownerBody.Valid = true;
        _ownerBody.Position = rb2D.position;
        _ownerBody.Rotation = rb2D.rotation;
        _ownerBody.Velocity = rb2D.linearVelocity;
        _ownerBody.AngularVelocity = rb2D.angularVelocity;
        _ownerBody.GravityScale = rb2D.gravityScale;
        _ownerBody.LinearDamping = rb2D.linearDamping;
        _ownerBody.AngularDamping = rb2D.angularDamping;
        _ownerBody.Constraints = rb2D.constraints;
    }

    void IBeforeTick.BeforeTick()
    {
        if (!IsClientOwner || rb2D == null) return;

        if (rb2D.bodyType != RigidbodyType2D.Dynamic) rb2D.bodyType = RigidbodyType2D.Dynamic;
        if (!_ownerBody.Valid) return;

        rb2D.position = _ownerBody.Position;
        rb2D.rotation = _ownerBody.Rotation;
        transform.position = _ownerBody.Position;
        rb2D.linearVelocity = _ownerBody.Velocity;
        rb2D.angularVelocity = _ownerBody.AngularVelocity;
        rb2D.gravityScale = _ownerBody.GravityScale;
        rb2D.linearDamping = _ownerBody.LinearDamping;
        rb2D.angularDamping = _ownerBody.AngularDamping;
        rb2D.constraints = _ownerBody.Constraints;
    }

    void IAfterTick.AfterTick()
    {
        if (!IsOwner || rb2D == null || !Runner.IsForward) return;

        // ภาพของเจ้าของ: ตำแหน่งปลาย 2 tick ล่าสุด ไว้ interpolate ใน Render
        _ownerVisualPrev = _ownerVisualCurr;
        _ownerVisualCurr = rb2D.position;
        _hasOwnerVisual = true;

        if (!HasStateAuthority) CaptureOwnerBody();
    }

    // ---- ภาพ ----
    private Vector2 _ownerVisualPrev;
    private Vector2 _ownerVisualCurr;
    private bool _hasOwnerVisual;

    private void SnapOwnerVisual(Vector2 pos)
    {
        _ownerVisualPrev = pos;
        _ownerVisualCurr = pos;
        _hasOwnerVisual = true;
    }

    // เจ้าของ: body เดินทีละ tick -> วาดภาพระหว่างปลาย tick ก่อนหน้ากับ tick ล่าสุด
    private void RenderOwnerVisual()
    {
        if (visualTransform == null || !_hasOwnerVisual) return;

        Vector2 pos = Vector2.Lerp(_ownerVisualPrev, _ownerVisualCurr, Runner.LocalAlpha);
        visualTransform.position = new Vector3(pos.x, pos.y, visualTransform.position.z);
    }

    // ตัวที่เครื่องนี้ไม่ได้เป็นเจ้าของ: วางตามตำแหน่งที่ sync (interpolate) — body เป็น kinematic
    private void RenderRemoteBody()
    {
        var interpolator = new NetworkBehaviourBufferInterpolator(this);
        Vector2 pos = interpolator ? interpolator.Vector2(nameof(RepPosition)) : RepPosition;
        pos += PlatformRenderOffset(interpolator);
        transform.position = new Vector3(pos.x, pos.y, transform.position.z);

        if (coll2D != null && coll2D.isTrigger != IsBeingCarried) coll2D.isTrigger = IsBeingCarried;
    }

    // ตัวที่ถูกวาดย้อนหลังแต่ยืนบนแพเลื่อน: เลื่อนตามระยะที่แพขยับไปแล้วระหว่างเวลาที่ตัวนี้ถูกวาดกับเวลาที่แพถูกวาด
    // -> ติดแพบนจอ client (P-2); บน host สองเวลานี้เท่ากัน ได้ 0
    private const float PlatformOffsetBlendTime = 0.12f;
    private MovingPlateform _renderPlatform;
    private float _renderPlatformWeight;

    private Vector2 PlatformRenderOffset(NetworkBehaviourBufferInterpolator interpolator)
    {
        bool onPlatform = false;
        if (RepPlatformId.IsValid && Runner.TryFindObject(RepPlatformId, out var platformObj)
            && platformObj.TryGetComponent<MovingPlateform>(out var platform))
        {
            _renderPlatform = platform;
            onPlatform = true;
        }

        // เกลี่ยน้ำหนักตอนขึ้น/ลงแพ กันภาพกระโดด (ตอนลงยังเทียบกับแพเดิมจนน้ำหนักเป็น 0)
        _renderPlatformWeight = Mathf.MoveTowards(_renderPlatformWeight, onPlatform ? 1f : 0f, Time.deltaTime / PlatformOffsetBlendTime);
        if (_renderPlatformWeight <= 0f || _renderPlatform == null || !interpolator) return Vector2.zero;

        float drawnTick = interpolator.From.Tick.Raw + (interpolator.To.Tick.Raw - interpolator.From.Tick.Raw) * interpolator.Alpha;
        Vector3 moved = _renderPlatform.PositionAtTime(_renderPlatform.LocalRenderTime) - _renderPlatform.PositionAtTime(drawnTick * Runner.DeltaTime);
        return (Vector2)moved * _renderPlatformWeight;
    }

    // ตัวที่เครื่องนี้ไม่ได้เป็นเจ้าของ: kinematic วางตามตำแหน่งที่ sync
    // useFullKinematicContacts: kinematic ปกติไม่ยิง trigger/contact กับ collider ที่อยู่นิ่ง (กับดัก จุดตาย checkpoint)
    // — ฝั่ง host ต้องได้ event เหล่านั้นจากตัวของ client
    private void MakeRemoteBody()
    {
        if (rb2D == null) return;
        if (rb2D.bodyType != RigidbodyType2D.Kinematic) rb2D.bodyType = RigidbodyType2D.Kinematic;
        if (!rb2D.useFullKinematicContacts) rb2D.useFullKinematicContacts = true;
    }

    // เจ้าของ: ล้างสถานะการเคลื่อนที่ตอนเกิดใหม่/รีเซ็ต (host ไม่ได้ถือค่าพวกนี้ของตัว client)
    protected virtual void ResetOwnerMovementState()
    {
        bool facingRight = GetMoveFlag(MoveFlags.FacingRight);
        _ownerFlags = MoveFlags.None;
        SetMoveFlag(MoveFlags.FacingRight, facingRight);

        isMoveAble = true;
        isOptional = false;
        isSpeedoptional = false;
        IsFalling = false;
        isInClimbZone = false;
        jumpedFromClimb = false;
        currentPlatform = null;
        _ownerWasCarried = false;

        _lastGroundedTick = NoTick;
        _jumpPressedTick = NoTick;
        _externalForceUntilTick = NoTick;
        _externalForceUntilLanding = false;
        JumpedThisTick = false;
    }
    #endregion

    #region LifeCycle Effect
    [Header("LifeCycle Effect")]
    [SerializeField] public LifeCycle lifeCycle;
    #endregion

    private void Awake()
    {
        if (stats == null) stats = GetComponent<CharacterStats>();
        if (rb2D == null) rb2D = GetComponent<Rigidbody2D>();
        if (coll2D == null) coll2D = GetComponent<Collider2D>();
        if (localGUI == null) localGUI = GetComponent<PlayerGUI>();

        // กันผู้เล่นถูกแรงดัน (เช่นเครื่องพ่นลม) ดันทะลุ collider พื้นที่เป็นเส้นขอบ
        if (rb2D != null) rb2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (cAnimation == null) cAnimation = GetComponentInChildren<CharacterAnimation>();
        if (_damageFlash == null) _damageFlash = GetComponentInChildren<DamageFlash>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (cAnimation != null) cAnimation.InitializeMovement(this);
        if (spriteRenderer != null) originalSortingOrder = spriteRenderer.sortingOrder;

        if (visualTransform == null && transform.Find("Player_Animation") != null)
        {
            visualTransform = transform.Find("Player_Animation");
        }

        if (lifeCycle == null) lifeCycle = GetComponentInChildren<LifeCycle>();

        // NetworkRigidbody2D ใน prefab: ให้วาดลง child เปล่า — ตำแหน่งและภาพของตัวละครคุมเองทั้งหมด (NR-12)
        if (TryGetComponent<Fusion.Addons.Physics.NetworkRigidbody2D>(out var netRb))
        {
            var unusedTarget = new GameObject("NetworkRigidbodyUnusedTarget").transform;
            unusedTarget.SetParent(transform, false);
            netRb.InterpolationTarget = unusedTarget;
        }
    }

    #region CoreNetwork

    public override void Spawned()
    {
        bool isThisCharacterBird = (stats != null && stats.skinType == characterType.Bird);

        if (cAnimation != null) cAnimation.InitializeMovement(this);

        // TEMP verify M-4: static unlock flags as seen when this character spawns
        VerifyLog.Log($"MovementCharacter.Spawned {(isThisCharacterBird ? "Bird" : "Duck")} stateAuth={HasStateAuthority} inputAuth={HasInputAuthority} | {VerifyLog.Statics()}");

        if (HasStateAuthority)
        {
            isBird = isThisCharacterBird;
            characterMaxHealth = stats != null ? stats.s_maxHealth : 5;
            currentHealth = characterMaxHealth;
            isDead = false;

            if (isThisCharacterBird && this is Bird_Moveset bird)
            {
                bird.isFlyUnlocked = PlayerInterface._birdFlyUnlocked;
                bird.isThrowUnlocked = PlayerInterface._birdThrowUnlocked;
            }
            else if (!isThisCharacterBird && this is Duck_Moveset duck)
            {
                duck.isDiveUnlocked = PlayerInterface._duckDiveUnlocked;
                duck.isSmashUnlocked = PlayerInterface._duckSmashUnlocked;
            }
        }
        else if (stats != null)
        {
            characterMaxHealth = stats.s_maxHealth;
        }

        CameraCharacter cam = GetComponentInChildren<CameraCharacter>();
        if (cam != null)
        {
            if (HasInputAuthority)
                cam.InitializeAsLocal(visualTransform);
            else
                cam.DisableForRemote();
        }

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        if (spriteRenderer != null)
        {
            spriteRenderer.GetPropertyBlock(mpb);
            mpb.SetColor("_OutlineColor", isThisCharacterBird ? bird_Color : duck_Color);
            spriteRenderer.SetPropertyBlock(mpb);
        }

        if (cAnimation != null)
        {
            cAnimation.UpdateSkin(stats.skinType);
            if (lifeCycle != null) lifeCycle.Initialize(isThisCharacterBird);
        }

        // เจ้าของจำลองตัวเอง (dynamic) / เครื่องอื่นวางตามตำแหน่งที่ sync (kinematic) — NR-12
        if (HasInputAuthority) Local = this;

        if (rb2D != null)
        {
            if (HasInputAuthority)
            {
                rb2D.bodyType = RigidbodyType2D.Dynamic;
            }
            else
            {
                MakeRemoteBody();
                rb2D.linearVelocity = Vector2.zero;
            }
        }

        if (HasStateAuthority)
        {
            RepPosition = transform.position;
        }
        else if (HasInputAuthority)
        {
            transform.position = RepPosition;
            if (rb2D != null) rb2D.position = RepPosition;
        }
        SnapOwnerVisual(transform.position);

        JumpCooldown = TickTimer.CreateFromSeconds(Runner, jumpCooldownTime);

        if (GameManager.Instance != null) GameManager.Instance.RegisterPlayer(this);

        // เดินทะลุตัวมอน (มอนที่ spawn ทีหลังจัดการเองใน BaseMonster.Spawned)
        foreach (var monster in FindObjectsByType<BaseMonster>(FindObjectsSortMode.None))
            BaseMonster.IgnorePlayerCollision(monster, this);
        if (localGUI != null) localGUI.SetCharacterType(isThisCharacterBird);

        if (HasInputAuthority)
        {
            Invoke(nameof(ForceUpdateUI), 0.5f);
        }

        isMoveAble = true;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Local == this) Local = null;
    }

    private void ForceUpdateUI()
    {
        OnHealthChanged();
        OnCharacterTypeChanged();
    }

    public override void FixedUpdateNetwork()
    {
        bool hasInput = GetInput(out NetworkInputData input) && !IsSoloFollower;

        if (IsMirror)
        {
            // host กับตัวของ client: วางตามตำแหน่งที่ client คำนวณเองแล้วส่งมา
            if (hasInput) ApplyOwnerReport(input);
        }
        else if (HasInputAuthority && (HasStateAuthority || Runner.IsForward))
        {
            // เจ้าของตัวขยับตัวเอง — client ทำเฉพาะ tick ใหม่ ไม่ย้อนคำนวณตอน resimulate (ไม่ถูก host ดึงกลับ)
            OwnerSimulate(input, hasInput);
        }

        if (HasStateAuthority)
        {
            HostRules(input, hasInput);
            if (HasInputAuthority) WriteReplicaFromOwner();
        }

        // จำปุ่มของ tick นี้ไว้เทียบ tick หน้า — อัปเดตทุก tick แม้ข้ามการจำลอง (ตาย / ยังโหลดไม่เสร็จ / ย้อนคำนวณ)
        if (hasInput) PreviousButtons = input.Buttons;
    }

    // เจ้าของตัว: การเคลื่อนที่ทั้งหมดของตัวเอง
    private void OwnerSimulate(NetworkInputData input, bool hasInput)
    {
        JumpedThisTick = false;
        ApplyPendingOwnerEvent();

        // ตั้งก่อนจุด return ด้านล่าง — ไม่งั้นตายระหว่างเปิดหน้าสอนเล่นแล้วธงค้าง (TU-1)
        SetMoveFlag(MoveFlags.ReadingTutorial, TutorialUIManager.Instance != null && TutorialUIManager.Instance.IsTutorialOpen);

        if (GameManager.Instance == null || GameManager.Instance.Object == null || !GameManager.Instance.Object.IsValid
            || !GameManager.Instance.isLoadMapDone || !GameManager.Instance.IsGameReady)
        {
            rb2D.linearVelocity = Vector2.zero;
            return;
        }

        if (isDead) return;

        UpdateOwnerCarriedState();

        bool carried = IsBeingCarried;

        if (carried)
        {
            rb2D.gravityScale = 0f;
            rb2D.linearVelocity = Vector2.zero;
            if (coll2D != null && !coll2D.isTrigger) coll2D.isTrigger = true;
            isMoveAble = false;

            // ผู้ยกอาจเป็นตัวของอีกเครื่อง — ใช้ตำแหน่งที่วาดอยู่ในเครื่องนี้
            if (Runner.TryFindObject(CarrierId, out var carrierObj) && carrierObj.TryGetComponent<MovementCharacter>(out var carrier))
            {
                Vector2 targetPos = (Vector2)carrier.transform.position + Vector2.up * betweenCarryPosition;
                rb2D.MovePosition(targetPos);
            }
        }

        bool isMenuOpen = GameManager.Instance.IsPaused;
        bool wasMenuOpenBeforeThisTick = wasMenuOpenLastTick;
        wasMenuOpenLastTick = isMenuOpen;

        CheckGround();
        UpdateMovementFeelState();

        // ติดหนึบกับ moving platform แนวนอน (ตำแหน่งแพคำนวณจาก tick ตรงกันทุกเครื่อง — NR-10)
        // แนวตั้งปล่อยให้ kinematic collision จัดการ — กัน double-count ที่เคยทำให้โดนผลักลง
        if (IsGrounded && currentPlatform != null && !carried && !isMenuOpen)
        {
            Vector3 cur = currentPlatform.position;
            rb2D.position += new Vector2(cur.x - lastPlatformPos.x, 0f);
            lastPlatformPos = cur;
        }

        bool wasInZone = isInClimbZone;
        isInClimbZone = CheckInClimbZone();

        if (wasInZone && !isInClimbZone && isClimbing)
        {
            ExitClimbState();
        }

        if (hasInput)
        {
            if (isMenuOpen)
            {
                cAnimation.UpdateAnimationController(Vector2.zero);

                HandleEtcInput(input);
            }
            else
            {
                if (isInClimbZone && !isClimbing && !HostMovementLocked && Mathf.Abs(input.vertical) > 0.1f)
                {
                    EnterClimbState();
                }

                if (isClimbing)
                {
                    if (input.KeybindJump && !jumpedFromClimb)
                    {
                        ExitClimbState();
                        jumpedFromClimb = true;
                        ClimbJump();
                    }
                    else
                    {
                        HandleMovement(input);
                        rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, input.vertical * climbSpeed);
                    }
                }
                else if (isMoveAble && !HostMovementLocked)
                {
                    HandleMovement(input);
                    HandleJump(input);
                    HandleJumpPhysics(input);
                }

                HandleEtcInput(input);
            }
        }

        OnOwnerSimulateSpecific(input, hasInput);

        if (isMenuOpen)
        {
            rb2D.linearVelocity = Vector2.zero;
            rb2D.gravityScale = 0f;
        }
        else if (wasMenuOpenBeforeThisTick)
        {
            rb2D.gravityScale = normalGravity;
        }
    }

    // host: กติกาที่ host ตัดสินให้ทุกตัว (ตัวของ client ใช้ปุ่มจาก input) — กด E, ทิ้งของ, มึน, ตาย/เกิดใหม่ และของเฉพาะเป็ด/นก
    private void HostRules(NetworkInputData input, bool hasInput)
    {
        if (GameManager.Instance == null || GameManager.Instance.Object == null || !GameManager.Instance.Object.IsValid) return;

        if (!GameManager.Instance.isLoadMapDone)
        {
            hasSetInitialPosition = false;
            return;
        }

        if (!hasSetInitialPosition)
        {
            hasSetInitialPosition = true;
            GameManager.Instance.PlayerFinishedLoading();
            RaiseOwnerEvent(GameManager.Instance.GetRespawnPosition(), Vector2.zero, true);
        }

        if (!GameManager.Instance.IsGameReady) return;

        if (isDead)
        {
            if (canbeRespawn && respawnTimer.Expired(Runner)) Respawn();
            return;
        }

        TickStun();
        TickTutorialGrace();

        bool isMenuOpen = GameManager.Instance.IsPaused;

        InteractUsedOnWorldThisTick = false;
        if (hasInput && !isMenuOpen && !isClimbing)
        {
            if (!IsInteractBusy) HandleInteraction(input);
            HandleDrop(input);
        }

        OnHostRulesSpecific(input, hasInput, isMenuOpen);
    }

    public override void Render()
    {
        // ตัวตามหลังของ Solo ใช้ภาพแบบเจ้าของ: RenderRemoteBody เขียน root transform ย้อนหลังแล้ว physics sync ทับตำแหน่ง body (SO-7)
        if (IsOwner || IsSoloFollower) RenderOwnerVisual();
        else RenderRemoteBody();

        if (HasInputAuthority) CheckInteractable();

        ManageMovementSounds();
    }

    // ภาพตัวที่ถูกยกติดหัวผู้ยก — ต้องเป็นตัวเขียนตำแหน่งภาพคนสุดท้ายของเฟรม (LateUpdate หลัง Render ของทุกตัว)
    private void LateUpdate()
    {
        if (Object == null || !Object.IsValid || Runner == null) return;

        if (IsBeingCarried
            && Runner.TryFindObject(CarrierId, out var duckObj)
            && duckObj.TryGetComponent<MovementCharacter>(out var duckMC))
        {
            if (spriteRenderer != null) spriteRenderer.sortingOrder = originalSortingOrder - 1;

            if (visualTransform != null && duckMC.visualTransform != null)
            {
                visualTransform.position = duckMC.visualTransform.position
                                           + Vector3.up * betweenCarryPosition;
                _wasVisuallyCarried = true;
            }
        }
        else
        {
            if (spriteRenderer != null) spriteRenderer.sortingOrder = originalSortingOrder;

            if (_wasVisuallyCarried && visualTransform != null)
            {
                visualTransform.localPosition = Vector3.zero;
                _wasVisuallyCarried = false;
            }
        }
    }

    #endregion

    #region Menu & Etc Input

    private void HandleEtcInput(NetworkInputData input)
    {
        bool isEscPressed = WasPressed(input, InputButton.Esc);
        if (isEscPressed)
        {
            if (Runner.IsForward)
            {
                if (HasStateAuthority)
                {
                    if (GameManager.Instance != null) GameManager.Instance.RequestOpenMenu();
                }
                else if (HasInputAuthority)
                {
                    RPC_RequestOpenMenuFromClient();
                }
            }
        }

        bool isTabPressed = WasPressed(input, InputButton.Tab);
        if (isTabPressed && HasInputAuthority && Runner.IsForward)
        {
            // เฉพาะเครื่องเจ้าของตัว — เดิมบน host ปิดหน้าสอนเล่นของ host ตอน client กด Tab ด้วย (FB3-2)
            if (PlayerInterface.Instance != null)
                PlayerInterface.Instance.HideNote();
            TutorialUIManager.Instance?.HideTutorial();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestOpenMenuFromClient()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RequestOpenMenu();
        }
    }

    #endregion

    #region Movement

    private void HandleMovement(NetworkInputData input)
    {
        if (UseForceControl) HandleForceMovement(input);
        else HandleVelocityMovement(input);

        cAnimation.UpdateAnimationController(new Vector2(input.horizontal, rb2D.linearVelocity.y));
    }

    // ตั้งความเร็วแนวนอนตรง: ออกตัว/หยุดตามเวลาที่ตั้ง กลางอากาศคุมได้น้อยกว่า (MV-1)
    private void HandleVelocityMovement(NetworkInputData input)
    {
        float maxSpeed = runSpeed;
        float targetSpeed = input.horizontal * maxSpeed;
        float vx = rb2D.linearVelocity.x;

        bool pushing = Mathf.Abs(targetSpeed) > 0.01f;
        bool sameDirection = Mathf.Sign(vx) == Mathf.Sign(targetSpeed);
        bool accelerating = pushing && (!sameDirection || Mathf.Abs(vx) < Mathf.Abs(targetSpeed));

        float rate = maxSpeed / Mathf.Max(accelerating ? groundAccelTime : groundDecelTime, 0.001f);
        if (!IsGrounded) rate *= airControl;
        // เร็วเกิน maxSpeed ไปทางที่กดอยู่ (เพิ่งหลุดจากลม/แรงกระเด็น): ค่อย ๆ ลด ไม่หยุดกึก
        if (pushing && sameDirection && Mathf.Abs(vx) > maxSpeed) rate *= overSpeedReduce;

        vx = Mathf.MoveTowards(vx, targetSpeed, rate * Runner.DeltaTime);
        rb2D.linearVelocity = new Vector2(vx, rb2D.linearVelocity.y);
    }

    // สูตรเดิม (ใช้แรง): ในน้ำ/ร่อน/ลอย และช่วงมีแรงภายนอก (ลม, แรงกระเด็น, ถูกโยน) — คงสมดุลเดิมของด่าน
    private void HandleForceMovement(NetworkInputData input)
    {
        float targetSpeed = input.horizontal * stats.maxSpeed;
        float currentSpeed = rb2D.linearVelocity.x;
        float accelRate = isSpeedoptional
            ? (Mathf.Abs(targetSpeed) > 0.01f ? accelerationSpeedOptional : decelerationSpeedOptional)
            : (Mathf.Abs(targetSpeed) > 0.01f ? stats.acceleration : stats.deceleration);

        float speedDif = targetSpeed - currentSpeed;
        rb2D.AddForce(Vector2.right * (speedDif * accelRate));
    }

    private bool UseForceControl =>
        isSpeedoptional || isOptional || _externalForceUntilLanding || Runner.Tick.Raw <= _externalForceUntilTick;

    // กับดักลมเรียกทุก tick ที่ดันตัวนี้ — อยู่ในลมใช้สูตรเดิม ลมจึงแรงเท่าเดิม (MV-5)
    // +1 tick: กันลำดับ FixedUpdateNetwork ของกับดักกับผู้เล่นที่ไม่แน่นอน
    public void NotifyExternalForce()
    {
        if (Runner == null) return;
        _externalForceUntilTick = Mathf.Max(_externalForceUntilTick, Runner.Tick.Raw + 1);
    }

    // หลังเช็คพื้นทุก tick: จำเวลายืนล่าสุด (coyote), จบช่วงแรงภายนอกเมื่อลงพื้น, สลับแรงเสียดทานตอนลอย
    private void UpdateMovementFeelState()
    {
        int tick = Runner.Tick.Raw;

        if (IsGrounded && !isJumping) _lastGroundedTick = tick;
        if (IsGrounded && tick > _externalForceUntilTick) _externalForceUntilLanding = false;

        UpdateAirFriction();
    }

    // ลอยอยู่: ตัวไม่มีแรงเสียดทาน ไม่ติดกำแพงตอนกดเข้าหา; ยืน: กลับไปใช้ material ของ Rigidbody2D (นกสลับเองตอนบิน) (MV-4)
    // collider ตอนเป็ดยกนก (carryCollider) Duck_Moveset ตั้ง material เอง
    private void UpdateAirFriction()
    {
        Collider2D body = normalCollider != null ? normalCollider : coll2D;
        if (body == null) return;

        PhysicsMaterial2D wanted = IsGrounded ? null : AirMaterial;
        if (body.sharedMaterial != wanted) body.sharedMaterial = wanted;
    }

    private PhysicsMaterial2D AirMaterial
    {
        get
        {
            if (zeroFiction != null) return zeroFiction;
            if (s_airMaterial == null) s_airMaterial = new PhysicsMaterial2D("PlayerAirZeroFriction") { friction = 0f, bounciness = 0f };
            return s_airMaterial;
        }
    }

    private int SecondsToTicks(float seconds) => Mathf.RoundToInt(seconds / Runner.DeltaTime);

    protected void ClearJumpBuffer() => _jumpPressedTick = NoTick;

    #endregion

    #region Jump System

    // กระโดดตามจังหวะกด (ไม่ใช่กดค้าง) + jump buffer + coyote time (MV-2)
    protected virtual void HandleJump(NetworkInputData input)
    {
        int tick = Runner.Tick.Raw;
        if (WasPressed(input, InputButton.Jump)) _jumpPressedTick = tick;

        if (jumpedFromClimb)
        {
            if (IsGrounded) jumpedFromClimb = false;
            else return;
        }

        bool buffered = tick - _jumpPressedTick <= SecondsToTicks(jumpBufferTime);
        bool onGroundRecently = tick - _lastGroundedTick <= SecondsToTicks(coyoteTime);

        if (buffered && onGroundRecently && JumpCooldown.ExpiredOrNotRunning(Runner))
        {
            isJumping = true;
            JumpedThisTick = true;
            _jumpPressedTick = NoTick;
            _lastGroundedTick = NoTick;

            if (HasInputAuthority && jumpSoundClip != null)
            {
                AudioManager.Instance?.PlayClipAtPosition(jumpSoundClip, transform.position);
            }
            rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, 0f);
            rb2D.AddForce(Vector2.up * stats.s_jumpForce, ForceMode2D.Impulse);
            IsGrounded = false;
            JumpCooldown = TickTimer.CreateFromSeconds(Runner, jumpCooldownTime);
        }
    }

    private void HandleJumpPhysics(NetworkInputData input)
    {
        if (!isJumping) return;

        if (CheckHeadBump()) return;

        if (Mathf.Abs(rb2D.linearVelocity.y) <= apexThreshold)
        {
            ApplyApexHang();
        }
        else if (rb2D.linearVelocity.y > apexThreshold && !input.KeybindJump)
        {
            ApplyJumpCut();
        }
        else
        {
            rb2D.gravityScale = normalGravity;
        }
    }

    private bool CheckHeadBump()
    {
        LayerMask mask = LayerMask.GetMask("Ground", "Platform");
        if (!Physics2D.Raycast(transform.position, Vector2.up, headBumpCheckDistance, mask))
            return false;

        isJumping = false;
        rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, headBumpDownwardVelocity);
        return true;
    }

    private void ApplyApexHang()
    {
        rb2D.gravityScale = normalGravity * apexGravityMultiplier;
    }

    private void ApplyJumpCut()
    {
        rb2D.gravityScale = normalGravity * jumpCutMultiplier;
    }

    #endregion

    #region Climb System

    // ใช้ซ้ำทุก tick (รวม tick ที่ client ย้อนคำนวณ) แทน OverlapBoxAll ที่จอง array ใหม่ทุกครั้ง
    private readonly Collider2D[] climbHits = new Collider2D[8];

    private bool CheckInClimbZone()
    {
        int hitCount = Runner.GetPhysicsScene2D().OverlapBox(
            transform.position,
            new Vector2(0.5f, 1f),
            0f,
            climbHits,
            climbableMask
        );

        for (int i = 0; i < hitCount; i++)
        {
            var hit = climbHits[i];
            if (hit.GetComponent<IClimbable>() != null
             || hit.GetComponentInParent<IClimbable>() != null)
                return true;
        }
        return false;
    }

    private void EnterClimbState()
    {
        if (isClimbing) return;
        isClimbing = true;
        IsGrounded = false;
        isJumping = false;
        rb2D.gravityScale = 0f;
        rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, 0f);
    }

    private void ExitClimbState()
    {
        if (!isClimbing) return;
        isClimbing = false;
        isMoveAble = true;
        rb2D.gravityScale = normalGravity;
    }

    private void ClimbJump()
    {
        isJumping = true;
        rb2D.linearVelocity = Vector2.zero;
        rb2D.AddForce(Vector2.up * stats.s_jumpForce, ForceMode2D.Impulse);
        IsGrounded = false;
        JumpCooldown = TickTimer.CreateFromSeconds(Runner, jumpCooldownTime);

        if (HasInputAuthority && jumpSoundClip != null)
            AudioManager.Instance?.PlayClipAtPosition(jumpSoundClip, transform.position);
    }

    #endregion

    #region Interaction

    private void HandleInteraction(NetworkInputData input)
    {
        bool isEPressed = WasPressed(input, InputButton.Interact);
        if (isEPressed)
        {
            Collider2D[] hitsItem = Physics2D.OverlapCircleAll(transform.position, interactRadius);

            if (isBird && HeldItemName.ToString() == "")
            {
                foreach (var hit in hitsItem)
                {
                    if (hit.gameObject == gameObject) continue;

                    if (hit.TryGetComponent<ThrowAbleItem>(out var throwableItem))
                    {
                        if (throwableItem.AlreadyThrow) continue;
                        if (!throwableItem.IsStationary()) continue;

                        VerifyLog.Log($"HandleInteraction PICKUP '{hit.name}' by {(isBird ? "Bird" : "Duck")} stateAuth={HasStateAuthority} inputAuth={HasInputAuthority} isForward={Runner.IsForward} tick={Runner.Tick}"); // TEMP verify H-4
                        cAnimation.InteractAnimation();
                        throwableItem.PickupItem_RPC(this);
                        InteractUsedOnWorldThisTick = true;
                        return;
                    }
                }
            }

            // ของที่ใกล้ที่สุด (เดิมเลือกตัวแรกที่ overlap คืนมา) — ถ้าเพื่อนใกล้กว่า/กำลังแบกอยู่ ให้เป็ดจัดการแทน (IN-1)
            Interactable interactable = FindClosestInteractable(out float interactDist, out Collider2D interactHit);
            if (interactable == null || FriendInteractWins(interactDist)) return;

            VerifyLog.Log($"HandleInteraction INTERACT '{interactHit.name}' by {(isBird ? "Bird" : "Duck")} stateAuth={HasStateAuthority} inputAuth={HasInputAuthority} isForward={Runner.IsForward} tick={Runner.Tick}"); // TEMP verify H-4
            cAnimation.InteractAnimation();
            interactable.Interact(this);
            InteractUsedOnWorldThisTick = true;
        }
    }

    // ของที่กด E ได้และใกล้ที่สุด (วัดจากจุดกลางแบบเดียวกับป้ายกด)
    private Interactable FindClosestInteractable(out float distance, out Collider2D hitCollider)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius);

        Interactable closest = null;
        distance = float.MaxValue;
        hitCollider = null;

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (!hit.TryGetComponent<Interactable>(out var interactable) || !interactable.CanInteract(this)) continue;

            float dist = Vector2.Distance(transform.position, hit.transform.position);
            if (dist < distance)
            {
                distance = dist;
                closest = interactable;
                hitCollider = hit;
            }
        }
        return closest;
    }

    // กด E แล้วเพื่อนชนะของที่ใกล้สุดระยะ interactDist ไหม (เป็ด: กำลังแบก หรือนกใกล้กว่า) — host ใช้ตัดสิน, ป้ายกดใช้ซ่อน
    protected virtual bool FriendInteractWins(float interactDist) => false;

    #endregion

    #region Health & Damage

    public void TakeDamage(int dmg, float knockbackForce, Vector2 vec)
    {
        if (isDead || !InvincibleTimer.ExpiredOrNotRunning(Runner)) return;

        // ตอน pause/dialogue ตัวละครถูกหยุดอยู่ แต่ trigger/collision ของหนาม น้ำแข็ง ท่อนไม้ มอน ยังเรียกมาได้ (TP-7)
        if (GameplayPause.Frozen) return;
        RPC_TakeDamage(dmg, knockbackForce, vec);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(int dmg, float knockbackForce, Vector2 vec)
    {
        // เช็คบน host จุดเดียว ครอบทั้งกับดัก มอน และดาเมจจมน้ำ (TU-1)
        if (IsReadingTutorial) return;

        currentHealth -= dmg;
        if (HasInputAuthority && dmgSoundClip != null)
        {
            AudioManager.Instance?.PlayClipAtPosition(dmgSoundClip, transform.position);
        }
        // แรงกระเด็น: เจ้าของตัวเป็นคนใส่ให้ตัวเอง (NR-12)
        RaiseOwnerEvent(null, vec * knockbackForce, true);
        if (_damageFlash != null) _damageFlash.StartFlash();

        if (currentHealth <= 0)
        {
            isMoveAble = false;
            DeathMechanic(true);
        }
        else
        {
            InvincibleTimer = TickTimer.CreateFromSeconds(Runner, invincibleDuration);
        }
    }

    // host: ปิดหน้าสอนเล่นแล้วอมตะต่อสั้น ๆ — มอน/กับดักที่รออยู่ข้างตัวจะได้ไม่ตีทันทีที่ปิดหน้า (TU-1)
    private void TickTutorialGrace()
    {
        bool reading = IsReadingTutorial;
        if (_wasReadingTutorial && !reading) InvincibleTimer = TickTimer.CreateFromSeconds(Runner, invincibleDuration);
        _wasReadingTutorial = reading;
    }

    protected void StartDrowningDamage()
    {
        if (!HasStateAuthority || isDrowningDamageActive || isDead) return;
        isDrowningDamageActive = true;
        DrowningDamageTimer = TickTimer.CreateFromSeconds(Runner, drowningDamageInterval);
    }

    protected void StopDrowningDamage()
    {
        if (!HasStateAuthority || !isDrowningDamageActive) return;
        isDrowningDamageActive = false;
        DrowningDamageTimer = TickTimer.None;
    }

    protected void TickDrowningDamage()
    {
        if (!HasStateAuthority || !isDrowningDamageActive || isDead) return;
        if (DrowningDamageTimer.Expired(Runner))
        {
            RPC_TakeDamage(drowningDamagePerTick, 0f, Vector2.zero);
            if (!isDead)
                DrowningDamageTimer = TickTimer.CreateFromSeconds(Runner, drowningDamageInterval);
        }
    }

    protected void ApplyStun()
    {
        if (!HasStateAuthority || isStunned) return;
        isStunned = true;
        isMoveAble = false;
        IsInteractBusy = true;
        StunTimer = TickTimer.CreateFromSeconds(Runner, stunDuration);
    }

    private void TickStun()
    {
        if (!HasStateAuthority || !isStunned) return;
        if (StunTimer.Expired(Runner))
        {
            isStunned = false;
            isMoveAble = true;
            IsInteractBusy = false;
        }
    }

    public void OnStunStateChanged()
    {
        if (localGUI == null) return;
        if (isStunned) localGUI.ShowStunIcon();
        else localGUI.HideStunIcon();
    }

    #endregion

    #region Death & Respawn

    // host: เริ่มการตาย — เดิม DeathMechanic_RPC ยิงไปทุกเครื่องแล้วทุกเครื่องรัน CharacterDie เอง
    // (รวมถึง client ส่ง RPC_UpdateCarry ซ้ำ) ตอนนี้ host ตัดสินที่เดียว เครื่องอื่นตามจาก DeathCount
    public virtual void DeathMechanic(bool isPrimaryDeath)
    {
        if (!HasStateAuthority || isDead) return;

        IsPrimaryDeath = isPrimaryDeath;
        DeathCount++;
        CharacterDie(isPrimaryDeath);
        ApplyDeathLocal();
    }

    public void OnDeathCountChanged()
    {
        if (HasStateAuthority) return; // host ทำไปแล้วใน DeathMechanic
        ApplyDeathLocal();
    }

    // ภาพ เสียง UI และฟิสิกส์ของเครื่องตัวเองตอนตาย
    private void ApplyDeathLocal()
    {
        if (localGUI != null)
        {
            localGUI.StopOxygenTracking();
            localGUI.StopFlightBar();
        }

        if (!IsPrimaryDeath && lifeCycle != null)
        {
            lifeCycle.PlayOnFriendDeath();
        }

        if (dieSoundClip != null)
            AudioManager.Instance?.PlayClipAtPosition(dieSoundClip, transform.position);

        rb2D.linearVelocity = Vector2.zero;
        rb2D.simulated = false;

        if (IsClientOwner) CaptureOwnerBody();
    }

    // host เท่านั้น (เรียกจาก DeathMechanic)
    public virtual void CharacterDie(bool isPrimaryDeath)
    {
        if (isDead) return;
        isDead = true;

        if (IsBeingCarried)
        {
            if (Runner.TryFindObject(CarrierId, out var carrierObj) && carrierObj.TryGetComponent<Duck_Moveset>(out var duck))
            {
                duck.DropFriend(false);
            }
            if (IsBeingCarried) HostReleaseCarried(rb2D.position, Vector2.zero);
        }
        else if (isCarrying)
        {
            ((Duck_Moveset)this).DropFriend(true);
        }

        if (cAnimation != null)
        {
            if (isPrimaryDeath)
            {
                cAnimation.DeathAnimation();
            }
        }

        if (canbeRespawn)
        {
            respawnTimer = TickTimer.CreateFromSeconds(Runner, respawnCooldown);
        }

        if (isPrimaryDeath)
        {
            MovementCharacter[] allPlayers = FindObjectsByType<MovementCharacter>(FindObjectsSortMode.None);

            foreach (var partner in allPlayers)
            {
                if (!partner.enabled) continue;
                if (partner == this) continue;
                if (partner.isDead) continue;

                partner.DeathMechanic(false);
            }
        }
    }

    public virtual void Respawn()
    {
        if (HasStateAuthority)
        {
            isDead = false;
            currentHealth = characterMaxHealth;
            stilldrowning = false;
            IsHeadUnderwater = false;
            isWaterSurface = false;
            IsFalling = false;
            FallingBusy = false;
            isDrowningDamageActive = false;
            DrowningDamageTimer = TickTimer.None;

            isMoveAble = true;
            isOptional = false;
            isSpeedoptional = false;
            isClimbing = false;
            jumpedFromClimb = false;
            isJumping = false;

            if (cAnimation != null) cAnimation.ClearAnimationLock();

            if (GameManager.Instance != null) RaiseOwnerEvent(GameManager.Instance.GetRespawnPosition(), Vector2.zero, true);

            LastRespawnWasReset = false;
            RespawnCount++;
            ApplyRespawnLocal();
        }
    }

    public void OnRespawnCountChanged()
    {
        if (HasStateAuthority) return; // host ทำไปแล้วใน Respawn / DoResetToSpawn
        ApplyRespawnLocal();
    }

    private void ApplyRespawnLocal()
    {
        // รีเซ็ตจากเมนู (DoResetToSpawn) เล่นเอฟเฟกต์เพื่อนตายก่อนเหมือนเดิม
        if (LastRespawnWasReset && lifeCycle != null) lifeCycle.PlayOnFriendDeath();
        OnRespawned();
        SoloParty.Instance?.OnCharacterRespawned(this);
    }

    // ทุกเครื่องรันตอนเกิดใหม่ (host เรียกตรง, เครื่องอื่นจาก RespawnCount) — เดิมเป็น RPC_OnRespawned
    public virtual void OnRespawned()
    {
        if (IsOwner) ResetOwnerMovementState();
        isMoveAble = true;

        if (coll2D != null) coll2D.isTrigger = false;

        if (rb2D != null)
        {
            rb2D.simulated = true;
            rb2D.linearVelocity = Vector2.zero;
            rb2D.angularVelocity = 0f;

            if (IsOwner)
            {
                rb2D.bodyType = RigidbodyType2D.Dynamic;
                rb2D.gravityScale = normalGravity;
                rb2D.linearDamping = 0f;
            }
            else
            {
                MakeRemoteBody();
            }
        }

        if (this is Duck_Moveset duckSelf)
        {
            if (normalCollider != null) normalCollider.enabled = true;
            if (carryCollider != null) carryCollider.enabled = false;
            if (HasStateAuthority)
            {
                duckSelf.IsCarry = false;
                duckSelf.CarriedFriendId = default;
            }
        }

        _ownerWasCarried = false;
        if (HasStateAuthority)
        {
            IsBeingCarried = false;
            CarrierId = default;
            IsInteractBusy = false;
            isDrowningDamageActive = false;
            DrowningDamageTimer = TickTimer.None;
        }

        if (respawnSoundClip != null)
        {
            AudioManager.Instance?.PlayClipAtPosition(respawnSoundClip, transform.position);
        }


        if (visualTransform != null)
        {
            visualTransform.localPosition = Vector3.zero;
        }

        if (lifeCycle != null) lifeCycle.PlayOnRespawn();

        if (IsClientOwner) CaptureOwnerBody();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ResetPlayer()
    {
        if (!HasStateAuthority) return;
        DoResetToSpawn();
    }

    private void DoResetToSpawn()
    {
        // ── Reset state ──
        isDead = false;
        currentHealth = characterMaxHealth;
        stilldrowning = false;
        IsHeadUnderwater = false;
        isWaterSurface = false;
        IsFalling = false;
        FallingBusy = false;
        isDrowningDamageActive = false;
        DrowningDamageTimer = TickTimer.None;

        // ── Reset movement modifiers ──
        isMoveAble = true;
        isOptional = false;
        isSpeedoptional = false;
        isClimbing = false;
        isInClimbZone = false;
        jumpedFromClimb = false;
        isJumping = false;

        if (cAnimation != null) cAnimation.ClearAnimationLock();

        // ── cancel carry ──
        if (IsBeingCarried)
        {
            if (Runner.TryFindObject(CarrierId, out var carrierObj)
                && carrierObj.TryGetComponent<Duck_Moveset>(out var duck))
            {
                duck.DropFriend(false);
            }
            if (IsBeingCarried) HostReleaseCarried(rb2D.position, Vector2.zero);
        }
        else if (isCarrying)
        {
            ((Duck_Moveset)this).DropFriend(false);
        }

        if (GameManager.Instance != null) RaiseOwnerEvent(GameManager.Instance.GetRespawnPosition(), Vector2.zero, true);

        LastRespawnWasReset = true;
        RespawnCount++;
        ApplyRespawnLocal();
    }

    #endregion

    #region Solo
    // โหมด Solo: ตัวที่ไม่ได้คุมทำตัวเป็น "ตัวของอีกเครื่อง" ตามโค้ดเดิม (kinematic, วาดจาก Rep*)
    // SoloParty เขียน Rep* ให้แทน client และปิด rb2D.simulated — ไม่มี collider ไม่เหยียบปุ่ม ไม่โดนดาเมจ มอนมองไม่เห็น
    // GameMode.Single ปลด input authority ไม่ได้เลย (Remove, Assign None และ Assign ผู้เล่นอื่นไม่มีผล) จึงบังค่าที่นี่จุดเดียว
    public bool IsSoloFollower { get; private set; }
    public new bool HasInputAuthority => base.HasInputAuthority && !IsSoloFollower;

    // เป็ดแบกนกกลางอากาศ: ไม่มีท่าแบกตอนกระโดด จึงเล่นท่ากระโดดปกติและซ่อนนก
    public bool SoloHideCarryPose { get; set; }

    // รับช่วงต่อจาก from: ตำแหน่ง ความเร็ว สถานะกลางอากาศ เลือด เวลาอมตะ และปุ่มที่กดค้าง
    public void SoloBecomeLeader(MovementCharacter from)
    {
        Vector2 position = from.rb2D.position;
        Vector2 velocity = from.rb2D.linearVelocity;
        const MoveFlags carried = MoveFlags.Grounded | MoveFlags.InAir | MoveFlags.Jumping | MoveFlags.WaterSurface
            | MoveFlags.HeadUnderwater | MoveFlags.BodyOnWater | MoveFlags.StillDrowning | MoveFlags.FacingRight;
        MoveFlags flags = from._ownerFlags & carried;

        from.SoloBecomeFollower();

        IsSoloFollower = false;
        Object.AssignInputAuthority(Runner.LocalPlayer);
        Local = this;
        _ownerFlags = flags;
        _appliedEventSeq = EventSeq; // event ที่ค้างตอนเป็นตัวตามหลังไม่ต้องทำซ้ำ

        rb2D.bodyType = RigidbodyType2D.Dynamic;
        rb2D.simulated = true;
        rb2D.gravityScale = normalGravity;
        rb2D.linearDamping = 0f;
        rb2D.position = position;
        transform.position = position;
        rb2D.linearVelocity = velocity;
        if (coll2D != null) coll2D.isTrigger = false;
        SnapOwnerVisual(position);

        // ไม่ได้สิทธิ์ coyote/jump buffer จากตัวเดิม และปุ่มที่กดค้างไม่นับเป็นการกดใหม่
        PreviousButtons = from.PreviousButtons;
        _lastGroundedTick = NoTick;
        _jumpPressedTick = NoTick;
        isMoveAble = true;
        isOptional = false;
        isSpeedoptional = false;

        currentHealth = from.currentHealth;
        InvincibleTimer = from.InvincibleTimer;

        ForceUpdateUI();
        OnHeldItemChanged();
    }

    public void SoloBecomeFollower()
    {
        RepFlags = (uint)(_ownerFlags & MoveFlags.FacingRight);
        _ownerFlags = MoveFlags.None;
        IsSoloFollower = true;
        if (Local == this) Local = null;
        if (movementAudioSource != null && movementAudioSource.isPlaying) movementAudioSource.Stop();

        SoloMakeFollowerBody();
    }

    public void SoloMakeFollowerBody()
    {
        if (rb2D != null)
        {
            rb2D.linearVelocity = Vector2.zero;
            MakeRemoteBody();
            rb2D.simulated = false;
        }
        if (visualTransform != null) visualTransform.localPosition = Vector3.zero;
    }

    public void SoloSetFollowerState(Vector2 position, Vector2 velocity, MoveFlags flags, bool snap)
    {
        RepPosition = position;
        RepVelocity = velocity;
        RepFlags = (uint)flags;
        rb2D.position = position;
        transform.position = position;

        if (snap || !_hasOwnerVisual)
        {
            SnapOwnerVisual(position);
            return;
        }

        _ownerVisualPrev = _ownerVisualCurr;
        _ownerVisualCurr = position;
    }

    public void SoloSetHidden(bool hidden)
    {
        if (spriteRenderer != null) spriteRenderer.enabled = !hidden;
    }

    public void SoloSetCarriedBy(MovementCharacter carrier)
    {
        IsBeingCarried = carrier != null;
        CarrierId = carrier != null ? carrier.Object.Id : default;
    }
    #endregion

    #region CarrySystem
    // ยก/ปล่อยตัดสินที่ host ที่เดียว (เดิม RPC_UpdateCarry ยิงทุกเครื่องและ client ทำนายไปก่อน — ทำให้ตัวที่ถูกยกโยกบนจอ client)
    // ตัวที่ถูกยก: เจ้าของตัวติดตามผู้ยกเอง (OwnerSimulate) และวาร์ป/รับแรงโยนผ่าน event ตอนถูกปล่อย

    // host: เริ่มถูกยก (เรียกจาก Duck_Moveset.PickupFriend)
    public void HostSetCarried(NetworkId carrierId)
    {
        if (!HasStateAuthority) return;
        IsBeingCarried = true;
        CarrierId = carrierId;
        IsInteractBusy = true;
    }

    // host: ถูกปล่อย/โยน
    public void HostReleaseCarried(Vector2 dropPos, Vector2 impulse)
    {
        if (!HasStateAuthority) return;
        IsBeingCarried = false;
        CarrierId = default;
        IsInteractBusy = false;
        RaiseOwnerEvent(dropPos, impulse, true);
    }

    // เจ้าของ: จับจังหวะเพิ่งถูกยก/เพิ่งถูกปล่อยจากค่าที่ host ตั้ง
    private void UpdateOwnerCarriedState()
    {
        bool carried = IsBeingCarried;
        if (carried == _ownerWasCarried) return;
        _ownerWasCarried = carried;

        if (carried) OnOwnerCarried();
        else OnOwnerReleased();
    }

    protected virtual void OnOwnerCarried()
    {
        isJumping = false;
        IsGrounded = false;
    }

    // ตำแหน่งและแรงโยนมาจาก event (ทำไปแล้วก่อนหน้านี้ใน tick เดียวกัน) — ตรงนี้คืนสถานะตัวเอง
    protected virtual void OnOwnerReleased()
    {
        rb2D.gravityScale = normalGravity;
        if (coll2D != null) coll2D.isTrigger = false;
        IsGrounded = false;
        IsInAir = true;
        OnDroppedEvent();
    }

    public virtual void OnDroppedEvent()
    {
        isMoveAble = true;
    }

    #endregion

    #region OnChange

    public void OnHealthChanged()
    {
        if (HasInputAuthority && PlayerInterface.Instance != null)
        {
            PlayerInterface.Instance.UpdateHealthUI(currentHealth);
        }
    }

    public void OnCharacterTypeChanged()
    {
        if (HasInputAuthority && PlayerInterface.Instance != null)
        {
            PlayerInterface.Instance.UpdateProfileUI(isBird);
        }
    }

    public void OnHeldItemChanged()
    {
        string itemName = HeldItemName.ToString();

        if (HasInputAuthority && PlayerInterface.Instance != null
            && GameManager.Instance != null && GameManager.Instance.IsGameReady)
        {
            if (string.IsNullOrEmpty(itemName))
                PlayerInterface.Instance.HideItemOverlay();
            else
                PlayerInterface.Instance.ShowItemOverlay(itemName);
        }

        _canThrowItem = (itemName == "Rock");
    }

    #endregion

    #region CheckSystem

    private void CheckGround()
    {
        bool wasGrounded = IsGrounded;
        bool effectivelyCarried = IsBeingCarried;
        NetworkId effectiveCarrierId = CarrierId;

        float referenceVelocityY = rb2D.linearVelocity.y;
        bool isNearGround = false;

        if (effectivelyCarried && Runner.TryFindObject(effectiveCarrierId, out var duckObj) && duckObj.TryGetComponent<MovementCharacter>(out var duckMC) && IsBeingCarried)
        {
            IsGrounded = duckMC.IsGrounded;
            IsInAir = duckMC.IsInAir;
            isWaterSurface = duckMC.isWaterSurface;
            IsHeadUnderwater = duckMC.IsHeadUnderwater;
            IsBodyOnWater = duckMC.IsBodyOnWater;

            referenceVelocityY = duckMC.CurrentVelocity.y;

            if (isJumping)
            {
                IsGrounded = false;
                IsInAir = true;
            }
        }
        else
        {
            LayerMask mask = LayerMask.GetMask("Ground", "Platform");
            LayerMask waterMask = LayerMask.GetMask("Water");

            RaycastHit2D groundHit = GroundRay(rayDistance, mask);
            bool hitGround = groundHit.collider != null;
            isNearGround = GroundRay(rayDistance + nearGroundDistance, mask).collider != null;

            // จับว่ายืนอยู่บน moving platform ไหม (ไว้ follow แนวนอน)
            Transform plat = null;
            if (groundHit.collider != null && groundHit.collider.GetComponentInParent<IRideablePlatform>() is Component pc)
                plat = pc.transform;
            if (plat != currentPlatform)
            {
                currentPlatform = plat;
                if (currentPlatform != null) lastPlatformPos = currentPlatform.position; // กันกระโดดตอนเพิ่งเหยียบ
            }

            Vector2 headPosition = (Vector2)transform.position + (Vector2.up * headOffset);
            Vector2 bodyPosition = (Vector2)transform.position + (Vector2.up * bodyOffset);

            Collider2D bodyCollider = Physics2D.OverlapCircle(transform.position, 0.5f, waterMask);
            IsHeadUnderwater = Physics2D.OverlapPoint(headPosition, waterMask);
            IsBodyOnWater = Physics2D.OverlapPoint(bodyPosition, waterMask);

            IsGrounded = (!isJumping || rb2D.linearVelocity.y <= 0.05f) && hitGround && !IsHeadUnderwater;

            if (bodyCollider != null)
            {
                if (currentWater == null || currentWater.gameObject != bodyCollider.gameObject)
                {
                    currentWater = bodyCollider.GetComponentInParent<NetworkInteractableWater>() ?? bodyCollider.GetComponent<NetworkInteractableWater>();
                }
                if (IsBodyOnWater && !IsHeadUnderwater) { isWaterSurface = true; stilldrowning = false; }
                else if (IsHeadUnderwater) { isWaterSurface = false; stilldrowning = true; }
            }
            else
            {
                currentWater = null; isWaterSurface = false; stilldrowning = false;
            }

            IsInAir = !IsGrounded && !isWaterSurface;
        }

        if (!wasGrounded && IsGrounded)
        {
            isJumping = false;
            if (HasInputAuthority && landingSoundClip != null)
            {
                AudioManager.Instance?.PlayClipAtPosition(landingSoundClip, transform.position);
            }
        }

        if (IsGrounded)
        {
            isOptional = false;
            FallingBusy = false;
        }

        if (IsInAir && !isClimbing)
        {
            if (referenceVelocityY < -0.1f)
            {
                isJumping = false;
                if (!FallingBusy && !isOptional)
                {
                    if (!effectivelyCarried) FallingCheck();
                }
            }
        }
        else if (!effectivelyCarried && !isClimbing)
        {
            rb2D.gravityScale = isOptional ? optionalGravity : normalGravity;
        }
    }

    // ยิง ray ลงจากกลางตัวก่อน แล้วซ้าย/ขวาตามความกว้างเท้า — ยืนเกินขอบครึ่งตัวยังนับว่ายืน (MV-3)
    private RaycastHit2D GroundRay(float distance, int mask)
    {
        Vector2 origin = transform.position;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, distance, mask);
        if (hit.collider != null) return hit;

        float halfWidth = FeetHalfWidth() * groundCheckWidth;
        if (halfWidth <= 0f) return hit;

        hit = Physics2D.Raycast(origin + Vector2.left * halfWidth, Vector2.down, distance, mask);
        if (hit.collider != null) return hit;
        return Physics2D.Raycast(origin + Vector2.right * halfWidth, Vector2.down, distance, mask);
    }

    // ครึ่งความกว้างของ collider ตัวที่ใช้อยู่ (เป็ดตอนยกนกใช้ carryCollider)
    private float FeetHalfWidth()
    {
        Collider2D body = normalCollider != null && normalCollider.enabled ? normalCollider
            : carryCollider != null && carryCollider.enabled ? carryCollider
            : coll2D;
        return body != null && body.enabled ? body.bounds.extents.x : 0f;
    }

    private void FallingCheck()
    {
        float speedPercent = Mathf.Abs(rb2D.linearVelocity.y) / maxGravity;
        rb2D.gravityScale = Mathf.Lerp(normalGravity, heavyGravity, speedPercent);
        float cappedY = Mathf.Max(rb2D.linearVelocity.y, -maxGravity);
        rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, cappedY);
    }

    private void ManageMovementSounds()
    {
        if (!HasInputAuthority) return;
        if (movementAudioSource == null) return;

        if (AudioManager.Instance != null)
        {
            movementAudioSource.volume = AudioManager.Instance.GetGlobalSFXVolume();
        }

        AudioClip targetClip = null;

        bool isMovingOnGround = IsGrounded && Mathf.Abs(rb2D.linearVelocity.x) > 0.1f;
        bool isMovingInWater = (isWaterSurface || IsHeadUnderwater) && (Mathf.Abs(rb2D.linearVelocity.x) > 0.1f || Mathf.Abs(rb2D.linearVelocity.y) > 0.1f);

        if (isMovingOnGround && !isJumping)
        {
            targetClip = walkSoundClip;
        }
        else if (isMovingInWater)
        {
            targetClip = swimSoundClip;
        }

        if (targetClip != null)
        {
            if (movementAudioSource.clip != targetClip)
            {
                movementAudioSource.clip = targetClip;
                movementAudioSource.Play();
            }
            else if (!movementAudioSource.isPlaying)
            {
                movementAudioSource.Play();
            }
        }
        else
        {
            if (movementAudioSource.isPlaying)
            {
                movementAudioSource.Stop();
            }
        }
    }

    private void CheckInteractable()
    {
        Collider2D[] hitsItem = Physics2D.OverlapCircleAll(transform.position, interactRadius);

        Transform closestItem = null;
        float minItemDist = float.MaxValue;

        Transform closestInteract = null;
        float minInteractDist = float.MaxValue;

        isNearBreakableRock = false;

        foreach (var hit in hitsItem)
        {
            if (hit.gameObject == gameObject) continue;

            if (hit.TryGetComponent<Interactable>(out var interactable))
            {
                if (hit.GetComponent<BreakableRock>() != null) isNearBreakableRock = true;
                if (!interactable.CanInteract(this)) continue;

                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist < minInteractDist)
                {
                    minInteractDist = dist;
                    closestInteract = hit.transform;
                }
            }
            else if (hit.TryGetComponent<ThrowAbleItem>(out var throwableItem))
            {
                if (isBird && HeldItemName.ToString() == "" && !throwableItem.AlreadyThrow && throwableItem.IsStationary())
                {
                    float dist = Vector2.Distance(transform.position, hit.transform.position);
                    if (dist < minItemDist)
                    {
                        minItemDist = dist;
                        closestItem = hit.transform;
                    }
                }
            }
        }

        // กด E แล้วจะไปยก/โยนเพื่อนแทน -> ไม่ชี้ป้ายที่ของ (IN-1)
        if (closestInteract != null && FriendInteractWins(minInteractDist)) closestInteract = null;

        Transform target = (closestItem != null) ? closestItem : closestInteract;

        if (target != null && PlayerInterface.Instance != null)
            PlayerInterface.Instance.ShowInteract(target);
        else if (PlayerInterface.Instance != null)
            PlayerInterface.Instance.HideInteract();
    }

    // เจ้าของตัว: การเคลื่อนที่เฉพาะของเป็ด/นก
    protected virtual void OnOwnerSimulateSpecific(NetworkInputData input, bool hasInput) { }

    // host: กติกาเฉพาะของเป็ด/นก (ยก/ปล่อย, ปา, ดาเมจจมน้ำ)
    protected virtual void OnHostRulesSpecific(NetworkInputData input, bool hasInput, bool isMenuOpen) { }

    #endregion

    #region DropItem

    private void HandleDrop(NetworkInputData input)
    {
        bool isDropPressed = WasPressed(input, InputButton.Drop);

        if (isDropPressed && HeldItemName.ToString() != "")
        {
            if (this is Bird_Moveset bird && bird._prepareToThrow) return;

            if (Runner.IsForward)
            {
                Vector2 dropPosition = GetSafeDropPosition();
                GameManager.Instance.RPC_DropItemByName(HeldItemName.ToString(), dropPosition, this);
            }
        }
    }

    private Vector2 GetSafeDropPosition()
    {
        float dir = cAnimation.FlipX ? 1f : -1f;
        Vector2 origin = transform.position;
        Vector2 desired = origin + new Vector2(dir * dropFrontOffset, dropUpOffset);

        Vector2 toDesired = desired - origin;
        float dist = toDesired.magnitude;

        if (dist > 0.001f)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, toDesired.normalized, dist, dropBlockLayer);
            if (hit.collider != null)
                return hit.point - toDesired.normalized * 0.2f;
        }

        return desired;
    }

    #endregion

    #region SkillUnlock

    public virtual void SyncSkillUI() { }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_UnlockSkill(SkillType skill)
    {
        if (this is Duck_Moveset duck)
        {
            if (skill == SkillType.Duck_Dive) duck.isDiveUnlocked = true;
            if (skill == SkillType.Duck_Smash) duck.isSmashUnlocked = true;
        }
        else if (this is Bird_Moveset bird)
        {
            if (skill == SkillType.Bird_Fly) bird.isFlyUnlocked = true;
            if (skill == SkillType.Bird_Throw) bird.isThrowUnlocked = true;
        }
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue; Gizmos.DrawRay(transform.position, Vector2.down * rayDistance);
        Gizmos.color = Color.cyan; Gizmos.DrawRay((Vector2)transform.position + Vector2.right * 0.5f, Vector2.down * (rayDistance + nearGroundDistance));
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, interactRadius);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, playerInteractRadius);
        Gizmos.color = Color.magenta; Gizmos.DrawRay(transform.position, Vector2.up * headBumpCheckDistance);
    }
}
