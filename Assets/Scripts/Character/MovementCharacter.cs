using Fusion;
using UnityEngine;
using Fusion.Addons.Physics;

public class MovementCharacter : NetworkBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] public CharacterStats stats;
    [SerializeField] public CharacterAnimation cAnimation;
    [SerializeField] public Rigidbody2D rb2D;
    [SerializeField] public Collider2D coll2D;
    [SerializeField] public PlayerGUI localGUI;
    [SerializeField] public SpriteRenderer spriteRenderer;

    //SOUND
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


    [Networked, OnChangedRender(nameof(OnCharacterTypeChanged))]
    public bool isBird { get; set; }

    [Header("Visual")]
    [SerializeField] public Transform visualTransform;
    private int originalSortingOrder;

    [Header("Movement Settings")]
    [Networked] public bool IsGrounded { get; set; }
    [Networked] public bool isWaterSurface { get; set; }
    [Networked] public bool IsInAir { get; set; }
    [Networked] public Vector2 MoveInput { get; set; }
    [Networked] public bool isFloating { get; set; }
    [SerializeField] public bool isMoveAble = true;

    [Networked] public bool resetAnimation { get; set; }
    [Networked] public bool isJumping { get; set; }

    [Networked] private TickTimer JumpCooldown { get; set; }
    [SerializeField] private float JumpCooldownTimer = 2f;

    [Header("Gravity Settings")]
    [SerializeField] public bool isSpeedoptional;
    [SerializeField] public float normalGravity = 3.5f;
    [SerializeField] public float heavyGravity = 6.5f;
    [SerializeField] public float maxGravity = 19f;

    private bool hasSetInitialPosition = false;

    // Falling
    [Networked] public bool IsFalling { get; set; }
    [Networked] public bool FallingBusy { get; set; }
    [SerializeField] public bool isOptional;
    [SerializeField] public float optionalGravity;
    [SerializeField] public float accelerationSpeedOptional = 0.6f;
    [SerializeField] public float decelerationSpeedOptional = 3f;
    [SerializeField] public float optionalMaxSpeed = 1f;

    [Header("Health & Respawn")]
    [Networked] public int characterMaxHealth { get; set; }
    [Networked, OnChangedRender(nameof(OnHealthChanged))] public int currentHealth { get; set; }
    [Networked] public bool isDead { get; set; }

    [Networked] TickTimer respawnTimer { get; set; }
    [SerializeField] float respawnCooldown = 3f;
    [SerializeField] bool canbeRespawn = true;

    [Header("Water Setting")]
    [Networked] public bool IsHeadUnderwater { get; set; }
    [Networked] public bool IsBodyOnWater { get; set; }
    [SerializeField] public float headOffset = 0.2f;
    [SerializeField] public float bodyOffset = -0.2f;
    [SerializeField] public float nearGroundDistance = 0.63f;
    [SerializeField] public NetworkInteractableWater currentWater;
    [Networked] public bool stilldrowning { get; set; }

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

    // Local Predict Variables
    public bool localIsBeingCarriedPredict;
    public NetworkId localCarrierIdPredict;
    [SerializeField] public bool _isEPressed;

    private bool _wasTabPressed;

    [Header("Interaction & Physics")]
    public float rayDistance = 1.2f;
    public float interactRadius = 1.5f;
    public float playerInteractRadius = 1f;

    [Header("I-Frames & Effects")]
    [Networked] private TickTimer InvincibleTimer { get; set; }
    [SerializeField] private float invincibleDuration = 1.5f;
    [SerializeField] private DamageFlash _damageFlash;
    [SerializeField] public Color duck_Color;
    [SerializeField] public Color bird_Color;

    [Header("Climbing")]
    [SerializeField] float climbSpeed = 2f;
    [SerializeField] LayerMask climbableMask;
    [Networked] public bool isInClimbZone { get; set; }
    [Networked] public bool isClimbing { get; set; }
    [Networked] public bool jumpedFromClimb { get; set; }

    // UnlockableSkills
    public enum SkillType { None, Duck_Dive, Duck_Smash, Bird_Fly, Bird_Throw }

    [Header("Inventory System (1 Slot)")]
    [Networked, OnChangedRender(nameof(OnHeldItemChanged))]
    public NetworkString<_32> HeldItemName { get; set; }
    private bool _wasDropPressed;

    [HideInInspector] public bool isNearBreakableRock;

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

    // Throw System
    [Networked] public bool _canThrowItem { get; set; }

    [Header("Etc")]
    [Networked] public bool _wasEscPressed { get; set; }

    [Header("LifeCycle Effect")]
    [SerializeField] public LifeCycle lifeCycle;

    private void Awake()
    {
        if (stats == null) stats = GetComponent<CharacterStats>();
        if (rb2D == null) rb2D = GetComponent<Rigidbody2D>();
        if (coll2D == null) coll2D = GetComponent<Collider2D>();
        if (localGUI == null) localGUI = GetComponent<PlayerGUI>();

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

        if (TryGetComponent<Fusion.Addons.Physics.NetworkRigidbody2D>(out var netRb))
        {
            netRb.InterpolationTarget = transform;
        }
    }

    #region CoreNetwork

    public override void Spawned()
    {
        bool isThisCharacterBird = (stats != null && stats.skinType == characterType.Bird);

        if (isThisCharacterBird && this is Duck_Moveset) { this.enabled = false; return; }
        if (!isThisCharacterBird && this.GetType().Name == "Bird_Moveset") { this.enabled = false; return; }

        if (cAnimation != null) cAnimation.InitializeMovement(this);

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

        JumpCooldown = TickTimer.CreateFromSeconds(Runner, JumpCooldownTimer);
        resetAnimation = true;

        if (GameManager.Instance != null) GameManager.Instance.RegisterPlayer(this);
        if (localGUI != null) localGUI.SetCharacterType(isThisCharacterBird);

        if (HasInputAuthority)
        {
            Invoke(nameof(ForceUpdateUI), 0.5f);
        }

        isMoveAble = true;
    }

    private void ForceUpdateUI()
    {
        OnHealthChanged();
        OnCharacterTypeChanged();
    }

    public override void FixedUpdateNetwork()
    {
        if (GameManager.Instance == null || GameManager.Instance.Object == null || !GameManager.Instance.Object.IsValid)
        {
            rb2D.linearVelocity = Vector2.zero;
            return;
        }
        if (!GameManager.Instance.isLoadMapDone)
        {
            hasSetInitialPosition = false;
            rb2D.linearVelocity = Vector2.zero;
            return;
        }

        if (!hasSetInitialPosition)
        {
            hasSetInitialPosition = true;

            if (HasStateAuthority)
            {
                GameManager.Instance.PlayerFinishedLoading();
            }
        }

        if (!GameManager.Instance.IsGameReady) { rb2D.linearVelocity = Vector2.zero; return; }

        if (isDead) { if (HasStateAuthority && canbeRespawn && respawnTimer.Expired(Runner)) Respawn(); return; }

        bool effectivelyCarried = IsBeingCarried || localIsBeingCarriedPredict;
        NetworkId effectiveCarrierId = IsBeingCarried ? CarrierId : localCarrierIdPredict;

        if (effectivelyCarried)
        {
            rb2D.gravityScale = 0f;
            rb2D.linearVelocity = Vector2.zero;
            if (coll2D != null && !coll2D.isTrigger) coll2D.isTrigger = true;
            isMoveAble = false;

            if (HasStateAuthority
                && Runner.TryFindObject(effectiveCarrierId, out var duckObj)
                && duckObj.TryGetComponent<Rigidbody2D>(out var duckRb))
            {
                Vector2 targetPos = duckRb.position + Vector2.up * betweenCarryPosition;

                if (TryGetComponent<NetworkRigidbody2D>(out var netRb))
                    netRb.Teleport(targetPos, transform.rotation);
                else
                    rb2D.position = targetPos;
            }
        }

        bool isMenuOpen = MenuController.Instance != null && MenuController.Instance.Object != null && MenuController.Instance.Object.IsValid && MenuController.Instance.IsMenuOpen;

        if (HasStateAuthority || HasInputAuthority) CheckGround();

        // Test

        bool wasInZone = isInClimbZone;
        isInClimbZone = CheckInClimbZone();

        // ----
        if (wasInZone && !isInClimbZone && isClimbing)
        {
            ExitClimbState();
        }

        if (GetInput(out NetworkInputData input))
        {
            if (isMenuOpen)
            {
                if (IsGrounded && !isJumping)
                {
                    rb2D.linearVelocity = new Vector2(0f, rb2D.linearVelocity.y);
                }
                cAnimation.UpdateAnimationController(Vector2.zero);

                HandleEtcInput(input);
            }
            else
            {
                if (isInClimbZone && !isClimbing && Mathf.Abs(input.vertical) > 0.1f)
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

                    HandleEtcInput(input);
                }
                else
                {
                    if (isMoveAble)
                    {
                        HandleMovement(input);
                        HandleJump(input);
                    }
                    if (!IsInteractBusy) HandleInteraction(input);
                    HandleEtcInput(input);
                    HandleDrop(input);
                }
            }
        }

        OnFixedUpdateSpecific();
    }

    #endregion

    #region CharacterSystem

    private void HandleEtcInput(NetworkInputData input)
    {
        bool isEscPressed = input.Keyboard_ESC && !_wasEscPressed;
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
        _wasEscPressed = input.Keyboard_ESC;

        bool isTabPressed = input.Keyboard_Tab && !_wasTabPressed;
        if (isTabPressed)
        {
            if (HasInputAuthority && PlayerInterface.Instance != null)
                PlayerInterface.Instance.HideNote();
        }
        _wasTabPressed = input.Keyboard_Tab;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestOpenMenuFromClient()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RequestOpenMenu();
        }
    }

    private void HandleMovement(NetworkInputData input)
    {
        float targetSpeed = input.horizontal * stats.maxSpeed;
        float currentSpeed = rb2D.linearVelocity.x;
        float accelRate = isSpeedoptional
            ? (Mathf.Abs(targetSpeed) > 0.01f ? accelerationSpeedOptional : decelerationSpeedOptional)
            : (Mathf.Abs(targetSpeed) > 0.01f ? stats.acceleration : stats.deceleration);

        float speedDif = targetSpeed - currentSpeed;
        rb2D.AddForce(Vector2.right * (speedDif * accelRate));
        cAnimation.UpdateAnimationController(new Vector2(input.horizontal, rb2D.linearVelocity.y));
    }

    protected virtual void HandleJump(NetworkInputData input)
    {
        if (jumpedFromClimb)
        {
            if (IsGrounded) jumpedFromClimb = false;
            else return;
        }

        if (input.KeybindJump && IsGrounded && JumpCooldown.ExpiredOrNotRunning(Runner))
        {
            isJumping = true;

            if (HasInputAuthority && jumpSoundClip != null)
            {
                AudioManager.Instance?.PlayClipAtPosition(jumpSoundClip, transform.position);
            }
            rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, 0f);
            rb2D.AddForce(Vector2.up * stats.s_jumpForce, ForceMode2D.Impulse);
            IsGrounded = false;
            resetAnimation = false;
            JumpCooldown = TickTimer.CreateFromSeconds(Runner, JumpCooldownTimer);
            if (cAnimation != null && !isCarrying) cAnimation.JumpAnimation();
        }
    }

    private void HandleInteraction(NetworkInputData input)
    {
        bool isEPressed = input.KeybindInteract && !_isEPressed;
        if (isEPressed)
        {
            Collider2D[] hitsItem = Physics2D.OverlapCircleAll(transform.position, interactRadius);
            foreach (var hit in hitsItem)
            {
                if (hit.gameObject == gameObject) continue;

                if (hit.TryGetComponent<Interactable>(out var interactable))
                {
                    if (!interactable.CanInteract(this)) continue;

                    cAnimation.InteractAnimation();
                    interactable.Interact(this);
                    Debug.Log($"Try to Interact with {hit.name}");
                    break;
                }
                else if (hit.TryGetComponent<ThrowAbleItem>(out var throwableItem))
                {
                    if (isBird)
                    {
                        if (throwableItem.AlreadyThrow || HeldItemName.ToString() != "") continue;

                        cAnimation.InteractAnimation();

                        throwableItem.PickupItem_RPC(this);
                        Debug.Log($"Try to pickup the item : {hit.name}");
                        break;
                    }
                }
            }
        }
        _isEPressed = input.KeybindInteract;
    }

    public void TakeDamage(int dmg, float knockbackForce, Vector2 vec)
    {
        if (isDead || !InvincibleTimer.ExpiredOrNotRunning(Runner)) return;
        RPC_TakeDamage(dmg, knockbackForce, vec);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(int dmg, float knockbackForce, Vector2 vec)
    {
        currentHealth -= dmg;
        if (HasInputAuthority && dmgSoundClip != null)
        {
            AudioManager.Instance?.PlayClipAtPosition(dmgSoundClip, transform.position);
        }
        rb2D.linearVelocity = Vector2.zero;
        rb2D.AddForce(vec * knockbackForce, ForceMode2D.Impulse);
        if (_damageFlash != null) _damageFlash.CallDamageFlash_RPC();

        if (currentHealth <= 0)
        {
            isMoveAble = false;
            DeathMechanic_RPC(true);
        }
        else
        {
            InvincibleTimer = TickTimer.CreateFromSeconds(Runner, invincibleDuration);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public virtual void DeathMechanic_RPC(bool isPrimaryDeath)
    {
        if (localGUI != null)
        {
            localGUI.StopOxygenTracking();
            localGUI.StopFlightBar();
        }

        if (!isPrimaryDeath && lifeCycle != null)
        {
            Debug.Log($"[LifeCycle] ▶ PlayOnFriendDeath on {gameObject.name}");
            lifeCycle.PlayOnFriendDeath();
        }

        CharacterDie(isPrimaryDeath);
    }

    public virtual void CharacterDie(bool isPrimaryDeath)
    {
        if (isDead) return;
        isDead = true;

        if (HasStateAuthority)
        {
            RPC_PlayDieSound();
        }

        if (IsBeingCarried)
        {
            if (Runner.TryFindObject(CarrierId, out var carrierObj) && carrierObj.TryGetComponent<Duck_Moveset>(out var duck))
            {
                duck.DropFriend(false);
            }
            RPC_UpdateCarry(false, default);
        }
        else if (isCarrying)
        {
            ((Duck_Moveset)this).DropFriend(true);
        }

        rb2D.linearVelocity = Vector2.zero;
        rb2D.simulated = false;

        if (cAnimation != null)
        {
            if (isPrimaryDeath)
            {
                cAnimation.DeathAnimation();
            }
        }

        if (HasStateAuthority && canbeRespawn)
        {
            respawnTimer = TickTimer.CreateFromSeconds(Runner, respawnCooldown);
        }

        if (HasStateAuthority && isPrimaryDeath)
        {
            MovementCharacter[] allPlayers = FindObjectsByType<MovementCharacter>(FindObjectsSortMode.None);

            foreach (var partner in allPlayers)
            {
                if (!partner.enabled) continue;
                if (partner == this) continue;
                if (partner.isDead) continue;

                partner.DeathMechanic_RPC(false);
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

            isMoveAble = true;
            isOptional = false;
            isSpeedoptional = false;
            isClimbing = false;
            jumpedFromClimb = false;
            isJumping = false;

            if (cAnimation != null) cAnimation.ClearAnimationLock();

            if (GameManager.Instance != null)
            {
                Vector3 newPos = GameManager.Instance.GetRespawnPosition();

                if (TryGetComponent<NetworkRigidbody2D>(out var netRb))
                {
                    netRb.Teleport(newPos, transform.rotation);
                }
                else
                {
                    transform.position = newPos;
                    if (rb2D != null) rb2D.position = newPos;
                }
            }

            RPC_OnRespawned();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public virtual void RPC_OnRespawned()
    {
        isMoveAble = true;

        if (rb2D != null)
        {
            rb2D.simulated = true;
            rb2D.bodyType = RigidbodyType2D.Dynamic;
            rb2D.gravityScale = normalGravity;
            rb2D.linearVelocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
            rb2D.linearDamping = 0f;
        }

        if (cAnimation != null) cAnimation.ReturnToBlendAnimation();

        if (respawnSoundClip != null)
        {
            AudioManager.Instance?.PlayClipAtPosition(respawnSoundClip, transform.position);
        }

        if (visualTransform != null)
        {
            visualTransform.localPosition = Vector3.zero;
        }

        if (lifeCycle != null) lifeCycle.PlayOnRespawn();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ForceClientTeleport(Vector3 newPos)
    {
        isMoveAble = true;
        isOptional = false;
        isSpeedoptional = false;

        transform.position = newPos;

        if (rb2D != null)
        {
            rb2D.simulated = true;
            rb2D.gravityScale = normalGravity;
            rb2D.position = newPos;
            rb2D.linearVelocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
        }

        if (TryGetComponent<NetworkRigidbody2D>(out var netRb))
        {
            netRb.Teleport(newPos, transform.rotation);
        }

        if (visualTransform != null)
        {
            visualTransform.position = newPos;
            visualTransform.localPosition = Vector3.zero;
        }
    }

    #region Climb System
    /*private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & climbableMask) == 0) return;

        IClimbable vine = other.GetComponent<IClimbable>()
                       ?? other.GetComponentInParent<IClimbable>();
        if (vine != null) isInClimbZone = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & climbableMask) == 0) return;

        IClimbable vine = other.GetComponent<IClimbable>()
                       ?? other.GetComponentInParent<IClimbable>();
        if (vine != null)
        {
            isInClimbZone = false;
            if (isClimbing) ExitClimbState();
        }
    }*/

    // Test

    private bool CheckInClimbZone()
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            transform.position,
            new Vector2(0.5f, 1f),  // ขนาดตัวละคร — ปรับให้พอดี
            0f,
            climbableMask
        );

        foreach (var hit in hits)
        {
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
        resetAnimation = false;
        JumpCooldown = TickTimer.CreateFromSeconds(Runner, JumpCooldownTimer);

        if (HasInputAuthority && jumpSoundClip != null)
            AudioManager.Instance?.PlayClipAtPosition(jumpSoundClip, transform.position);
        if (cAnimation != null && !isCarrying) cAnimation.JumpAnimation();
    }

    #endregion
    #endregion

    #region CarrySystem

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_UpdateCarry(bool state, NetworkId carrierId, bool doThrow = false, float throwDir = 1f, float forceX = 4f, float forceY = 4f, Vector2 throwSpawnPos = default)
    {
        if (HasStateAuthority)
        {
            IsBeingCarried = state;
            CarrierId = carrierId;
            IsInteractBusy = state;
        }
        localIsBeingCarriedPredict = state;
        localCarrierIdPredict = carrierId;

        if (Runner.TryFindObject(carrierId, out var duckObjForCollision)
            && duckObjForCollision.TryGetComponent<Collider2D>(out var duckColl))
        {
            Physics2D.IgnoreCollision(coll2D, duckColl, state);
        }
        if (!state)
        {
            if (HasStateAuthority)
            {
                Debug.Log($"[DROP] Bird rb2D.position ปัจจุบัน: {rb2D.position}");
                Debug.Log($"[DROP] throwSpawnPos ที่ได้รับ: {throwSpawnPos}");
                Debug.Log($"[DROP] Duck position: {(Runner.TryFindObject(carrierId, out var d) && d.TryGetComponent<Rigidbody2D>(out var dr) ? dr.position.ToString() : "NOT FOUND")}");

                Vector2 dropPos;
                if (throwSpawnPos != default)
                {
                    dropPos = throwSpawnPos;
                }
                else if (Runner.TryFindObject(carrierId, out var duckObjForPos)
                         && duckObjForPos.TryGetComponent<Rigidbody2D>(out var duckRb))
                {
                    dropPos = duckRb.position + Vector2.up * betweenCarryPosition;
                }
                else
                {
                    dropPos = rb2D.position;
                }

                rb2D.gravityScale = normalGravity;

                if (TryGetComponent<NetworkRigidbody2D>(out var netRb))
                {
                    netRb.Teleport(dropPos, transform.rotation);
                }
                else
                {
                    rb2D.position = dropPos;
                }

                rb2D.linearVelocity = Vector2.zero;

                if (doThrow)
                {
                    rb2D.AddForce(new Vector2(throwDir * forceX, forceY), ForceMode2D.Impulse);
                }

                IsGrounded = false;
                IsInAir = true;
                resetAnimation = false;
            }

            if (cAnimation != null)
                cAnimation.FallingAndFloatAnimation(true, false);
            if (visualTransform != null)
                visualTransform.localPosition = Vector3.zero;
            OnDroppedEvent();
        }
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

    #endregion

    #region CheckSystem

    private void CheckGround()
    {
        bool wasGrounded = IsGrounded;
        bool effectivelyCarried = IsBeingCarried || localIsBeingCarriedPredict;
        NetworkId effectiveCarrierId = IsBeingCarried ? CarrierId : localCarrierIdPredict;

        float referenceVelocityY = rb2D.linearVelocity.y;
        bool isNearGround = false;

        if (effectivelyCarried && Runner.TryFindObject(effectiveCarrierId, out var duckObj) && duckObj.TryGetComponent<MovementCharacter>(out var duckMC) && IsBeingCarried)
        {
            IsGrounded = duckMC.IsGrounded;
            IsInAir = duckMC.IsInAir;
            isWaterSurface = duckMC.isWaterSurface;
            IsHeadUnderwater = duckMC.IsHeadUnderwater;
            IsBodyOnWater = duckMC.IsBodyOnWater;

            referenceVelocityY = duckMC.rb2D.linearVelocity.y;

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

            bool hitGround = Physics2D.Raycast(transform.position, Vector2.down, rayDistance, mask);
            isNearGround = Physics2D.Raycast(transform.position, Vector2.down, rayDistance + nearGroundDistance, mask);

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
            resetAnimation = true;
            if (HasInputAuthority && landingSoundClip != null)
            {
                AudioManager.Instance?.PlayClipAtPosition(landingSoundClip, transform.position);
            }
        }

        if (IsGrounded)
        {
            isOptional = false;
            FallingBusy = false;
            if (resetAnimation)
            {
                if (cAnimation != null) cAnimation.ReturnToBlendAnimation();
                resetAnimation = false;
            }
        }

        if (IsInAir && !isClimbing)
        {
            if (referenceVelocityY < -0.1f)
            {
                isJumping = false;
                if (!FallingBusy && !isOptional)
                {
                    if (!effectivelyCarried) FallingCheck();
                    if (cAnimation != null) cAnimation.FallingAndFloatAnimation(true, isNearGround);
                }
            }
        }
        else if (!effectivelyCarried && !isClimbing)
        {
            rb2D.gravityScale = isOptional ? optionalGravity : normalGravity;
        }
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

        // 🟢 ดึง Volume จาก AudioManager ตลอดเวลา
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
        float minDistance = float.MaxValue;

        isNearBreakableRock = false;

        foreach (var hit in hitsItem)
        {
            if (hit.gameObject == gameObject) continue;

            if (hit.TryGetComponent<Interactable>(out var interactable))
            {
                if (hit.GetComponent<BreakableRock>() != null) isNearBreakableRock = true;

                if (!interactable.CanInteract(this)) continue;

                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestItem = hit.transform;
                }
            }
            else if (hit.TryGetComponent<ThrowAbleItem>(out var throwableItem))
            {
                if (isBird)
                {
                    if (throwableItem.AlreadyThrow || HeldItemName.ToString() != "") continue;

                    float dist = Vector2.Distance(transform.position, hit.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestItem = hit.transform;
                    }
                }
            }
        }

        if (closestItem != null && PlayerInterface.Instance != null)
            PlayerInterface.Instance.ShowInteract(closestItem);
        else if (PlayerInterface.Instance != null)
            PlayerInterface.Instance.HideInteract();
    }

    protected virtual void OnFixedUpdateSpecific() { }

    #endregion

    #region DropItem

    private void HandleDrop(NetworkInputData input)
    {
        bool isDropPressed = input.KeybindDropItem && !_wasDropPressed;

        if (isDropPressed && HeldItemName.ToString() != "")
        {
            if (this is Bird_Moveset bird && bird._prepareToThrow) return;

            float offsetDir = cAnimation.FlipX ? 0.6f : -0.6f;
            Vector2 dropPosition = (Vector2)transform.position + new Vector2(offsetDir, 0.5f);

            GameManager.Instance.RPC_DropItemByName(HeldItemName.ToString(), dropPosition, this);
        }

        _wasDropPressed = input.KeybindDropItem;
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

    public override void Render()
    {
        if (HasInputAuthority) CheckInteractable();

        bool effectivelyCarried = IsBeingCarried || localIsBeingCarriedPredict;
        NetworkId effectiveCarrierId = IsBeingCarried ? CarrierId : localCarrierIdPredict;

        if (effectivelyCarried && Runner.TryFindObject(effectiveCarrierId, out var duckObj) && duckObj.TryGetComponent<MovementCharacter>(out var duckMC))
        {
            if (spriteRenderer != null) spriteRenderer.sortingOrder = originalSortingOrder - 1;
            if (cAnimation != null) cAnimation.FlipX = duckMC.cAnimation.FlipX;
        }
        else
        {
            if (spriteRenderer != null) spriteRenderer.sortingOrder = originalSortingOrder;

            if (visualTransform != null) visualTransform.localPosition = Vector3.zero;
        }
        ManageMovementSounds();
    }

    private void LateUpdate()
    {
        if (Runner == null || !Runner.IsRunning) return;

        bool effectivelyCarried = localIsBeingCarriedPredict;
        NetworkId effectiveCarrierId = localCarrierIdPredict;

        if (effectivelyCarried
            && Runner.TryFindObject(effectiveCarrierId, out var duckObj)
            && duckObj.TryGetComponent<MovementCharacter>(out var duckMC))
        {
            transform.position = duckMC.transform.position + new Vector3(0, betweenCarryPosition, 0);

            if (visualTransform != null)
                visualTransform.localPosition = Vector3.zero;
        }
        else
        {
            if (visualTransform != null)
                visualTransform.localPosition = Vector3.zero;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_PlayRespawnSound()
    {
        if (respawnSoundClip != null)
        {
            AudioManager.Instance?.PlayClipAtPosition(respawnSoundClip, transform.position);
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayDieSound()
    {
        if (dieSoundClip != null)
            AudioManager.Instance?.PlayClipAtPosition(dieSoundClip, transform.position);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue; Gizmos.DrawRay(transform.position, Vector2.down * rayDistance);
        Gizmos.color = Color.cyan; Gizmos.DrawRay((Vector2)transform.position + Vector2.right * 0.5f, Vector2.down * (rayDistance + nearGroundDistance));
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, interactRadius);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, playerInteractRadius);
    }
}