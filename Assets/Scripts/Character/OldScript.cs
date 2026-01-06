/*using Fusion;
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

    // Universal Controls Mechanics
    [Networked] public Vector2 _moveX { get; set; }
    public float InputMoveX;
    public bool _moveAble;

    [SerializeField] public bool _busy;
    [SerializeField] public bool _staminaBusy;

    public bool isDash;
    [Networked] public bool _jumpAble { get; set; }
    [Networked] public bool _isGrounded { get; set; }
    [Networked] public bool _isWaterGround { get; set; }
    [Networked] public bool _isInTheAir { get; set; }
    [Networked] public bool _alreadyJump { get; set; }

    // Unique Mechanics
    [Networked] public bool _isFalling { get; set; }

    [Networked] public bool _isFloat { get; set; }
    [SerializeField] public bool _floatAble;

    [Header("Duck Setting")]


    [Header("Bird Setting")]

    [SerializeField] public bool flyAble;
    [Networked] public bool _isFly { get; set; }

    [Networked] public float fly_Acceleration { get; set; }
    [Networked] public float fly_Deceleration { get; set; }


    [Header("Ray")]
    [SerializeField] public float rayDistance;
    [SerializeField] RaycastHit2D hit2D;


    [SerializeField] bool DoFly;
    public characterType CurrentSkinType => stats.skinType;

    [Header("Position")]
    public Vector3 currentPosition;

    private void Awake()
    {
        Setup();
    }

    public override void Spawned()
    {

    }

    public void Setup()
    {
        coll2D = GetComponent<Collider2D>();
        rb2D = GetComponent<Rigidbody2D>();
        netRb2D = GetComponent<NetworkRigidbody2D>();

        stats = GetComponent<CharacterStats>();
        if (stats != null)
        {
            stats.Setup();
        }
        else Debug.LogError("can't find Stats");

        cAnimation = GetComponent<CharacterAnimation>();
        if (cAnimation != null)
        {
            cAnimation.Setup();
        }
        else Debug.LogError("can't find CharacterAnimation");

        action = GetComponent<CharacterAction>();
        if (action != null)
        {
            action.Setup();
        }
        else Debug.LogError("can't find CharacterAction");
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

        if (CurrentSkinType == characterType.Bird)
        {
            HandleFlying();
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

    private void Update()
    {
        UpdateStates();
        UpdatePosition();
    }

    public void UpdateMovement()
    {
        if (GetInput(out NetworkInputData input))
        {
            float moveX = input.horizontal;
            InputMoveX = moveX;

            if (MoveAble)
            {
                float targetSpeed = moveX * MaxSpeed;
                float speedDiff = targetSpeed - rb2D.linearVelocity.x;
                float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;

                float force = speedDiff * accelRate;
                rb2D.AddForce(Vector2.right * force, ForceMode2D.Force);
            }
        }
        if (HasInputAuthority)
        {
            UpdateActionInput(input.jump);
            action.InteractAble(input.Keyboard_E);
            if (CurrentSkinType == characterType.Bird)
            {
                action.MouseInput(input.mouse2, input.mouse1);
                action.UpdateCursorPos(input.mousePos);
            }
        }

        RayCast2DCheckGround();

        _moveX = this.transform.position;
    }

    #region InputZone
    public void UpdateActionInput(bool Jump)
    {
        if (CurrentSkinType == characterType.Duck)
        {
            if (_isGrounded && _jumpAble && Jump && !_isWaterGround)
            {
                JumpAction();
                if (_alreadyJump)
                {
                    StartCoroutine(WaitForJump());
                    StartCoroutine(WaitToFly());
                }
            }
        }

        if (CurrentSkinType == characterType.Bird)
        {
            if (_isGrounded && _jumpAble && Jump)
            {
                JumpAction();
                if (_alreadyJump)
                {
                    StartCoroutine(WaitForJump());
                    StartCoroutine(WaitToFly());
                }
            }

            if (_isInTheAir && Jump && flyAble && _alreadyJump)
            {
                Fly();
            }
        }

        /*if (_isInTheAir && !flyAble && _alreadyJump && Jump && _floatAble)
        {
            StartFloat();
        }

    }

    private void StartFloat()
    {
        _isFloat = true;
        cAnimation.FallingAndFloatAnimation(false);
    }

    private IEnumerator WaitForJump()
    {
        yield return new WaitForSeconds(1.5f);
        _jumpAble = true;
        _alreadyJump = false;
    }

    private void JumpAction()
    {
        _isGrounded = false;
        _isWaterGround = false;
        _alreadyJump = true;
        _isInTheAir = true;
        _jumpAble = false;

        cAnimation.JumpAnimation();
        StartCoroutine(JumpWait());
    }

    IEnumerator JumpWait()
    {
        yield return new WaitForSeconds(0.1f);
        rb2D.AddForce(Vector2.up * stats.s_jumpForce, ForceMode2D.Impulse);
    }

    public void UpdateStates()
    {
        if (_isGrounded)
        {
            rb2D.gravityScale = 1f;
            _isFly = false;
            _isFalling = false;
            _isFloat = false;
            cAnimation.OnGroundCheck();
            return;
        }

        if (_isFly)
        {
            rb2D.gravityScale = 1f;
        }

        if (_isFalling)
        {
            cAnimation.FallingAndFloatAnimation(true);
        }
    }

    public void UpdatePosition()
    {
        if (_alreadyJump && !_isFly && !_isFloat)
        {
            if (rb2D.linearVelocity.y < -0.8f)
            {
                rb2D.gravityScale = Mathf.Lerp(rb2D.gravityScale, 2f, Time.deltaTime * gravityTime);
                _isFalling = true;
            }
        }
    }

    #endregion

    #region UpdateAction

    #region Bird

    private void HandleFlying()
    {
        if (DoFly && stats.s_minStamina > 0 && !_isGrounded)
        {
            Flying();
        }
        else
        {
            StopFlying();
            cAnimation.FlyAnimation(false);
            _isFly = false;
            _staminaBusy = false;
        }
    }

    private void Fly()
    {
        Debug.Log("Trigger fly");

        _isFly = true;
        _isFalling = false;
        _staminaBusy = true;

        StartFly();
        Debug.Log("complete fly");
    }

    public void StartFly()
    {
        if (stats.s_minStamina > 0 && !_isGrounded)
        {
            cAnimation.FlyAnimation(true);
            DoFly = true;
            Debug.Log("Flyyy");
        }
        else
        {
            Debug.Log("not enough stamina");
        }
    }

    public void Flying()
    {
        float currentY = rb2D.linearVelocity.y;
        float targetY = stats.s_flySpeed;

        float speedDiff = targetY - currentY;
        float force = speedDiff * fly_Acceleration;

        rb2D.AddForce(Vector2.up * force, ForceMode2D.Force);
        stats.StaminaReduce(5f);
    }

    private void StopFlying()
    {
        if (rb2D.linearVelocity.y > 0.1f)
        {
            float speedDiff = 0 - rb2D.linearVelocity.y;
            float force = speedDiff * fly_Deceleration;
            rb2D.AddForce(Vector2.up * force, ForceMode2D.Force);
            DoFly = false;
        }
    }

    private IEnumerator WaitToFly()
    {
        yield return new WaitForSeconds(0.5f);
        flyAble = true;
    }

    #endregion

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
            Debug.Log(hit2D.collider.name);
            if (hit2D.collider.IsTouchingLayers(layerGround) || hit2D.collider.IsTouchingLayers(layerPlatform))
            {
                _isGrounded = true;
                _isInTheAir = false;
                flyAble = false;
                _isWaterGround = false;
                if (!_alreadyJump)
                {
                    _jumpAble = true;
                }
            }

            if (hit2D.collider.IsTouchingLayers(layerWater))
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
                    Debug.Log("rest in peace");
                }
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
*/