using Fusion;
using UnityEngine;

public class BreakableRock : NetworkBehaviour, Interactable
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Collider2D collider2D;

    [SerializeField] NetworkObject selfNet;
    [SerializeField] NetworkObject itemToDrop;
    [SerializeField] int dropAmount = 1;

    [SerializeField] bool canDrop;

    [SerializeField] Sprite alreadyBreakRock;
    [SerializeField] Animator animator;

    private void Awake()
    {
        if (selfNet == null) selfNet = GetComponent<NetworkObject>();
        if (spriteRenderer  == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (collider2D == null) collider2D = GetComponent<Collider2D>();
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
                collider2D.enabled = false;
            }
        }
    }

    public void ChangeSprite()
    {
        animator.Play(""); // add Break Animation
    }

    public void SpawnItem()
    {
        GameManager.Instance.SpawnDropItem(itemToDrop, transform.position);
    }
}