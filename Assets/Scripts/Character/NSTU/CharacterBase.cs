using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterBase : MonoBehaviour, IDamageable, CharacterInteract
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

    [SerializeField] bool _isGetCarry;
    [SerializeField] GameObject _playerToCarry;
    [SerializeField] bool _isCarry;
    [SerializeField] bool _canCarry;

    [SerializeField] float _interactRadius;
    [SerializeField] Collider2D _interactColl;

    [SerializeField] bool isMoveAble;

    [Header("CharacterSet")]
    [SerializeField] bool _isDuck;
    [SerializeField] bool _isEagle;

    [SerializeField] Vector2 _duckPosition = new Vector2(-2f, 1);
    [SerializeField] Vector2 _eaglePosition = new Vector2(-1.5f, -1.5f);
    [SerializeField] Vector2 _positionToBe;

    [Header("Referent")]
    [SerializeField] Rigidbody2D rb2D;
    [SerializeField] Collider2D rb2Coll;

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
        s_weight = rb2D.mass;
        s_minStamina = s_maxStamina;
        if (rb2Coll == null)
        {
            rb2Coll = this.gameObject.GetComponent<Collider2D>();
            if (rb2Coll == null)
            {
                rb2Coll = this.gameObject.AddComponent<Collider2D>();
            }
        }
    }

    // Fix Carry Character

    private void Update()
    {
        if (isMoveAble)
        {
            CheckItemInteract();
            UpdateActionInput();

            if (_isGetCarry)
            {

            }
            else { UpdateMoveInput(); }
        }

        if (_isCarry)
        {
            if (_playerToCarry == null)
            {
                _isCarry = false;
                Debug.Log("can't find _playerToCarry");
                return;
            }

            UpdateCarryPos();
            _playerToCarry.transform.position = _positionToBe;
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
            if (m_ShiftAction.IsPressed())
            {
                Debug.Log("Run");
                s_speed = s_runSpeed;
            }
            else
            {
                s_speed = s_walkSpeed;
            }

            Vector2 movement = _moveXAmt * (s_speed - s_weight) * Time.deltaTime;
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

    public void InteractAble(Collider2D hit)
    {
        if (_isInteractAble)
        {
            if (_busy) return;

            if (m_interactAction.WasPressedThisFrame())
            {
                Debug.Log("press E");
                Interact(hit);

                if (!_isCarry)
                {
                    if (_canCarry && hit.gameObject.layer == LayerMask.NameToLayer("Player"))
                    {
                        if (_playerToCarry != null)
                        {
                            Debug.Log("Start Carry");

                            CarryCompanion(_playerToCarry);
                            _isCarry = true;
                        }
                    }
                }
                else
                {
                    _isCarry = false;
                }

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

    public void TakeDamage(int dmg, float knockbackForce, Vector2 vec)
    {
        if (_isDash)
        {
            Debug.Log("Dodge");
            return;
        }
        Vector2 direction = (vec - (Vector2)transform.position).normalized;
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
    #endregion

    #region Interact Zone

    public void CheckItemInteract()
    {
        Vector2 player = transform.position;
        int mask = LayerMask.GetMask("Player", "Interactable");
        Collider2D[] itemhitInteract = Physics2D.OverlapCircleAll(player, _interactRadius, mask);

        foreach (Collider2D hit in itemhitInteract)
        {
            if (hit == null || hit.gameObject == this.gameObject)
                continue;

            _isInteractAble = true;
            if (hit.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                CheckPlayerInteract(hit);
            }

            if (hit.gameObject.layer == LayerMask.NameToLayer("Interactable"))
            {
                Debug.Log("Hit an Item: " + hit.name);
            }

            InteractAble(hit);
        }
    }

    public void Interact(Collider2D hit)
    {
        Vector2 player = transform.position;

        if (hit != null)
        {
            _isInteractAble = true;
            Vector2 selfpos = new Vector2(transform.position.x, transform.position.y);
            switch (hit.gameObject)
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

    public void CheckPlayerInteract(Collider2D player)
    {
        switch (player.gameObject)
        {
            case GameObject g when g.TryGetComponent<CharacterInteract>(out var character):
                _playerToCarry = character.CharacterInteract();
                if (_isCarry)
                {
                    character.SetCollider(true);
                }else { character.SetCollider(false); }

                _canCarry = true;

                break;

            default:
                _canCarry = false;
                break;
        }
    }

    #endregion

    #region Skill

    public GameObject CharacterInteract()
    {
        return this.gameObject;
    }

    public void UpdateCarryPos()
    {
        Vector2 selfPos = new Vector2(transform.position.x, transform.position.y);
        if (s_minStamina > 0)
        {
            if (_isDuck)
            {
                _positionToBe = _duckPosition + selfPos;
                _isCarry = true;
            }

            if (_isEagle)
            {
                _positionToBe = _eaglePosition + selfPos;
                _isCarry = true;
            }
        }
        else { Debug.Log("out of stamina"); _isCarry = false; }
    }

    private void CarryCompanion(GameObject carryObject)
    {
        if (carryObject != null)
        {
            UpdateCarryPos();
        }
        else { Debug.Log("can't find carryObject"); };
    }

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

    public void DropCharacter()
    {
        throw new NotImplementedException(); // add to Drop
    }

    public void SetCollider(bool o)
    {
        rb2Coll.isTrigger = o;
    }
}