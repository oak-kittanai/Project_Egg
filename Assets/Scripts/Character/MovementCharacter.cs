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
    float acceleration => stats.Acceleration;
    float deceleration => stats.Deceleration;
    float MaxSpeed => stats.MaxSpeed;
    float minStamina => stats.MinStamina;
    float maxStamina => stats.MaxStamina;

    bool IsEagle => stats.isEagle;
    bool IsDuck => stats.isDuck;

    [Header("Movement")]
    [SerializeField] Vector2 _moveX;
    [SerializeField] bool _moveAble;

    [SerializeField] bool _busy;
    [SerializeField] bool _staminaBusy;

    public bool isDash;
    [SerializeField] bool _jumpAble;
    [SerializeField] bool _isGrounded;
    [SerializeField] bool _isInTheAir;

    [Header("Data_Stats")]
    [SerializeField] bool _alreadyJump;
    [SerializeField] bool _isFalling;
    [SerializeField] bool _isFloat;

    [SerializeField] bool _isFly;

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
    }

    #region InputZone
    public void UpdateActionInput()
    {
        if (_isGrounded && input.JumpAction.WasPressedThisFrame())
        {
            Jump();
        }

        if (IsEagle && !_isGrounded && input.JumpAction.WasPerformedThisFrame() && minStamina > 0)
        {
            Fly();
        }
    }

    private void Jump()
    {
        _isGrounded = false;
        _alreadyJump = true;
        _isInTheAir = true;
        _jumpAble = false;

        rb2D.AddForce(Vector2.up * stats.jumpForce, ForceMode2D.Impulse);
        cAnimation.UpdateActionAnimation(1);
    }

    private void Fly()
    {
        _isFly = true;
        _isFalling = false;
        _alreadyJump = false;

        action.Flying(minStamina, _isGrounded, stats.FlySpeed, rb2D);
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
            _alreadyJump = false;
            cAnimation.OnGroundCheck();
            return;
        }

        /*if (_alreadyJump && !_isFly)
        {
            if (rb2D.linearVelocity.y < -0.2f)
            {
                _isFalling = true;
                rb2D.gravityScale = Mathf.Lerp(rb2D.gravityScale, 2f, Time.deltaTime * 2f);
            }
            else
            {
                rb2D.gravityScale = Mathf.Lerp(rb2D.gravityScale, 1f, Time.deltaTime * 2f);
            }
        }*/

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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.tag == "Ground" || collision.collider.tag == "Platform") // change to check reycast
        {
            _jumpAble = true;
            _isGrounded = true;
            _isInTheAir = false;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.tag == "Ground" || collision.collider.tag == "Platform")
        {
            _jumpAble = false;
            _isGrounded = false;
            _isInTheAir = true;
        }
    }
}
