using Fusion;
using UnityEngine;

public class FStoneItem : NetworkBehaviour, Interactable
{
    [Header("Stone Shape")]
    public StoneShape stoneShape; //ใส่ทรง

    private void Collect()
    {
        if (StoneInventory.Instance != null)
        {
            StoneInventory.Instance.AddStone_RPC(stoneShape);
            Runner.Despawn(Object);
        }
    }

    public void Interact(MovementCharacter player)
    {
        Collect();
    }

    public bool CanInteract(MovementCharacter player) => true;
}