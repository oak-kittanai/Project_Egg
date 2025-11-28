using Fusion;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerSpawn : SingletonNetwork<PlayerSpawn>
{
    public NetworkRunner runner;

    [SerializeField] public NetworkObject PlayerPrefab;
    [SerializeField] private NetworkObject PlayerPrefabPath;


    [SerializeField] Vector2 startPos;
    [SerializeField] int numPlayer;

    [SerializeField] RuntimeAnimatorController controller_Bird;
    [SerializeField] RuntimeAnimatorController controller_Duck;

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

    public override void Spawned()
    {
        Debug.Log("PS Spawned");
    }


    public override void FixedUpdateNetwork()
    {
        numPlayer = runner.SessionInfo.PlayerCount;
    }


    public void SpawnPlayer(PlayerRef player)
    {
        Debug.Log("Try Spawn Player");
        Vector2 spawnPos = startPos;
        if (runner != null)
        {
            NetworkObject playerObj = runner.Spawn(PlayerPrefab, startPos, Quaternion.identity, player, InitializeObjBeforeSpawn);
            runner.SetPlayerObject(player, playerObj);
        }
        else Debug.Log("can't find Runner to spawn player");
    }

    private void InitializeObjBeforeSpawn(NetworkRunner runner, NetworkObject playerObj)
    {
        CharacterStats playerStats = playerObj.GetComponent<CharacterStats>();
        CharacterAnimation playerAnimation = playerObj.GetComponent<CharacterAnimation>();
        if (playerObj.InputAuthority == runner.LocalPlayer)
        {
            playerStats.skinType = SkinType.Duck;
            playerObj.name = "Player (Duck)Host";
            Debug.Log("Spawn Player Host");
        }
        else
        {
            playerStats.skinType = SkinType.Bird;
            playerObj.name = "Player (Bird)Client";
            Debug.Log("Spawn Player Client");
        }

        Debug.Log("Local player configured: " + playerObj.name);
    }
}
