using Fusion;
using System;
using UnityEditor.Presets;
using UnityEngine;

public class Duck_Moveset : MovementCharacter, CharacterInteract
{
    [Header("OnTheWaterSetting")]
    [SerializeField] bool onWater;
    [SerializeField] bool diveIntoWater;

    [Header("Item Interact")]
    [SerializeField] bool _isInteractAble;
    [SerializeField] bool _isAbleSkill;

    [Header("Value")]
    [SerializeField] public bool canInteract;
    [SerializeField] public bool isCarry;

    private void Awake()
    {
        //cAnimation = GetComponent<CharacterAnimation>();
    }

    public override void FixedUpdateNetwork()
    {
        if (_isWaterGround)
        {
            onWater = true;
        }
        else
        {
            onWater = false;
        }

        while (onWater)
        {
            cAnimation.UpdateGroundTypeOnDuck(onWater);
        }
    }

    #region Interact Zone

    public GameObject CharacterInteract()
    {
        return this.gameObject;
    }

    public void DropCharacter()
    {
        throw new NotImplementedException();
    }

    public void SetCollider(bool o)
    {
        coll2D.isTrigger = o;
    }

    public void CheckItemInteract()
    {
        Vector2 player = transform.position;
        int mask = LayerMask.GetMask("Player", "Interactable", "ThrowAbleProjectile");
        Collider2D[] itemhitInteract = Physics2D.OverlapCircleAll(player, _interactRadius, mask);

        foreach (Collider2D hit in itemhitInteract)
        {
            if (hit == null || hit.gameObject == this.gameObject)
                continue;

            _isInteractAble = true;
            if (hit.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                //Debug.Log("Detect Player");
                canInteract = true;
                Interact(hit);
            }
            else canInteract = false;

            if (hit.gameObject.layer == LayerMask.NameToLayer("Interactable"))
            {
                Debug.Log("Detect Interactable Object");
                canInteract = true;
                Interact(hit);
            }
            else canInteract = false;

            if (hit.gameObject.layer == LayerMask.NameToLayer("ThrowAbleProjectile"))
            {
                canInteract = true; // Work fine
                Interact(hit);
            }
            else canInteract = false;
        }
    }

    public void Interact(Collider2D hit)
    {
        Vector2 player = transform.position;

        if (hit != null)
        {
            _isInteractAble = true;
            Vector2 selfpos = new Vector2(transform.position.x, transform.position.y);
            if (pressed && canInteract)
            {
                switch (hit.gameObject)
                {
                    case GameObject g when g.TryGetComponent<Interactable>(out var obj):
                        Debug.Log("trigger interactable object");
                        if (stats.skinType == characterType.Bird)
                        {
                            cAnimation.SmashAnimation();
                            obj.Interact();
                        }
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
        }
        else
        {
            _isInteractAble = false;
        }
    }
    #endregion
}
