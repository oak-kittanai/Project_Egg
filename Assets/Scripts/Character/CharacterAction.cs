using Fusion;
using System;
using UnityEngine;

public class CharacterAction : NetworkBehaviour, CharacterInteract
{
    InputControl controller;
    CharacterStats stats;

    Rigidbody2D rb2D;
    Collider2D coll2D;

    public SkillSet characterSkillSet;

    [SerializeField] bool _isInteractAble;
    [SerializeField] bool _isAbleSkill;
    [SerializeField] float _interactRadius;
    [SerializeField] bool _busy;

    private void Awake()
    {
        Setup();
    }

    public override void Spawned()
    {
        base.Spawned();
        if (HasInputAuthority)
        {

        }
    }

    public void Setup()
    {
        controller = GetComponent<InputControl>();
        stats = GetComponent<CharacterStats>();
    }

    public GameObject CharacterInteract()
    {
        return this.gameObject;
    }

    public void Flying(float stamina, bool onground, float flyspeed, Rigidbody2D rb2D)
    {
        if (stamina > 0 && !onground)
        {
            Debug.Log("Flyyy");
            Vector2 movement = Vector2.up * flyspeed * Time.deltaTime;
            rb2D.AddForce(movement, ForceMode2D.Impulse);
        }
        else
        {
            Debug.Log("not enough stamina");
        }
    }

    #region InputZone
    public void UpdateActionInputSkill()
    {
        if (_isAbleSkill)
        {
            if (_busy) return;

            /*if (input.SkillPressAction.WasPressedThisFrame())
            {
                _busy = true;
                // do skill

                // return _busy = false; return _busy to false
            }*/
        }
    }

    public void InteractAble(Collider2D hit)
    {
        if (_isInteractAble)
        {
            if (_busy) return;

            /*if (input.InteractAction.WasPressedThisFrame())
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

            }*/
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

    #region CarryOn

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
        else { Debug.Log("can't find carryObject"); };
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
