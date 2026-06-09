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

    public bool CanInteract(MovementCharacter player)
    {
        if (player is Duck_Moveset duck && canDrop && duck.isSmashUnlocked)
        {
            return true;
        }
        return false;
    }

    public void Interact(MovementCharacter player)
    {
        if (!HasStateAuthority) return;

        if (player is Duck_Moveset duck && duck.isSmashUnlocked)
        {
            duck.PlayHitAnimation_RPC();
            RPC_BreakRock();
        }
        else
        {
            Debug.Log("Interact failed: Not a duck or smash skill is locked.");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_BreakRock()
    {
        if (itemToDrop != null && canDrop)
        {
            animator.SetTrigger("Trigger");

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

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = alreadyBreakRock;
        }
    }

    public void SpawnItem()
    {
        if (!HasStateAuthority) return;
        GameManager.Instance.SpawnDropItem(itemToDrop, transform.position);
    }
}