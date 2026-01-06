using Fusion;
using Fusion.Addons.Physics;
using System.Collections;
using UnityEngine;

public class MovementCharacter : NetworkBehaviour
{
    [Header("Referent")]
    CharacterStats stats;
    CharacterAnimation cAnimation;
    CharacterAction action;

    // Mono
    [SerializeField] Rigidbody2D rb2D;
    [SerializeField] NetworkRigidbody2D netRb2D;
    Collider2D coll2D;

    [Header("Set Value")]
    float acceleration => stats.acceleration;
    float deceleration => stats.deceleration;
    float MaxSpeed => stats.maxSpeed;
    float minStamina => stats.s_minStamina;
    float maxStamina => stats.s_maxStamina;

    [Header("Movement")]

    public bool MoveAble; // make sure you can move this character

    [SerializeField] public float gravityTime = 1f; // gravity
    [SerializeField] public Vector2 _moveX; // show where player at

    // Universal Controls Mechanics
    public float InputMoveX;
    public bool _moveAble;
    
    [SerializeField] public bool _busy;
    [SerializeField] public bool _staminaBusy;

    public bool isDash;
    [Networked] public bool _jumpAble { get; set; }
    [Networked] public bool _isGrounded { get; set; }
    [Networked] public bool _isWaterGround {  get; set; }
    [Networked] public bool _isInTheAir { get; set; }
    [Networked] public bool _alreadyJump { get; set; }
    [Networked] public bool _isFalling { get; set; }

    [Networked] private TickTimer jumpCooldown { get; set; }
    [Networked] private TickTimer jumpDelayTimer { get; set; }

    [Header("Ray")] // aka check ground
    [SerializeField] public float rayDistance;
    [SerializeField] RaycastHit2D hit2D;

    public characterType CurrentSkinType => stats.skinType;

    [Header("Position")]
    public Vector3 currentPosition;

    private void Awake()
    {
        Setup();
    }

    public override void Spawned()
    {
        _jumpAble = true;
    }

    public void Setup()
    {
        coll2D = GetComponent<Collider2D>();
        rb2D = GetComponent<Rigidbody2D>();
        netRb2D = GetComponent<NetworkRigidbody2D>();

        stats = GetComponent<CharacterStats>();
        cAnimation = GetComponent<CharacterAnimation>();
        action = GetComponent<CharacterAction>();

        if (stats != null) stats.Setup();
        if (cAnimation != null) cAnimation.Setup();
        if (action != null) action.Setup();
    }

    public override void FixedUpdateNetwork()
    {
        if (_moveAble)
        {
            UpdateMovement();

            if (stats.s_minStamina < stats.s_maxStamina && !_staminaBusy)
            {
                stats.RechargeStamina(true);
            }

            if (action.carryRock)
            {
                MoveAble = false;
            }
            else MoveAble = true;
        }

        if (CurrentSkinType == characterType.Duck)
        {
            cAnimation.UpdateAnimationOnDuck(new Vector2(InputMoveX, rb2D.linearVelocity.y), _isWaterGround);
        }
        else
        {
            cAnimation.UpdateAnimationOnBird(new Vector2(InputMoveX, rb2D.linearVelocity.y));
        }
    }

    public void UpdateMovement()
    {
        if (GetInput(out NetworkInputData input))
        {
            InputMoveX = input.horizontal;

            if (MoveAble)
            {
                float targetSpeed = InputMoveX * MaxSpeed;
                float speedDiff = targetSpeed - rb2D.linearVelocity.x;
                float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;

                float force = speedDiff * accelRate;
                rb2D.AddForce(Vector2.right * force, ForceMode2D.Force);

                HandleJump(input.jump);

                if (HasInputAuthority) action.InteractAble(input.Keyboard_E);
            }
        }
        RayCast2DCheckGround();
        HandleGravityAndFalling();

        _moveX = this.transform.position;
    }

    #region InputZone

    private void HandleJump(bool jumpPressed)
    {
        if (jumpDelayTimer.Expired(Runner))
        {
            rb2D.AddForce(Vector2.up * stats.s_jumpForce, ForceMode2D.Impulse);
            jumpDelayTimer = TickTimer.None;
        }

        if (_isGrounded && _jumpAble && jumpPressed && !_isWaterGround)
        {
            _isGrounded = false;
            _isWaterGround = false;
            _alreadyJump = true;
            _isInTheAir = true;
            _jumpAble = false;

            cAnimation.JumpAnimation();

            jumpDelayTimer = TickTimer.CreateFromSeconds(Runner, 0.1f);

            jumpCooldown = TickTimer.CreateFromSeconds(Runner, 1.5f);
        }

        if (jumpCooldown.Expired(Runner) && !_jumpAble)
        {
            _jumpAble = true;
            _alreadyJump = false;
            jumpCooldown = TickTimer.None;
        }
    }

    private void HandleGravityAndFalling()
    {
        if (_alreadyJump)
        {
            if (rb2D.linearVelocity.y < 0.8f)
            {
                rb2D.gravityScale = Mathf.Lerp(rb2D.gravityScale, 2f, Runner.DeltaTime * gravityTime);
                _isFalling = true;
            }
        }

        if (_isGrounded)
        {
            rb2D.gravityScale = 1f;
            _isFalling = false;
        }
    }
    #endregion

    #region RayCast

    public void RayCast2DCheckGround()
    {
        LayerMask layerGround = LayerMask.GetMask("Ground");
        LayerMask layerPlatform = LayerMask.GetMask("Platform");
        LayerMask layerWater = LayerMask.GetMask("Water");

        Vector2 playerPosition = transform.position;
        Vector2 checkGroundPosition = Vector2.down * rayDistance;

        hit2D = Physics2D.Raycast(playerPosition, checkGroundPosition);
        if (hit2D.collider != null)
        {
            int hitLayer = hit2D.collider.gameObject.layer;

            bool isGround = LayerMask.GetMask("Ground") == (LayerMask.GetMask("Ground") | (1 << hitLayer));
            bool isPlatform = LayerMask.GetMask ("Platform") == (LayerMask.GetMask("Platform") | (1 << hitLayer));
            bool isWater = LayerMask.GetMask("Water") == (LayerMask.GetMask("Water") | (1 << hitLayer));

            if (isGround && isPlatform)
            {
                _isGrounded = true;
                _isInTheAir = false;
                _isWaterGround = false;

                if (jumpCooldown.ExpiredOrNotRunning(Runner))
                {
                    _jumpAble = true;
                }
                else if (isWater)
                {
                    if (CurrentSkinType == characterType.Duck)
                    {
                        _isWaterGround = true;
                        _isGrounded = false;
                        _isInTheAir = false;
                        _jumpAble = false;
                    }
                    else
                    {
                        Debug.Log("Rest in peace bird");
                    }
                }
            }
            else
            {
                _isGrounded = false;
                _isInTheAir = true;
            }
        }
    }

    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Vector2 start = transform.position;
        Vector2 direction = Vector2.down * rayDistance;
        Gizmos.DrawRay(start, direction);
    }
}
