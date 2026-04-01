using Fusion;
using UnityEngine;

public class MovementCharacter : NetworkBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] public CharacterStats stats;
    [SerializeField] public CharacterAnimation cAnimation;
    [SerializeField] public Rigidbody2D rb2D;
    [SerializeField] public Collider2D coll2D;
    [SerializeField] public PlayerGUI localGUI;
    [SerializeField] public SpriteRenderer spriteRenderer;

    [Networked, OnChangedRender(nameof(OnCharacterTypeChanged))]
    public bool isBird { get; set; }

    [Header("Visual Smoothing")]
    [SerializeField] public Transform visualTransform;
    [SerializeField] private float visualSmoothTime = 0.03f;
    private Vector3 visualVelocity = Vector3.zero;
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

    // Local Predict Variables
    public bool localIsBeingCarriedPredict;
    public NetworkId localCarrierIdPredict;
    [SerializeField] public bool _isEPressed;

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

    [Header("Etc")]
    [Networked] public bool _wasEscPressed { get; set; }

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

        if (cAnimation != null) cAnimation.UpdateSkin(stats.skinType);
        JumpCooldown = TickTimer.CreateFromSeconds(Runner, JumpCooldownTimer);
        resetAnimation = true;

        if (GameManager.Instance != null) GameManager.Instance.RegisterPlayer(this);
        if (localGUI != null) localGUI.SetCharacterType(isThisCharacterBird);
        if (visualTransform != null) visualTransform.SetParent(null);

        if (HasInputAuthority)
        {
            Invoke(nameof(ForceUpdateUI), 0.5f);
        }
    }

    private void ForceUpdateUI()
    {
        OnHealthChanged();
        OnCharacterTypeChanged();
    }

    public override void FixedUpdateNetwork()
    {
        if (GameManager.Instance == null || GameManager.Instance.Object == null || !GameManager.Instance.Object.IsValid || !GameManager.Instance.isLoadMapDone)
        {
            rb2D.linearVelocity = Vector2.zero;
            return;
        }

        if (!hasSetInitialPosition)
        {
            transform.position = GameManager.Instance.GetRespawnPosition();
            if (visualTransform != null) visualTransform.position = transform.position;
            hasSetInitialPosition = true;
            if (HasStateAuthority) GameManager.Instance.PlayerFinishedLoading();
        }

        if (!GameManager.Instance.IsGameReady) { rb2D.linearVelocity = Vector2.zero; return; }
        if (isDead) { if (HasStateAuthority && canbeRespawn && respawnTimer.Expired(Runner)) Respawn(); return; }

        bool effectivelyCarried = IsBeingCarried || localIsBeingCarriedPredict;
        NetworkId effectiveCarrierId = IsBeingCarried ? CarrierId : localCarrierIdPredict;

        if (effectivelyCarried)
        {
            if (rb2D.bodyType != RigidbodyType2D.Kinematic) rb2D.bodyType = RigidbodyType2D.Kinematic;
            rb2D.linearVelocity = Vector2.zero;

            if (coll2D != null && !coll2D.isTrigger) coll2D.isTrigger = true;

            isMoveAble = false;
        }
        else
        {
            if (rb2D.bodyType == RigidbodyType2D.Kinematic) rb2D.bodyType = RigidbodyType2D.Dynamic;

            if (coll2D != null && coll2D.isTrigger) coll2D.isTrigger = false;

            isMoveAble = true;
        }

        if (HasStateAuthority && effectivelyCarried)
        {
            if (Runner.TryFindObject(effectiveCarrierId, out var duckObj) && duckObj.TryGetComponent<Rigidbody2D>(out var duckRb))
            {
                rb2D.MovePosition(duckRb.position + Vector2.up * betweenCarryPosition);
            }
        }

        if (HasStateAuthority || HasInputAuthority) CheckGround();

        if (GetInput(out NetworkInputData input))
        {
            if (isMoveAble)
            {
                HandleMovement(input);
                HandleJump(input);
            }
            HandleInteraction(input);
            HandleEtcInput(input);
        }

        InFrontCheck();
        OnFixedUpdateSpecific();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (visualTransform != null) Destroy(visualTransform.gameObject);
    }

    #endregion

    #region CharacterSystem

    private void HandleEtcInput(NetworkInputData input)
    {
        bool isEscPressed = input.Keyboard_ESC && !_wasEscPressed;

        if (isEscPressed)
        {
            if (HasStateAuthority)
            {
                Menu_Interface.Instance.HostToggleMenu_RPC();
            }
            else if (HasInputAuthority)
            {
                Menu_Interface.Instance.ClientToggleLocalMenu();
            }
        }

        _wasEscPressed = input.Keyboard_ESC;
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
        if (input.jump && IsGrounded && JumpCooldown.ExpiredOrNotRunning(Runner))
        {
            isJumping = true;
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
        bool isEPressed = input.Keyboard_E && !_isEPressed;
        if (isEPressed)
        {
            Collider2D[] hitsItem = Physics2D.OverlapCircleAll(transform.position, interactRadius);
            foreach (var hit in hitsItem)
            {
                if (hit.gameObject == gameObject) continue;
                if (hit.TryGetComponent<Interactable>(out var interactable))
                {
                    cAnimation.InteractAnimation();
                    interactable.Interact();
                    break;
                }
            }
        }
        _isEPressed = input.Keyboard_E;
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
        rb2D.linearVelocity = Vector2.zero;
        rb2D.AddForce(vec * knockbackForce, ForceMode2D.Impulse);
        if (_damageFlash != null) _damageFlash.CallDamageFlash_RPC();

        if (currentHealth <= 0)
        {
            isMoveAble = false;
            DeathMechanic_RPC();
        }
        else
        {
            InvincibleTimer = TickTimer.CreateFromSeconds(Runner, invincibleDuration);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public virtual void DeathMechanic_RPC()
    {
        CharacterDie();
    }

    public virtual void CharacterDie()
    {
        if (isDead) return;
        isDead = true;

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

        if (HasStateAuthority && canbeRespawn)
        {
            respawnTimer = TickTimer.CreateFromSeconds(Runner, respawnCooldown);
        }

        if (HasStateAuthority)
        {
            MovementCharacter[] allPlayers = FindObjectsByType<MovementCharacter>(FindObjectsSortMode.None);

            foreach (var partner in allPlayers)
            {
                if (partner != this && !partner.isDead)
                {
                    partner.DeathMechanic_RPC();
                }
            }
        }
    }

    public virtual void Respawn()
    {
        isDead = false;
        isMoveAble = true;
        if (HasStateAuthority) currentHealth = characterMaxHealth;

        stilldrowning = false;
        IsHeadUnderwater = false;
        isWaterSurface = false;
        IsFalling = false;
        FallingBusy = false;

        rb2D.simulated = true;
        rb2D.linearVelocity = Vector2.zero;
        rb2D.angularVelocity = 0f;
        rb2D.gravityScale = normalGravity;

        if (GameManager.Instance != null)
        {
            transform.position = GameManager.Instance.GetRespawnPosition();
            if (visualTransform != null) visualTransform.position = transform.position;
        }

        if (cAnimation != null) cAnimation.ReturnToBlendAnimation();
    }

    #endregion

    #region CarrySystem

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_UpdateCarry(bool state, NetworkId carrierId, bool doThrow = false, float throwDir = 1f, float forceX = 4f, float forceY = 4f)
    {
        if (HasStateAuthority)
        {
            IsBeingCarried = state;
            CarrierId = carrierId;
            IsInteractBusy = state;
        }

        localIsBeingCarriedPredict = state;
        localCarrierIdPredict = carrierId;

        if (Runner.TryFindObject(carrierId, out var duckObj) && duckObj.TryGetComponent<Collider2D>(out var duckColl))
        {
            Physics2D.IgnoreCollision(coll2D, duckColl, state);
        }

        if (!state)
        {
            if (HasStateAuthority)
            {
                if (duckObj != null)
                {
                    rb2D.position = duckObj.transform.position + new Vector3(0, betweenCarryPosition, 0);
                }

                rb2D.bodyType = RigidbodyType2D.Dynamic;
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
            {
                cAnimation.FallingAndFloatAnimation(true, false);
            }

            OnDroppedEvent();
        }
    }

    public virtual void OnDroppedEvent()
    {

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

        if (IsInAir)
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
        else if (!effectivelyCarried)
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

    public override void Render()
    {
        if (HasInputAuthority) CheckInteractablePrompt();

        bool effectivelyCarried = IsBeingCarried || localIsBeingCarriedPredict;
        NetworkId effectiveCarrierId = IsBeingCarried ? CarrierId : localCarrierIdPredict;

        if (effectivelyCarried && Runner.TryFindObject(effectiveCarrierId, out var duckObj) && duckObj.TryGetComponent<MovementCharacter>(out var duckMC))
        {
            if (spriteRenderer != null) spriteRenderer.sortingOrder = originalSortingOrder - 1;
            if (visualTransform != null && duckMC.visualTransform != null)
            {
                visualTransform.position = duckMC.visualTransform.position + new Vector3(0, betweenCarryPosition, 0);
                visualVelocity = Vector3.zero;
            }
            if (cAnimation != null) cAnimation.FlipX = duckMC.cAnimation.FlipX;
        }
        else
        {
            if (spriteRenderer != null) spriteRenderer.sortingOrder = originalSortingOrder;
            if (visualTransform != null)
            {
                visualTransform.position = Vector3.SmoothDamp(visualTransform.position, transform.position, ref visualVelocity, visualSmoothTime);
            }
        }
    }

    private void CheckInteractablePrompt()
    {
        Collider2D[] hitsItem = Physics2D.OverlapCircleAll(transform.position, interactRadius);
        Transform closestItem = null;
        float minDistance = float.MaxValue;

        foreach (var hit in hitsItem)
        {
            if (hit.gameObject == gameObject) continue;
            if (hit.TryGetComponent<Interactable>(out var interactable))
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestItem = hit.transform;
                }
            }
        }

        if (closestItem != null && PlayerInterface.Instance != null)
            PlayerInterface.Instance.ShowInteractPrompt(closestItem);
        else if (PlayerInterface.Instance != null)
            PlayerInterface.Instance.HideInteractPrompt();
    }

    private void InFrontCheck() { }
    protected virtual void OnFixedUpdateSpecific() { }

    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue; Gizmos.DrawRay(transform.position, Vector2.down * rayDistance);
        Gizmos.color = Color.cyan; Gizmos.DrawRay((Vector2)transform.position + Vector2.right * 0.5f, Vector2.down * (rayDistance + nearGroundDistance));
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, interactRadius);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, playerInteractRadius);
    }
}