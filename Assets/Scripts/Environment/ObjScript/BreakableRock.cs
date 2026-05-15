using Fusion;
using UnityEngine;

public class BreakableRock : NetworkBehaviour, Interactable
{
    [SerializeField] NetworkObject selfNet;
    [SerializeField] NetworkObject itemToDrop;
    [SerializeField] int dropAmount = 1;

    private void Awake()
    {
        if (selfNet == null) selfNet = GetComponent<NetworkObject>();
    }

    public void Interact(MovementCharacter player)
    {
        if (player is Duck_Moveset duck)
        {
            duck.PlayHitAnimation_RPC();

            RPC_BreakRock();
        }
        else
        {
            Debug.Log("not duck");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_BreakRock()
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