using Fusion;
using System.Collections;
using UnityEngine;

public class MovementCharacter : MonoBehaviour
{
    [Header("Referent")]
    CharacterStats stats;
    InputControl input;
    CharacterAnimation cAnimation;
    CharacterAction action;

    // Mono
    public Rigidbody2D rb2D;
    Collider2D coll2D;

    [Header("Set Value")]
    float acceleration => stats._acceleration;
    float deceleration => stats._deceleration;
    float MaxSpeed => stats._maxSpeed;
    float minStamina => stats.s_minStamina;
    float maxStamina => stats.s_maxStamina;

    bool IsBird => stats.isBird;
    bool IsDuck => stats.isDuck;

    [Header("Movement")]
    [Networked] public Vector2 _moveX {  get; set; }
    public bool _moveAble;

    [Networked] public bool _busy { get; set; }
    [Networked] public bool _staminaBusy { get; set; }

    public bool isDash;
    [Networked] bool _jumpAble { get; set; }
    [Networked] bool _isGrounded { get; set; }
    [Networked] public bool _isInTheAir { get; set; }

    [Header("Data_Stats")]
    [Networked] bool _alreadyJump { get; set; }
    [Networked] bool _isFalling { get; set; }
    [Networked] bool _isFloat { get; set; }

    [Networked] bool _isFly { get; set; }

    [Networked] float rayDistance { get; set; }
    [Networked] RaycastHit2D hit2D { get; set; }

    [Header("Position")]
    public Vector3 currentPosition;

    public void Setup()
    {
        coll2D = GetComponent<Collider2D>();
        rb2D = GetComponent<Rigidbody2D>();

        stats = GetComponent<CharacterStats>();
        if (stats != null)
        {
            stats.Setup();
        }
        else Debug.LogError("can't find Stats");

        input = GetComponent<InputControl>();
        if (input != null)
        {
            input.Setup();
        }
        else Debug.LogError("can't find Input");

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

    public void UpdateMovement()
    {
        _moveX = input.UpdateMoveInput();

        float targetSpeed = _moveX.x * MaxSpeed;
        float speedDiff = targetSpeed - rb2D.linearVelocity.x;
        float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;

        float force = speedDiff * accelRate;
        rb2D.AddForce(Vector2.right * force, ForceMode2D.Force);

        cAnimation.UpdateAnimation(new Vector2(_moveX.x, rb2D.linearVelocity.y));

        RayCast2DCheckGround();
    }

    #region InputZone
    public void UpdateActionInput()
    {
        if (_isGrounded && _jumpAble && input.JumpAction.WasPressedThisFrame())
        {
            Jump();
            if (_alreadyJump)
            {
                StartCoroutine(WaitForJump());
            }
        }

        if (IsBird && !_isGrounded && input.JumpAction.WasPerformedThisFrame() && minStamina > 0)
        {
            Fly();
        }
    }

    private IEnumerator WaitForJump()
    {
        Debug.Log("Wait for jump delay");
        yield return new WaitForSeconds(1.5f);
        _jumpAble = true;
        _alreadyJump = false;
    }

    private void Jump()
    {
        _isGrounded = false;
        _alreadyJump = true;
        _isInTheAir = true;
        _jumpAble = false;

        rb2D.AddForce(Vector2.up * stats.s_jumpForce, ForceMode2D.Impulse);
        cAnimation.UpdateActionAnimation(1);
    }

    private void Fly()
    {
        _isFly = true;
        _isFalling = false;

        action.Flying(minStamina, _isGrounded, stats.s_flySpeed, rb2D);
        cAnimation.UpdateActionAnimation(2);

        stats.StaminaReduce(10);
        _staminaBusy = true;
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

    #region UpdateAction



    #endregion

    #region RayCast

    public void RayCast2DCheckGround()
    {
        LayerMask layerGround = LayerMask.GetMask("Ground");
        LayerMask layerPlatform = LayerMask.GetMask("Platform");

        Vector2 playerPosition = transform.position;
        Vector2 checkGroundPosition = transform.up * rayDistance;

        hit2D = Physics2D.Raycast(playerPosition, checkGroundPosition);

        if (hit2D.collider != null)
        {
            if (hit2D.collider.IsTouchingLayers(layerGround) || hit2D.collider.IsTouchingLayers(layerPlatform))
            {
                _isGrounded = true;
                _isInTheAir = false;
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

            if (hit2D.collider.tag == "Player")
            {

            }
        }
    }

    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;

        Vector2 start = transform.position;
        Vector2 direction = -transform.up * rayDistance;

        Gizmos.DrawRay(start, direction);
    }
}
