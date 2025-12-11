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
    [Networked] public Vector2 _moveX {  get; set; }
    public float InputMoveX;
    public bool _moveAble;

    [SerializeField] public bool _busy;
    [SerializeField] public bool _staminaBusy;

    public bool isDash;
    [Networked] public bool _jumpAble { get; set; }
    [Networked] public bool _isGrounded { get; set; }
    [Networked] public bool _isInTheAir { get; set; }

    [Header("Data_Stats")]
    [Networked] public bool _alreadyJump { get; set; }
    [Networked] public bool _isFalling { get; set; }

    [Networked] public bool _isFloat { get; set; }

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
        if (HasInputAuthority)
        {

        }
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
        }

        if (CurrentSkinType == characterType.Bird)
        {
            HandleFlying();
        }
    }

    private void Update()
    {
        UpdateStates();
        UpdatePosition();
        cAnimation.UpdateAnimation(new Vector2(InputMoveX, rb2D.linearVelocity.y));
    }

    public void UpdateMovement()
    {
        if (GetInput(out NetworkdInputData input))
        {
            float moveX = input.horizontal;
            InputMoveX = moveX;

            float targetSpeed = moveX * MaxSpeed;
            float speedDiff = targetSpeed - rb2D.linearVelocity.x;
            float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;

            float force = speedDiff * accelRate;
            rb2D.AddForce(Vector2.right * force, ForceMode2D.Force);
        }
        if (HasInputAuthority)
        {
            UpdateActionInput(input.jump);
            action.InteractAble(input.Keyboard_E);
            action.MouseInput(input.mouse2, input.mouse1);
            action.UpdateCursorPos(input.mousePos);
        }
        
        RayCast2DCheckGround();

        _moveX = this.transform.position;
    }

    #region InputZone
    public void UpdateActionInput(bool Jump)
    {
        if (_isGrounded && _jumpAble && Jump)
        {
            JumpAction();
            if (_alreadyJump)
            {
                StartCoroutine(WaitForJump());
                StartCoroutine(WaitToFly()); // change to reach the top when jump then can fly
            }
        }

        if (CurrentSkinType == characterType.Bird && _isInTheAir && Jump && flyAble && _alreadyJump)
        {
            Fly();
        }
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
        _alreadyJump = true;
        _isInTheAir = true;
        _jumpAble = false;

        cAnimation.UpdateActionAnimation(1);
        if (CurrentSkinType == characterType.Bird)
        {
            StartCoroutine(JumpWait());
        }
        else
        {
            rb2D.AddForce(Vector2.up * stats.s_jumpForce, ForceMode2D.Impulse);
        }
    }

    IEnumerator JumpWait()
    {
        yield return new WaitForSeconds(0.55f);
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
            cAnimation.UpdateActionAnimation(3);
        }
    }

    public void UpdatePosition()
    {
        if (_alreadyJump && !_isFly)
        {
            if (rb2D.linearVelocity.y < -1f)
            {
                rb2D.gravityScale = Mathf.Lerp(rb2D.gravityScale, 2f, Time.deltaTime * 2f);
            }
        }
        else if (rb2D.linearVelocity.y < -1f && !_isFly && !_isFloat)
        {
            _isFalling = true;
            rb2D.gravityScale = Mathf.Lerp(rb2D.gravityScale, 2f, Time.deltaTime * 2f);
            cAnimation.UpdateActionAnimation(3);
        }
    }

    #endregion

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
            DoFly = false;
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
        cAnimation.UpdateActionAnimation(2);

        Debug.Log("complete fly");
    }

    public void StartFly()
    {
        if (stats.s_minStamina > 0 && !_isGrounded)
        {
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
        }
    }

    private IEnumerator WaitToFly()
    {
        yield return new WaitForSeconds(0.5f);
        flyAble = true;
    }

    #endregion

    #region UpdateAction



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
        if (hit2D.collider != null && HasStateAuthority)
        {
            if (hit2D.collider.IsTouchingLayers(layerGround) || hit2D.collider.IsTouchingLayers(layerPlatform))
            {
                _isGrounded = true;
                _isInTheAir = false;
                flyAble = false;
                if (!_alreadyJump)
                {
                    _jumpAble = true;
                }
            }
            else
            {
                _jumpAble = false;
                _isGrounded = false;
                _isInTheAir = true;
            }

            if (hit2D.collider.IsTouchingLayers(layerWater))
            {

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
