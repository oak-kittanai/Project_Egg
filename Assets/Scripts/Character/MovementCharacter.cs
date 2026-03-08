using Fusion;
using Fusion.Addons.Physics;
using UnityEditor;
using UnityEngine;

public class MovementCharacter : NetworkBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] public CharacterStats stats;
    [SerializeField] public CharacterAnimation cAnimation;
    [SerializeField] public Rigidbody2D rb2D;
    [SerializeField] public Collider2D coll2D;

    [Networked] public bool isBird { get; set; }

    [Header("Movement Settings")]
    [Networked] public bool IsGrounded { get; set; }
    [Networked] public bool isWaterSurface { get; set; }
    [Networked] public bool IsInAir { get; set; }
    [Networked] public Vector2 MoveInput { get; set; }
    [Networked] public bool isFloating { get; set; }
    [SerializeField] public bool isMoveAble;

    [SerializeField] public bool resetAnimation;

    [SerializeField] bool isJumping;
    [Networked] private TickTimer JumpCooldown { get; set; }
    [SerializeField] private float JumpCooldownTimer = 2f;

    [SerializeField] public bool isSpeedoptional;
    [SerializeField] public float normalGravity = 3.5f;
    [SerializeField] public float heavyGravity = 6.5f;
    [SerializeField] public float maxGravity = 19f;

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
    [Networked] public int currentHealth { get; set; }

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

    private void Awake()
    {
        if (stats == null) stats = GetComponent<CharacterStats>();
        if (cAnimation == null) cAnimation = GetComponent<CharacterAnimation>();
        if (rb2D == null) rb2D = GetComponent<Rigidbody2D>();
        isMoveAble = true;
    }

    public override void Spawned()
    {
        isBird = stats.skinType == characterType.Bird;

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

            transform.position = GameManager.Instance.GetRespawnPosition();
        }
    }

    public void TakeDamage(int dmg, float knockbackForce, Vector2 vec)
    {

        if (isDead || !InvincibleTimer.ExpiredOrNotRunning(Runner)) return;

        currentHealth -= dmg;
        cAnimation.SmashAnimation();

        rb2D.linearVelocity = Vector2.zero;
        rb2D.AddForce(vec * knockbackForce, ForceMode2D.Impulse);

        if (currentHealth <= characterMinHealth)
        {
            Die();
        }
        else
        {
            InvincibleTimer = TickTimer.CreateFromSeconds(Runner, invincibleDuration);
        }
    }

    public virtual void Die()
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

        if (GameManager.Instance != null && GameManager.Instance.Object != null && GameManager.Instance.Object.IsValid)
        {
            if (!GameManager.Instance.IsGameReady)
            {
                rb2D.linearVelocity = Vector2.zero;
                return;
            }
        }
        else
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

        if (GetInput(out NetworkInputData input))
        {
            if (!IsBeingCarried)
            {
                HandleMovement(input);
                HandleJump(input);
            }
            HandleInteraction(input);
        }

        CheckGround();
        InFrontCheck();

        OnFixedUpdateSpecific();
    }

    private void HandleMovement(NetworkInputData input)
    {
        if (isMoveAble)
        {
            float targetSpeed = input.horizontal * stats.maxSpeed;
            float currentSpeed = rb2D.linearVelocity.x;
            float accelRate = isSpeedoptional ? (Mathf.Abs(targetSpeed) > 0.01f ? accelerationSpeedOptional : decelerationSpeedOptional) : (Mathf.Abs(targetSpeed) > 0.01f ? stats.acceleration : stats.deceleration);
            float speedDif = targetSpeed - currentSpeed;

            rb2D.AddForce(Vector2.right * (speedDif * accelRate));
            cAnimation.UpdateAnimationController(new Vector2(input.horizontal, rb2D.linearVelocity.y));
        }
    }

    protected virtual void HandleJump(NetworkInputData input)
    {
        if (input.jump && IsGrounded && JumpCooldown.Expired(Runner))
        {
            cAnimation.JumpAnimation();
            isJumping = true;
            IsInteractBusy = true;
            rb2D.AddForce(Vector2.up * stats.s_jumpForce, ForceMode2D.Impulse);
            IsGrounded = false;
            IsInteractBusy = false;
            resetAnimation = false;
            JumpCooldown = TickTimer.CreateFromSeconds(Runner, JumpCooldownTimer);
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
        IsGrounded = Physics2D.Raycast(transform.position, Vector2.down, rayDistance, mask);

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