using System;
using UnityEngine;

public class CharacterManager : MonoBehaviour, CharacterInteract, IDamageable
{
    [Header("Referent")]
    CharacterStats stats;
    InputControl input;
    CharacterAction action;

    // Mono
    Rigidbody2D rb2D;
    Collider2D coll2D;

    [Header("Movement")]
    [SerializeField] Vector2 moveX;
    [SerializeField] bool moveAble;

    [SerializeField] bool _isInteractAble;
    [SerializeField] bool _isAbleSkill;

    [SerializeField] bool _busy;

    [SerializeField] bool _isDash;
    [SerializeField] bool _isJump;
    private bool _isGrounded;
    private bool _isInTheAir;

    [Header("Set Value")]
    float speed;
    float walkSpeed => stats.WalkSpeed;
    float runSpeed => stats.RunSpeed;

    [Header("Interect")]
    [SerializeField] float _interactRadius;

    [SerializeField] bool _isGetCarry;
    [SerializeField] GameObject _playerToCarry;
    [SerializeField] bool _isCarry;
    [SerializeField] bool _canCarry;

    [Header("CharacterSet")]
    [SerializeField] bool _isDuck;
    [SerializeField] bool _isEagle;

    [SerializeField] Vector2 _duckPosition = new Vector2(-2f, 1);
    [SerializeField] Vector2 _eaglePosition = new Vector2(-1.5f, -1.5f);
    [SerializeField] Vector2 _positionToBe;

    private void Awake() // Change to spawn
    {
        Setup();
    }

    private void Update()
    {
        if (moveAble)
        {
            UpdateMovement();
            UpdateActionInput();
            CheckItemInteract();
        }
        
    }

    private void Setup()
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

        action = GetComponent<CharacterAction>();
        if (action != null)
        {
            action.Setup();
        }
        else Debug.LogError("can't find Action");
    }

    private void UpdateMovement()
    {
        moveX = input.UpdateMoveInput();

        if (moveX != Vector2.zero)
        {
            if (input.ShiftAction.IsPressed())
            {
                Debug.Log("Run");
                speed = runSpeed;
            }
            else
            {
                speed = walkSpeed;
            }

            Vector2 movement = moveX * speed * Time.deltaTime;
            Debug.Log("movement : " + movement);
            rb2D.AddForce(movement, ForceMode2D.Impulse);
        }
    }

    #region InputZone

    public void UpdateActionInput()
    {
        if (_isJump)
        {
            if (input.JumpAction.WasPressedThisFrame() && _isGrounded)
            {
                _isJump = false;
                rb2D.AddForce(Vector2.up * stats.jumpForce, ForceMode2D.Force);

                _isInTheAir = true;
            }
        }

        if (_isInTheAir)
        {
            if (_isEagle && !_isGrounded)
            {

            }
        }

        if (_isAbleSkill)
        {
            if (_busy) return;

            if (input.SkillPressAction.WasPressedThisFrame())
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

            if (input.InteractAction.WasPressedThisFrame())
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
                }
                else { character.SetCollider(false); }

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
        if (stats.MinStamina > 0)
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
        else { Debug.Log("can't find carryObject"); }
        ;
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

        stats.TakeDamage(dmg);
    }
    #endregion

    private void OnCollisionEnter2D(Collision2D collision)
    {

        if (collision.collider.tag == "Ground" || collision.collider.tag == "Platform")
        {
            _isJump = true;
            _isGrounded = true;
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
        coll2D.isTrigger = o;
    }
}
