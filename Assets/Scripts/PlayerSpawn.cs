using Fusion;
using System.Collections;
using UnityEngine;

public class PlayerSpawn : SingletonNetwork<PlayerSpawn>
{
    public NetworkRunner runner;
    INetworkStructure NetworkStructure;

    [SerializeField] public NetworkObject PlayerPrefab;
    [SerializeField] private NetworkObject PlayerPrefabPath;

    [SerializeField] int numPlayer;

    public void Setup()
    {
        Debug.Log("PS Awake Active");
        PlayerPrefabPath = Resources.Load<NetworkObject>("Prefabs/Character_Prefabs/Player");

        if (PlayerPrefabPath != null)
        {
            if (PlayerPrefab == null)
            {
                PlayerPrefab = PlayerPrefabPath;
            }
            else
            {
                Debug.Log("can't find PlayerPrefab");
            }
        }
        else
        {
            Debug.Log("can't find PlayerPath");
        }
    }

    public override void Spawned()
    {
        Debug.Log("PS Spawned active");
        StartCoroutine(WaitForSpawnInput());
    }

    IEnumerator WaitForSpawnInput()
    {
        yield return new WaitForSeconds(0.3f);
        if (runner == null)
        {
            Debug.Log("PlayerSpawner can't find Runner");
        }
        if (runner != null && runner.IsServer)
        {
            if (runner.SessionInfo.PlayerCount == 2)
            {
                Debug.Log("Has 2 Player on sever");
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        numPlayer = runner.SessionInfo.PlayerCount;
    }

    public void SpawnPlayer(PlayerRef player)
    {
        Debug.Log("Try Spawn Player");
        if (!runner.IsServer)
        {
            Debug.Log("Not Server");
            return;
        }
            
        NetworkObject playerObj = runner.Spawn(PlayerPrefab, new Vector3(0, 1, 0), Quaternion.identity, player);
        runner.SetPlayerObject(player, playerObj);

        CharacterStats playerStats = playerObj.GetComponent<CharacterStats>();
        if (player == runner.LocalPlayer)
        {
            playerStats.isDuck = true;
            playerStats.isBird = false;
            playerObj.name = "Player (Duck)Host";
            Debug.Log("Spawn Player Host");
        }
        else
        {
            playerStats.isDuck = false;
            playerStats.isBird = true;
            playerObj.name = "Player (Bird)Client";
            Debug.Log("Spawn Player Client");
        }
    }
}
