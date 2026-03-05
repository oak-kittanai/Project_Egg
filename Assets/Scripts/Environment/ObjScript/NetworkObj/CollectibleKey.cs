using Fusion;
using UnityEngine;

public class CollectibleKey : NetworkBehaviour, Interactable
{
    public void Interact()
    {
        Debug.Log("Collet the key");

        GameManager.Instance.AddKey();

        GameManager.Instance.RequestDespawn(Object);
    }
}