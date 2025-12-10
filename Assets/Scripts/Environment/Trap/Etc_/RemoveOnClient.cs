using Fusion;
using UnityEngine;

public class RemoveOnClient : NetworkBehaviour
{
    public override void Spawned()
    {
        if (!Object.HasStateAuthority)
        {
            RemoveFromClient();
        }
    }

    private void RemoveFromClient()
    {
        gameObject.SetActive(false);
    }
}
