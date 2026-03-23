using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class MovementCharacter : NetworkBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] public CharacterStats stats;
    [SerializeField] public CharacterAnimation cAnimation;
    [SerializeField] public Rigidbody2D rb2D;
    [SerializeField] public Collider2D coll2D;
    [SerializeField] public PlayerGUI localGUI;
    [SerializeField] public SpriteRenderer spriteRenderer;
    [Networked] public bool isBird { get; set; }

    [Header("Movement Settings")]
    [Networked] public bool IsGrounded { get; set; }
    [Networked] public bool isWaterSurface { get; set; }
    [Networked] public bool IsInAir { get; set; }
    [Networked] public Vector2 MoveInput { get; set; }
    [Networked] public bool isFloating { get; set; }
    [SerializeField] public bool isMoveAble;

    [Networked] public bool resetAnimation { get; set; }
    [Networked] public bool isJumping { get; set; }

    [Networked] private TickTimer PreJumpTimer { get; set; }
    [Networked] public NetworkBool IsPreparingToJump { get; set; }
    [SerializeField] private float jumpDelay = 0.5f;

    [Networked] private TickTimer JumpCooldown { get; set; }
    [SerializeField] private float JumpCooldownTimer = 2f;

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

    [Header("Character Setting (Health & Respawn)")]
    [Networked] public int characterMaxHealth { get; set; }
    [Networked] public int characterMinHealth { get; set; }
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

    [SerializeField] public bool _isEPressed;

    [Header("Passenger System")]
    [Networked] public NetworkId CarrierId { get; set; }
    [Networked] public bool IsBeingCarried { get; set; }
    [Networked] public bool IsInteractBusy { get; set; }

    public float rayDistance = 1.2f;
    public float interactRadius = 1.5f;
    public float playerInteractRadius = 1f;

    [Header("I-Frames")]
    [Networked] private TickTimer InvincibleTimer { get; set; }
    [SerializeField] private float invincibleDuration = 1.5f;

    [SerializeField] private DamageFlash _damageFlash;
    // Test Damage
    [SerializeField] private bool FirstTimeTest = true;

    [Header("Material Setting")]
    [SerializeField] public Material outline;
    [SerializeField] public Color duck_Color;
    [SerializeField] public Color bird_Color;


    private void Awake()
    {
        if (stats == null) stats = GetComponent<CharacterStats>();
        if (cAnimation == null) cAnimation = GetComponent<CharacterAnimation>();
        if (rb2D == null) rb2D = GetComponent<Rigidbody2D>();
        if (localGUI == null) localGUI = GetComponent<PlayerGUI>();
        if (_damageFlash == null) _damageFlash = GetComponent<DamageFlash>();
        if (outline == null) outline = GetComponent<Material>();

        isMoveAble = true;
    }

    public override void Spawned()
    {
        if (stats.skinType == characterType.Bird)
        {
            isBird = true;
        }
        else { isBird = false; }

        if (isBird)
        {
            outline.SetColor("_OutlineColor", bird_Color);
        }
        else
        {
            outline.SetColor("_OutlineColor", duck_Color);
        }

        if (cAnimation != null)
        {
            cAnimation.UpdateSkin(stats.skinType);
        }

        if (stats != null)
        {
            characterMaxHealth = stats.s_maxHealth;
            characterMinHealth = 0;
        }

        currentHealth = characterMaxHealth;

        JumpCooldown = TickTimer.CreateFromSeconds(Runner, JumpCooldownTimer);
        resetAnimation = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayer(this);
        }

        if (localGUI != null)
        {
            localGUI.SetCharacterType(isBird);
        }

        if (HasInputAuthority || (HasStateAuthority && Runner.LocalPlayer == Object.StateAuthority))
        {
            OnHealthChanged();

            if (PlayerInterface.Instance != null)
            {
                PlayerInterface.Instance.UpdateProfileUI(isBird);
            }
        }
    }

    public void TakeDamage(int dmg, float knockbackForce, Vector2 vec)
    {

        if (isDead || !InvincibleTimer.ExpiredOrNotRunning(Runner)) return;

        currentHealth -= dmg;
        //cAnimation.SmashAnimation();

        rb2D.linearVelocity = Vector2.zero;
        rb2D.AddForce(vec * knockbackForce, ForceMode2D.Impulse);

        if (currentHealth <= characterMinHealth)
        {
            _damageFlash.CallDamageFlash_RPC();
            CharacterDie();
        }
        else
        {
            _damageFlash.CallDamageFlash_RPC();
            InvincibleTimer = TickTimer.CreateFromSeconds(Runner, invincibleDuration);
        }
    }

    public void OnHealthChanged()
    {
        if (HasInputAuthority || (HasStateAuthority && Runner.LocalPlayer == Object.StateAuthority))
        {
            if (PlayerInterface.Instance != null)
            {
                PlayerInterface.Instance.UpdateHealthUI(currentHealth);
            }
        }
    }

    public virtual void CharacterDie()
    {
        if (isDead) return;

        isDead = true;
        isMoveAble = false;

        if (IsBeingCarried) SetCarriedState(false, default);

        if (this is Duck_Moveset duck && duck.IsCarrying) duck.DropFriend(true);

        rb2D.linearVelocity = Vector2.zero;


        if (canbeRespawn)
        {
            respawnTimer = TickTimer.CreateFromSeconds(Runner, respawnCooldown);
        }
    }

    public virtual void Respawn()
    {
        isDead = false;
        isMoveAble = true;
        currentHealth = characterMaxHealth;

        stilldrowning = false;
        IsHeadUnderwater = false;
        isWaterSurface = false;
        IsFalling = false;
        FallingBusy = false;

        rb2D.linearVelocity = Vector2.zero;
        rb2D.angularVelocity = 0f;
        rb2D.gravityScale = normalGravity;

        if (GameManager.Instance != null)
        {
            transform.position = GameManager.Instance.GetRespawnPosition();
        }

        cAnimation.ReturnToBlendAnimation();
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
            rb2D.linearVelocity = Vector2.zero;
            return;
        }

        if (!hasSetInitialPosition)
        {
            transform.position = GameManager.Instance.GetRespawnPosition();
            hasSetInitialPosition = true;

            if (HasStateAuthority)
            {
                GameManager.Instance.PlayerFinishedLoading();
            }
        }
        
        if (!GameManager.Instance.IsGameReady)
        {
            rb2D.linearVelocity = Vector2.zero;
            return;
        }

        if (isDead)
        {
            if (HasStateAuthority && canbeRespawn && respawnTimer.Expired(Runner))
            {
                Respawn();
            }
            return;
        }

        if (IsBeingCarried)
        {
            rb2D.simulated = false;
        }
        else
        {
            rb2D.simulated = true;
        }

        CheckGround();

        if (GetInput(out NetworkInputData input))
        {
            if (!IsBeingCarried)
            {
                HandleMovement(input);
                HandleJump(input);
            }
            HandleInteraction(input);

            if (input.Keyboard_T)
            {
                if (FirstTimeTest)
                {
                    FirstTimeTest = false;
                    if (IsGrounded)
                    {
                        Vector2 knockbackDirection = new Vector2(1f, 1f).normalized;

                        TakeDamage(1, 1, knockbackDirection);
                    }
                }
                FirstTimeTest = true;
            }
        }

        InFrontCheck();

        OnFixedUpdateSpecific();
    }

    private void HandleMovement(NetworkInputData input)
    {
        if (isMoveAble)
        {
            float targetSpeed = input.horizontal * stats.maxSpeed;
            float currentSpeed = rb2D.linearVelocity.x;

            float accelRate = 0;

            if (isSpeedoptional)
            {
                if (Mathf.Abs(targetSpeed) > 0.01f)
                {
                    accelRate = accelerationSpeedOptional;
                }
                else
                {
                    accelRate = decelerationSpeedOptional;
                }
            }
            else
            {
                if (Mathf.Abs(targetSpeed) > 0.01f)
                {
                    accelRate = stats.acceleration;
                }
                else
                {
                    accelRate = stats.deceleration;
                }
            }

            float speedDif = targetSpeed - currentSpeed;

            rb2D.AddForce(Vector2.right * (speedDif * accelRate));
            cAnimation.UpdateAnimationController(new Vector2(input.horizontal, rb2D.linearVelocity.y));
        }
    }

    protected virtual void HandleJump(NetworkInputData input)
    {
        if (input.jump && IsGrounded && JumpCooldown.ExpiredOrNotRunning(Runner))
        {
            isJumping = true;
            IsInteractBusy = true;

            rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, 0f);
            rb2D.AddForce(Vector2.up * stats.s_jumpForce, ForceMode2D.Impulse);

            IsGrounded = false;
            IsInteractBusy = false;
            resetAnimation = false;

            JumpCooldown = TickTimer.CreateFromSeconds(Runner, JumpCooldownTimer);

            if (cAnimation != null) cAnimation.JumpAnimation();
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
                    return;
                }
            }
        }
        _isEPressed = input.Keyboard_E;
    }

    public void SetCarriedState(bool state, NetworkId carrierId)
    {
        IsBeingCarried = state;
        CarrierId = carrierId;
        IsInteractBusy = state;
        if (!state) isMoveAble = true;
    }

    public void SetResetAnimation(bool o) => resetAnimation = o;

    private void CheckGround()
    {
        LayerMask mask = LayerMask.GetMask("Ground", "Platform");

        bool wasGrounded = IsGrounded;

        bool hitGround = Physics2D.Raycast(transform.position, Vector2.down, rayDistance, mask);

        if (isJumping && rb2D.linearVelocity.y > 0.05f)
        {
            IsGrounded = false;
        }
        else
        {
            IsGrounded = hitGround;
        }

        if (!wasGrounded && IsGrounded)
        {
            isJumping = false;
            resetAnimation = true;
        }

        bool isNearGround = Physics2D.Raycast(transform.position, Vector2.down, rayDistance + nearGroundDistance, mask);

        LayerMask waterMask = LayerMask.GetMask("Water");
        Vector2 headPosition = (Vector2)transform.position + (Vector2.up * headOffset);
        Vector2 bodyPosition = (Vector2)transform.position + (Vector2.up * bodyOffset);

        Collider2D bodyCollider = Physics2D.OverlapCircle(transform.position, 0.5f, waterMask);
        IsHeadUnderwater = Physics2D.OverlapPoint(headPosition, waterMask);
        IsBodyOnWater = Physics2D.OverlapPoint(bodyPosition, waterMask);

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

        if (isBodyInWater && IsBodyOnWater && !IsHeadUnderwater)
        {
            isWaterSurface = true;
            stilldrowning = false;
        }
        else if (isBodyInWater && IsHeadUnderwater)
        {
            isWaterSurface = false;
            stilldrowning = true;
        }
        else if (!isBodyInWater)
        {
            isWaterSurface = false;
            stilldrowning = false;
        }

        IsInAir = !IsGrounded;

        if (IsGrounded)
        {
            isOptional = false;
            FallingBusy = false;

            if (!IsInteractBusy && resetAnimation)
            {
                cAnimation.ReturnToBlendAnimation();
                resetAnimation = false;
            }
        }

        if (isWaterSurface) IsInAir = false;

        if (IsInAir)
        {
            if (rb2D.linearVelocity.y < -0.1f)
            {
                isJumping = false;

                if (!FallingBusy && !isOptional)
                {
                    FallingCheck();
                    cAnimation.FallingAndFloatAnimation(true, isNearGround);
                }
            }
        }
        else
        {
            rb2D.gravityScale = isOptional ? optionalGravity : normalGravity;
        }
    }

    public override void Render()
    {
        if (HasInputAuthority)
        {
            CheckInteractablePrompt();
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
        {
            PlayerInterface.Instance.ShowInteractPrompt(closestItem);
        }
        else if (PlayerInterface.Instance != null)
        {
            PlayerInterface.Instance.HideInteractPrompt();
        }
    }

    private void FallingCheck()
    {
        float speedPercent = Mathf.Abs(rb2D.linearVelocity.y) / maxGravity;
        rb2D.gravityScale = Mathf.Lerp(normalGravity, heavyGravity, speedPercent);
        float cappedY = Mathf.Max(rb2D.linearVelocity.y, -maxGravity);
        rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, cappedY);
    }

    private void InFrontCheck() { }

    protected virtual void OnFixedUpdateSpecific() { }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, Vector2.down * rayDistance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay((Vector2)transform.position + Vector2.right * 0.5f, Vector2.down * (rayDistance + nearGroundDistance));
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerInteractRadius);
    }
}