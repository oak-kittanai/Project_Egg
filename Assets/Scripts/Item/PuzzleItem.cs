using Fusion;
using UnityEngine;

public class PuzzleItem : NetworkBehaviour, Interactable
{
    [Header("Item Settings")]
    [SerializeField] string itemName = "";

    [SerializeField] NetworkObject selfNet;
    [SerializeField] SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (selfNet == null) selfNet = GetComponent<NetworkObject>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Interact(MovementCharacter player)
    {
        if (player.HeldItemName.ToString() != "")
        {
            Debug.Log("hand full can't pick");
            return;
        }

        PickupPuzzleItem_RPC(player);
    }

    public bool CanInteract(MovementCharacter player)
    {
        return player.HeldItemName.ToString() == "";
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void PickupPuzzleItem_RPC(MovementCharacter player)
    {
        if (Object != null && Object.IsValid)
        {
            player.HeldItemName = itemName;

            GameManager.Instance.RequestDespawn(selfNet);
            Debug.Log($"{player.name} pick {itemName}");
        }
    }
}