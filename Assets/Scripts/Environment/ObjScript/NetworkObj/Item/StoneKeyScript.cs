using Fusion;
using UnityEngine;

public class StoneKeyScript : NetworkBehaviour, Interactable
{
    [Header("Setting")]
    [SerializeField] public bool isOrangeStone;
    [SerializeField] public bool isBlueStone;

    public void Interact(MovementCharacter player)
    {
        if (isOrangeStone)
        {
            GameManager.Instance.RPC_RequestAddStone(true);
        }
        else if (isBlueStone)
        {
            GameManager.Instance.RPC_RequestAddStone(false);
        }
    }
}
