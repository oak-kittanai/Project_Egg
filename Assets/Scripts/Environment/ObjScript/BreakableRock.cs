using Fusion;
using UnityEngine;

public class BreakableRock : NetworkBehaviour, Interactable
{
    [SerializeField] SpriteRenderer spriteRenderer;

    [SerializeField] NetworkObject selfNet;
    [SerializeField] NetworkObject itemToDrop;
    [SerializeField] int dropAmount = 1;

    [SerializeField] bool canDrop;

    [SerializeField] Sprite alreadyBreakRock;

    private void Awake()
    {
        if (selfNet == null) selfNet = GetComponent<NetworkObject>();
        if (spriteRenderer  == null) spriteRenderer = GetComponent<SpriteRenderer>();
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
                canDrop = false;
                ChangeSprite();
            }
        }

        GameManager.Instance.RequestDespawn(selfNet);
    }

    public void ChangeSprite()
    {
        spriteRenderer.sprite = alreadyBreakRock;
    }

    public void SpawnItem()
    {
        GameManager.Instance.SpawnDropItem(itemToDrop, transform.position);
    }
}