using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterBase : MonoBehaviour, IDamageable
{
    [Header("Input")]
    [SerializeField] InputActionAsset InputActionAsset;

    private InputAction m_moveAction;
    private InputAction m_ShiftAction;
    private InputAction m_jumpAction;
    private InputAction m_interactAction;
    private InputAction m_skillActiveAction;

    [SerializeField] Vector2 _moveXAmt;
    [SerializeField] bool _isInteractAble;
    [SerializeField] bool _isAbleSkill;

    [SerializeField] bool _busy;

    [Header("Stats")]
    [SerializeField] string s_name;
    [SerializeField] float s_weight;

    [SerializeField] bool _isDash;
    [SerializeField] bool _isJump;
    private bool _isGrounded;
    private bool _isInTheAir;

    [SerializeField] float _interactRadius;
    [SerializeField] Collider2D _interactColl;

    [SerializeField] Vector2 direction = Vector2.zero;
    [SerializeField] bool isMoveAble;

    [Header("CharacterSet")]
    [SerializeField] bool _isDuck;
    [SerializeField] bool _isEagle;

    [Header("Referent")]
    [SerializeField] Rigidbody2D rb2D;

    [Header("Network")]
    [SerializeField] int s_minHealth;
    [SerializeField] int s_maxHealth = 5;

    [SerializeField] float s_speed;
    [SerializeField] float s_walkSpeed;
    [SerializeField] float s_runSpeed;
    [SerializeField] float s_jumpForce;

    [SerializeField] float s_minStamina;
    [SerializeField] float s_maxStamina = 30;



    #region Public_value

    public int hp => s_minHealth;
    public float speed => s_speed;
    public float weight => s_weight;
    public float jumpForce => s_jumpForce;

    #endregion

    private void Awake()
    {
        s_minHealth = s_maxHealth;
        CheckCharacterAbilty();
        GetInput();
    }

    private void Update()
    {
        if (isMoveAble)
        {
            UpdateMoveInput();
            CheckItemInteract();
            UpdateActionInput();
        }
    }

    #region Input

    public void GetInput()
    {
        m_moveAction = InputSystem.actions.FindAction("Move");
        m_ShiftAction = InputSystem.actions.FindAction("Sprint");
        m_jumpAction = InputSystem.actions.FindAction("Jump");
        m_interactAction = InputSystem.actions.FindAction("Interact");
        m_skillActiveAction = InputSystem.actions.FindAction("SkillActive");


        if (rb2D == null)
        {
            rb2D = this.GetComponent<Rigidbody2D>();
            if (rb2D == null)
            {
                rb2D = this.AddComponent<Rigidbody2D>();
            }
        }

    }

    public void UpdateMoveInput()
    {
        _moveXAmt = m_moveAction.ReadValue<Vector2>();

        if (_moveXAmt != Vector2.zero)
        {
            if (m_ShiftAction.WasPerformedThisDynamicUpdate())
            {
                s_speed = s_runSpeed;
            }
            else
            {
                s_speed = s_walkSpeed;
            }

            Vector2 movement = _moveXAmt * s_speed * Time.deltaTime;
            rb2D.AddForce(movement, ForceMode2D.Force);
        }
    }

    public void UpdateActionInput()
    {
        if (_isJump)
        {
            if (m_jumpAction.WasPressedThisFrame())
            {
                _isJump = false;
                rb2D.AddForce(Vector2.up * jumpForce, ForceMode2D.Force);

                _isInTheAir = true;
            }
        }

        if (_isInteractAble)
        {
            if (_busy) return;

            if (m_interactAction.WasPressedThisFrame())
            {
                Debug.Log("press E");
                Interact();
            }
        }

        if (_isAbleSkill)
        {
            if (_busy) return;

            if (m_skillActiveAction.WasPressedThisFrame())
            {
                _busy = true;
                // do skill

                // return _busy = false; return _busy to false
            }
        }
    }

    #endregion

    #region AdjustValue

    public void TakeDamage(int dmg, float knockbackForce, Collision2D coll)
    {
        if (_isDash)
        {
            Debug.Log("Dodge");
            return;
        }

        Vector2 direction = (coll.transform.position - transform.position).normalized;
        Vector2 knockbackDir = direction * knockbackForce;

        rb2D.AddForce(knockbackDir, ForceMode2D.Force);

        s_minHealth -= dmg;
    }

    public void HealPlayer(int amount)
    {
        if (s_minHealth == s_maxHealth)
        {
            Debug.Log("Health is already full");
            return;
        }
        else if (s_minHealth < s_maxHealth)
        {
            s_minHealth += amount;
            if (s_minHealth > s_maxHealth)
            {
                s_minHealth = s_maxHealth;
            }
        }
    }
    #region Interact Zone
    public void Interact()
    {
        Vector2 player = transform.position;
        Collider2D hitInteractRadius = Physics2D.OverlapCircle(player, _interactRadius, LayerMask.GetMask("Interactable"));

        if (hitInteractRadius != null)
        {
            _isInteractAble = true;
            Vector2 selfpos = new Vector2(transform.position.x, transform.position.y);
            switch (hitInteractRadius.gameObject)
            {
                case GameObject g when g.TryGetComponent<Interactable>(out var obj):
                    Debug.Log("trigger interactable object");
                    obj.Interact();
                    break;

                case GameObject g when g.TryGetComponent<MoveableObject>(out var moveobj):
                    Debug.Log("trigger moveable object");
                    moveobj.MoveInteract(selfpos);
                    break;

                default:
                    Debug.Log("didn't found any trigger");
                    break;
            }
        }
        else
        {
            _isInteractAble = false;
        }
    }

    public void CheckItemInteract()
    {
        Vector2 player = transform.position;
        Collider2D hitInteractRadius = Physics2D.OverlapCircle(player, _interactRadius, LayerMask.GetMask("Interactable"));

        if (hitInteractRadius != null)
        {
            _isInteractAble = true;
        }
        else
        {
            _isInteractAble = false;
        }
    }

    #endregion


    private void CarryCompanion()
    {
        //if ()
        if (s_minStamina != 0)
        {

        }
    }

    #endregion


    #region Skill
    

    #endregion

    #region CharacterAction

    private void CheckCharacterAbilty()
    {
        if (_isDuck)
        {
            Debug.Log("Duck");
        }

        if (_isEagle)
        {
            Debug.Log("Eagle");
        }
    }

    #endregion


    private void OnCollisionEnter2D(Collision2D collision)
    {

        if (collision.collider.tag == "Ground" || collision.collider.tag == "Platform")
        {
            _isJump = true;
            _isInTheAir = false;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 playerPosition = this.gameObject.transform.position;
        Gizmos.DrawWireSphere(playerPosition, _interactRadius);
    }
}



[Serializable]
public class SkillSet
{
    [Header("Eagle")]
    public float _flyDuration;
    public float _flySpeed;


    [Header("Duck")]
    public bool isFloating;
    public float floatingSpeed;
    // floating animation

    public bool isDive;
    public float diveSpeed;
    // dive animation


    // Duck
    public void WaterFloating()
    {
        // Set speed
        int i = 1;
        if (i == 1) // if on top of water
        {
            float speed;
            float walkSpeed = floatingSpeed;
            // floating animation

            speed = walkSpeed;

            if (i == 2) // if press some button to dive
            {
                speed = diveSpeed;
                // dive animation
            }
        }
    }

    // Eagle

}