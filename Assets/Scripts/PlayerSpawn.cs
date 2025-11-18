using Fusion;
using System.Collections;
using UnityEngine;

public class PlayerSpawn : NetworkBehaviour
{
    public NetworkRunner runner;

    [Networked] public NetworkObject Player { get; set; }

    [SerializeField] int numPlayer;

    public override void Spawned()
    {
        StartCoroutine(WaitForSpawnInput());
    }

    IEnumerator WaitForSpawnInput()
    {
        yield return new WaitForSeconds(1);
        if (runner == null && HasStateAuthority)
        {
            Debug.Log("PlayerSpawner can't find Runner");
        }
        if (runner != null)
        {
            if (runner.SessionInfo.PlayerCount == 2)
            {
                Debug.Log("Has 2 Player on sever");
                // Spawn Player 
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        numPlayer = runner.SessionInfo.PlayerCount;
    }
}
