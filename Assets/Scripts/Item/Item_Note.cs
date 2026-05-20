using Fusion;
using UnityEngine;

public class Item_Note : NetworkBehaviour, Interactable
{
    public void Interact(MovementCharacter player)
    {
        if (player.HasInputAuthority)
        {
            Debug.Log("เปิดโน้ต");
            //รำต่อเลย

        }
    }
    public bool CanInteract(MovementCharacter player)
    {
        return true;
    }
}
