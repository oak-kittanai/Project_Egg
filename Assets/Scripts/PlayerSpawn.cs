using Fusion;
using System.Collections;
using UnityEngine;

public class PlayerSpawn : SingletonNetwork<PlayerSpawn>
{
    public NetworkRunner runner;

    [SerializeField] public NetworkObject PlayerPrefab;
    [SerializeField] private NetworkObject PlayerPrefabPath;

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

    [Rpc(RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    public void SpawnPlayer_RPC(PlayerRef player)
    {
        Debug.Log("Try Spawn Player");
        Vector2 startPos = new Vector2(5.19f, -0.38f);
        NetworkObject playerObj = runner.Spawn(PlayerPrefab, startPos, Quaternion.identity, player);
        runner.SetPlayerObject(player, playerObj);
        SetSpecis_RPC(playerObj, player);
    }

    [Rpc(RpcSources.All,RpcTargets.All)]
    public void SetSpecis_RPC(NetworkObject playerObj, PlayerRef player)
    {
        CharacterStats playerStats = playerObj.GetComponent<CharacterStats>();
        CharacterAnimation playerAnimation = playerObj.GetComponent<CharacterAnimation>();

        if (player == runner.LocalPlayer)
        {
            playerAnimation.onChangeSkin = controller_Duck;
            playerStats.isDuck = true;
            playerStats.isBird = false;
            playerObj.name = "Player (Duck)Host";
            Debug.Log("Spawn Player Host");
        }
        else
        {
            playerAnimation.onChangeSkin = controller_Bird;
            playerStats.isDuck = false;
            playerStats.isBird = true;
            playerObj.name = "Player (Bird)Client";
            Debug.Log("Spawn Player Client");
        }
    }
}
