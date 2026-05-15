using Fusion;
using UnityEngine;

public class BreakableRock : NetworkBehaviour, Interactable
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Collider2D coll;

    [Networked, OnChangedRender(nameof(OnRockBroken))] public NetworkBool isBroken { get; set; }

    [SerializeField] NetworkObject selfNet;
    [SerializeField] NetworkObject itemToDrop;
    [SerializeField] int dropAmount = 1;

    [SerializeField] bool canDrop;
    [SerializeField] bool isPlayerSpecific;

    [SerializeField] Sprite alreadyBreakRock;
    [SerializeField] Animator animator;

    private void Awake()
    {
        if (selfNet == null) selfNet = GetComponent<NetworkObject>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
        if (coll == null) coll = GetComponent<Collider2D>();
    }

    public void Interact(MovementCharacter player)
    {
        if (player is Duck_Moveset duck)
        {
            duck.PlayHitAnimation_RPC();

            RPC_BreakRock();
        }
        else
        {
            Debug.Log("not duck");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_BreakRock()
    {
        if (itemToDrop != null && canDrop)
        {
            for (int i = 0; i < dropAmount; i++)
            {
                SpawnItem();
            }
            canDrop = false;

            isBroken = true;
        }
    }

    public void OnRockBroken()
    {
        ChangeSprite();
    }

    public void ChangeSprite()
    {
        if (coll != null) coll.enabled = false;
    }

    public void SpawnItem()
    {
        GameManager.Instance.SpawnDropItem(itemToDrop, transform.position);
    }

    public bool CanInteract(MovementCharacter player)
    {
        if (player is Duck_Moveset duck && canDrop)
        {
            return true;
        }
        else return false;
    }
}