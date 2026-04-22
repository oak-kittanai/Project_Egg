using Fusion;
using UnityEngine;

public class BreakableRock : NetworkBehaviour, Interactable
{
    [SerializeField] NetworkObject selfNet;

    [SerializeField] NetworkObject itemToDrop;
    [SerializeField] int dropAmount = 1;
    [SerializeField] float dropForce = 3f;

    private void Awake()
    {
        selfNet = GetComponent<NetworkObject>();
    }

    public void Interact(MovementCharacter player)
    {
        if (!HasStateAuthority) return;

        if (player is Duck_Moveset duck)
        {
            duck.PlayHitAnimation_RPC();

            BreakAndDrop();
        }
        else
        {
            Debug.Log("can't find duck");
        }
    }   

    void BreakAndDrop()
    {
        if (itemToDrop != null)
        {
            for (int i = 0; i < dropAmount; i++)
            {
                SpawnItem();
            }
        }

        GameManager.Instance.RequestDespawn(selfNet);
    }

    public void SpawnItem()
    {
        GameManager.Instance.SpawnDropItem(itemToDrop, transform.position);
    }
}