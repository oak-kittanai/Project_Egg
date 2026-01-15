using Fusion;
using UnityEngine;

public class SpawnPlayer : SingletonNetwork<SpawnPlayer>
{
    [SerializeField] NetworkObject PlayerPrefab;
    [SerializeField] Vector3 spawnPos;

    [SerializeField] CharacterStats stats;
    [SerializeField] public characterType characterType;

    public void SpawnPlayerToPosition(PlayerRef player, NetworkRunner runner)
    {
        if (runner != null)
        {
            NetworkObject playerObj = runner.Spawn(PlayerPrefab, spawnPos, Quaternion.identity, player, InitializeObjBeforeSpawn);
            runner.SetPlayerObject(player, playerObj);
        }
        else Debug.Log("can't find Runner to spawn player");
    }


    private void InitializeObjBeforeSpawn(NetworkRunner runner, NetworkObject playerObj)
    {
        CharacterStats playerStats = playerObj.GetComponent<CharacterStats>();
        CharacterAnimation playerAnimation = playerObj.GetComponent<CharacterAnimation>();
        Bird_Moveset bird = playerObj.GetComponent<Bird_Moveset>();
        Duck_Moveset duck = playerObj.GetComponent<Duck_Moveset>();

        if (playerObj.InputAuthority == runner.LocalPlayer)
        {
            playerStats.skinType = characterType;
            playerObj.name = $"Player ({playerStats.skinType})Host";
            Debug.Log("Spawn Player Host");
            if (playerStats.skinType == characterType.Duck)
            {
                Destroy(bird);
            }
            else
            {
                Destroy(duck);
            }
            stats = playerStats;
        }

        Debug.Log("Local player configured: " + playerObj.name);
    }
}
