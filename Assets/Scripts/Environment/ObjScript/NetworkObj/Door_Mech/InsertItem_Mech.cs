using Fusion;
using UnityEngine;

public class InsertItem_Mech : NetworkBehaviour, Interactable
{
    [Header("Sprites")]
    [SerializeField] Sprite emptyDoor;
    [SerializeField] Sprite orangeStoneDoor;
    [SerializeField] Sprite blueStoneDoor;
    [SerializeField] Sprite fullStoneDoor;

    [Header("Movement")]
    [SerializeField] float openHeight = 3f;
    [SerializeField] float openSpeed = 2f;

    private SpriteRenderer sr;

    [Networked] public NetworkBool HasOrangeStone { get; set; }
    [Networked] public NetworkBool HasBlueStone { get; set; }

    private Vector3 closedPosition;
    private Vector3 openPosition;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public override void Spawned()
    {
        closedPosition = transform.position;
        openPosition = closedPosition + Vector3.up * openHeight;
    }

    public void Interact()
    {
        if (HasStateAuthority)
        {
            ExecuteInsert();
        }
        else
        {
            RPC_InsertStone();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_InsertStone()
    {
        ExecuteInsert();
    }

    private void ExecuteInsert()
    {
        if (GameManager.Instance == null) return;

        bool insertedAny = false;
        if (!HasOrangeStone && GameManager.Instance.TeamHasOrangeStone)
        {
            HasOrangeStone = true;
            GameManager.Instance.TeamHasOrangeStone = false;
            insertedAny = true;
            Debug.Log("Inserted Orange Stone!");
        }

        if (!HasBlueStone && GameManager.Instance.TeamHasBlueStone)
        {
            HasBlueStone = true;
            GameManager.Instance.TeamHasBlueStone = false;
            insertedAny = true;
            Debug.Log("Inserted Blue Stone!");
        }

        if (!insertedAny)
        {
            Debug.Log("You don't have any Stones");
        }
    }

    public override void Render()
    {
        if (HasOrangeStone && HasBlueStone)
        {
            sr.sprite = fullStoneDoor;
        }
        else if (HasOrangeStone)
        {
            sr.sprite = orangeStoneDoor;
        }
        else if (HasBlueStone)
        {
            sr.sprite = blueStoneDoor;
        }
        else
        {
            sr.sprite = emptyDoor;
        }

        Vector3 targetPos = (HasOrangeStone && HasBlueStone) ? openPosition : closedPosition;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, openSpeed * Time.deltaTime);
    }
}