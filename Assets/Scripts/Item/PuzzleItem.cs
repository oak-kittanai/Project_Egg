using Fusion;
using UnityEngine;

public class PuzzleItem : NetworkBehaviour, Interactable
{
    [Header("Item Settings")]
    [SerializeField] string itemName = "";
    public string ItemName => itemName;
    [SerializeField] NetworkObject selfNet;
    [SerializeField] SpriteRenderer spriteRenderer;

    [Header("Outline Highlight")]
    [SerializeField] float highlightRadius = 1.5f;
    [SerializeField] LayerMask playerMask;
    [SerializeField] string shaderFloatProperty = "_OutlineThickness";
    [SerializeField] float activeValue = 3f;
    [SerializeField] float inactiveValue = 0f;

    private MaterialPropertyBlock mpb;
    private bool isHighlighted;

    private void Awake()
    {
        if (selfNet == null) selfNet = GetComponent<NetworkObject>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        mpb = new MaterialPropertyBlock();
        SetOutline(false);
    }

    private void Update()
    {
        bool playerNear = IsAnyPlayerNear();

        if (playerNear != isHighlighted)
        {
            isHighlighted = playerNear;
            SetOutline(isHighlighted);
        }
    }

    private bool IsAnyPlayerNear()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, highlightRadius, playerMask);

        foreach (var hit in hits)
        {
            MovementCharacter player = hit.GetComponent<MovementCharacter>();
            if (player == null || !player.enabled) continue;

            if (player.HeldItemName.ToString() == "") return true;
        }
        return false;
    }

    private void SetOutline(bool show)
    {
        if (spriteRenderer == null || mpb == null) return;

        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(shaderFloatProperty, show ? activeValue : inactiveValue);
        spriteRenderer.SetPropertyBlock(mpb);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, highlightRadius);
    }

    // ── existing methods ──
    public void Interact(MovementCharacter player)
    {
        if (player.HeldItemName.ToString() != "")
        {
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
        }
    }
}