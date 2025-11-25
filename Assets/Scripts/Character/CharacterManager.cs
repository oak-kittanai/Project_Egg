using Fusion;
using System;
using UnityEngine;

public class CharacterManager : NetworkBehaviour, CharacterInteract, IDamageable
{
    [Header("Referent")]
    CharacterStats stats;
    InputControl input;
    CharacterAction action;
    CharacterAnimation cAnimation;
    MovementCharacter movement;

    // Mono
    Rigidbody2D rb2D;
    Collider2D coll2D;

    [Header("Interect")]
    [SerializeField] float _interactRadius;

    [SerializeField] bool _isGetCarry;
    [SerializeField] GameObject _playerToCarry;
    [SerializeField] bool _isCarry;
    [SerializeField] bool _canCarry;

    bool StaminaBusy => movement._staminaBusy;

    [SerializeField] bool _isInteractAble;
    [SerializeField] bool _isAbleSkill;

    [SerializeField] bool _busy;

    [SerializeField] Vector2 _duckPosition = new Vector2(-2f, 1); // not use anymore i think
    [SerializeField] Vector2 _eaglePosition = new Vector2(-1.5f, -1.5f);
    [SerializeField] Vector2 _positionToBe;

    public override void Spawned()
    {
        Setup();
    }

    public override void FixedUpdateNetwork()
    {
        if (movement._moveAble)
        {
            UpdateActionInputSkill();
            CheckItemInteract();
            movement.UpdateMovement();
            movement.UpdateActionInput();
            movement.UpdateStates();
            movement.UpdatePosition();
        }

        if (stats.s_minStamina < stats.s_maxStamina && !StaminaBusy)
        {
            stats.RechargeStamina(true);
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

        cAnimation = GetComponent<CharacterAnimation>();
        if (cAnimation != null)
        {
            cAnimation.Setup();
        }
        else Debug.LogError("can't find CharacterAnimation");

        movement = GetComponent<MovementCharacter>();
        if (movement != null)
        {
            movement.Setup();
        }
        else Debug.Log("can't find MovementCharacter");
    }

    #region InputZone
    public void UpdateActionInputSkill()
    {
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
                            stats.StaminaReduce(2);
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
                //CheckPlayerInteract(hit);
            }

            if (hit.gameObject.layer == LayerMask.NameToLayer("Interactable"))
            {
                //Debug.Log("Hit an Item: " + hit.name);
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
                    //Debug.Log("didn't found any trigger");
                    break;
            }
        }
        else
        {
            _isInteractAble = false;
        }
    }

    /*public void CheckPlayerInteract(Collider2D player)
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
    }*/

    #endregion

    #region Skill
    public GameObject CharacterInteract()
    {
        return this.gameObject;
    }

    /*public void UpdateCarryPos()
    {
        Vector2 selfPos = new Vector2(transform.position.x, transform.position.y);
        if (stats.MinStamina > 0)
        {
            if (IsDuck)
            {
                _positionToBe = _duckPosition + selfPos;
                _isCarry = true;
            }

            if (IsEagle)
            {
                _positionToBe = _eaglePosition + selfPos;
                _isCarry = true;
            }
        }
        else { Debug.Log("out of stamina"); _isCarry = false; }
    }*/

    private void CarryCompanion(GameObject carryObject)
    {
        if (carryObject != null)
        {
            //UpdateCarryPos();
        }
        else { Debug.Log("can't find carryObject"); }
        ;
    }

    #endregion

    #region AdjustValue
    public void TakeDamage(int dmg, float knockbackForce, Vector2 vec)
    {
        if (movement.isDash)
        {
            Debug.Log("Dodge");
            return;
        }

        Vector2 direction = (vec - (Vector2)transform.position).normalized;
        Vector2 knockbackDir = -direction * knockbackForce;

        rb2D.AddForce(knockbackDir, ForceMode2D.Impulse);

        stats.TakeDamage(dmg);
    }
    #endregion

    private void OnDrawGizmosSelected()
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
