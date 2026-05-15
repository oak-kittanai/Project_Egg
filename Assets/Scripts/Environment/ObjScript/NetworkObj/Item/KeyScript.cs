using Fusion;
using UnityEngine;

public class KeyScript : NetworkObject, Interactable
{
    [Header("Setting")]
    [SerializeField] public bool isOrangeKey;
    [SerializeField] public bool isBlueKey;

    public void Interact(MovementCharacter player)
    {
        if (isOrangeKey)
        {
            GameManager.Instance.AddKey(true);
        }
        else if (isBlueKey)
        {
            GameManager.Instance.AddKey(false);
        }
    }

    public bool CanInteract(MovementCharacter player)
    {
        return true;
    }
}
