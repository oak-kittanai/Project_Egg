using Fusion;
using UnityEngine;

public class StoneItem : NetworkBehaviour, Interactable
{
    [Header("ColorSet")]
    [SerializeField] bool isOrangeStone;

    private void Collect()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.RPC_RequestAddStone(isOrangeStone);

        GameManager.Instance.RequestDespawn(Object);

        Debug.Log($"Picked up {(isOrangeStone ? "Orange" : "Blue")} Stone!");
    }

    public void Interact(MovementCharacter player)
    {
        Collect();
    }

    public bool CanInteract(MovementCharacter player)
    {
        return true;
    }
}