using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : SingletonNetwork<GameManager>
{
    [SerializeField] NetworkRunner NetworkRunner;

    [SerializeField] GameObject playerHost;
    [SerializeField] GameObject playerClient;

    [SerializeField] Vector3 respawnPos;

    [Header("Game Setting")]

    [Networked] public int MapsLoadedCount { get; set; }
    [Networked] public NetworkBool isPlayerReady { get; set; }
    [Networked] public NetworkBool isLoadMapDone { get; set; }
    [Networked] public NetworkBool IsGameReady { get; set; }
    [Networked] public int PlayersReadyCount { get; set; }

    // Loading scene
    private GameObject currentLoadingUI;

    [Networked] public bool gameOver { get; set; }
    [Networked] public int TeamKeys { get; set; }

    public override void Spawned()
    {
        base.Spawned();
    }

    public void GetNetworkRunner(NetworkRunner networkRunner)
    {
        NetworkRunner = networkRunner;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_PlayerFinishedLoading()
    {
        PlayersReadyCount++;
        CheckGameStart();
    }

    public void PlayerFinishedLoading()
    {
        if (HasStateAuthority)
        {
            PlayersReadyCount++;
            CheckGameStart();
        }
        else
        {
            RPC_PlayerFinishedLoading();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_MapFinishedLoading()
    {
        MapsLoadedCount++;
        CheckMapLoading();
    }
    public void MapFinishedLoading()
    {
        if (HasStateAuthority)
        {
            MapsLoadedCount++;
            CheckMapLoading();
        }
        else
        {
            RPC_MapFinishedLoading();
        }
    }

    private void CheckMapLoading()
    {
        if (MapsLoadedCount >= 2 && !isLoadMapDone)
        {
            isLoadMapDone = true;
            Debug.Log("Map Ready");
            CheckGameStart();
        }
    }

    private void CheckGameStart()
    {
        if (PlayersReadyCount >= 2 && !isPlayerReady)
        {
            isPlayerReady = true;
            Debug.Log("Player Ready");
        }

        if (isPlayerReady && isLoadMapDone && !IsGameReady)
        {
            IsGameReady = true;
            Debug.Log("All Ready! Game Start!");
        }
    }

    public override void Render()
    {
        if (IsGameReady && currentLoadingUI != null && currentLoadingUI.activeSelf)
        {
            currentLoadingUI.SetActive(false);
        }
    }

    public Vector3 GetRespawnPosition()
    {
        return respawnPos;
    }

    public void UpdateRespawnPos(Vector3 newPos)
    {
        respawnPos = newPos;
    }

    // Key
    public void AddKey()
    {
        if (HasStateAuthority) TeamKeys++;
        else RPC_RequestAddKey();
    }

    public void UseKey()
    {
        if (HasStateAuthority) TeamKeys--;
        else RPC_RequestUseKey();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestAddKey() => TeamKeys++;

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestUseKey() => TeamKeys--;

    public void RequestDespawn(NetworkObject objToDespawn)
    {
        if (HasStateAuthority) NetworkRunner.Despawn(objToDespawn);
        else RPC_Despawn(objToDespawn.Id);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_Despawn(NetworkId objId)
    {
        if (NetworkRunner.TryFindObject(objId, out var obj))
        {
            NetworkRunner.Despawn(obj);
        }
    }

    public void ProjectileSpawn(NetworkObject objToSpawn, Vector2 posToSpawn, Vector2 direction, Quaternion rota)
    {
        if (!HasStateAuthority) return;

        Vector3 spawnPos = new Vector3(posToSpawn.x, posToSpawn.y, 0f);
        NetworkRunner.Spawn(objToSpawn, spawnPos, rota);

        Rigidbody2D rb = objToSpawn.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * 10f;
        }
    }

    public void SpawnDropItem(NetworkObject objToSpawn, Vector2 posToSpawn)
    {
        if (!HasStateAuthority) return;

        Vector3 spawnPos = new Vector3(posToSpawn.x, posToSpawn.y, 0f);
        NetworkObject objIte = NetworkRunner.Spawn(objToSpawn, posToSpawn);

        float dropForce = Random.Range(0.5f, 1.5f);

        Rigidbody2D rb = objIte.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 randomDir = new Vector2(Random.Range(-1f, 1f), Random.Range(0.5f, 1f)).normalized;
            rb.AddForce(randomDir * dropForce, ForceMode2D.Impulse);
        }
    }

    // LoadLevel & Player

    public List<MovementCharacter> activePlayers = new List<MovementCharacter>();

    [SerializeField] public CheckPoint[] checkPoints;
    //public NetworkDoor currentExitDoor; 

    public void RegisterPlayer(MovementCharacter player)
    {
        if (!activePlayers.Contains(player))
        {
            activePlayers.Add(player);
            Debug.Log($"[GameManager] Player {player.Object.Id} Has Joined");
        }
    }
    public void SetupLevelData(LevelData data)
    {
        UpdateRespawnPos(data.startingSpawnPosition);

        checkPoints = data.levelCheckPoints;

        // door for event
        //currentExitDoor = data.mainExitDoor;
    }


    [Header("Team Inventory")]
    [Networked] public NetworkBool TeamHasOrangeStone { get; set; }
    [Networked] public NetworkBool TeamHasBlueStone { get; set; }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestAddStone(bool isOrange) 
    {
        if (isOrange)
        {
            TeamHasOrangeStone = true;
        }
        else TeamHasBlueStone = true; 
    }

}

[System.Serializable]
public class CheckPoint
{
    public Vector3 spawnPointPos;
    public float currentMapProgress;
}