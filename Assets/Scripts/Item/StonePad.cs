using Fusion;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class StonePad : NetworkBehaviour, Interactable
{
    [Header("Pad Shape")]
    public StoneShape requiredShape;

    [Networked] public NetworkBool IsFilled { get; set; }

    [Header("Visual")]
    public Sprite emptySprite;
    public Sprite fillSprite;
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Interact(MovementCharacter player)
    {
        if (IsFilled) return;

        if (StoneInventory.Instance != null && StoneInventory.Instance.HasStone(requiredShape))
        {
            RPC_InsertStone();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_InsertStone()
    {
        if (IsFilled) return;

        if (StoneInventory.Instance.HasStone(requiredShape))
        {
            StoneInventory.Instance.UseStone_RPC(requiredShape);
            IsFilled = true;
        }
    }

    public bool CanInteract(MovementCharacter player)
    {
        return !IsFilled;
    }

    public override void Render()
    {
        if (sr != null)
        {
            sr.sprite = IsFilled ? fillSprite : emptySprite;
        }
    }
}