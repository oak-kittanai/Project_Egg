using Fusion;
using System;
using System.Collections;
using UnityEngine;

public class CenterHost : SingletonNetwork<CenterHost>
{
    [Header("Spawning")]
    [SerializeField] Vector2 HostSpawnPos;
    [SerializeField] Vector2 ClientSpawnPos;
    public NetworkObject BirdPrefab;
    public NetworkObject DuckPrefab;

    [Header("Set Player")] // has Access to go everywhere
    [SerializeField] NetworkRunner hostRunner;
    [SerializeField] INetworkStructure netStructure;
    [SerializeField] PlayerRef hostPlayer;
    [SerializeField] CharacterStats hostStats;

    [SerializeField] NetworkRunner clientRunner;
    [SerializeField] PlayerRef clientPlayer;
    [SerializeField] CharacterStats clientStats;

    [Networked] public bool RemoveOldObj { get; set; }

    [Networked] public characterType currentHost { get; set; }
    [Networked] public characterType currentClient { get; set; }

    [Header("Core Systems (Local Prefabs)")]
    [SerializeField] private GameObject canvasPrefab;
    [SerializeField] private GameObject coreManagerPrefab;

    public void GetRunner()
    {
        if (hostRunner != null)
        {
            netStructure = hostRunner.GetComponent<INetworkStructure>();
        }
    }

    public override void Spawned()
    {
        base.Spawned();
        SetupGameCore();
    }

    private void SetupGameCore()
    {
        GameObject canvasObj = GameObject.Find(canvasPrefab.name);

        if (canvasObj == null && canvasPrefab != null)
        {
            canvasObj = Instantiate(canvasPrefab);
            canvasObj.name = canvasPrefab.name;
            DontDestroyOnLoad(canvasObj);
        }

        if (canvasObj != null)
        {
            PlayerInterface.Instance?.RegisterCanvas(canvasObj);
            TutorialUIManager.Instance?.RegisterCanvas(canvasObj);
        }

        string coreName = "CoreManagerSceneHop";
        if (GameObject.Find(coreName) == null && coreManagerPrefab != null)
        {
            GameObject core = Instantiate(coreManagerPrefab);
            core.name = coreName;
            DontDestroyOnLoad(core);
        }
    }


    #region ComponentZone

    public void AddPlayerRef(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            hostPlayer = player;
        }
        else
        {
            clientPlayer = player;
        }
    }

    public void AddComponent(NetworkRunner runner, INetworkStructure iNetStruct, NetworkObject birdPrefab, NetworkObject duckPrefab)
    {
        if (runner.IsServer)
        {
            hostRunner = runner;
            netStructure = iNetStruct;
        }
        else
        {
            clientRunner = runner;
        }

        if (birdPrefab != null) BirdPrefab = birdPrefab;
        if (duckPrefab != null) DuckPrefab = duckPrefab;
    }

    public void CheckComponentPlayer(PlayerRef player)
    {
        Debug.Log("Access to check componenet, player is : " + player);
        if (hostPlayer == null)
        {
            Debug.LogError("can't find PlayerRef of host");
            return;
        }
        else if (clientPlayer == null)
        {
            Debug.LogError("can't find PlayerRef of client");
        }

        if (player == hostPlayer) Debug.Log("Host Player Ready to spawn");
        if (player == clientPlayer) Debug.Log("Client Player Ready to spawn");
    }

    #endregion

    #region SpawnZone
    IEnumerator WaitForSecToSpawn(PlayerRef player)
    {
        yield return new WaitForSeconds(0.4f);
    }


    public void SpawnPlayer(PlayerRef player, characterType Type, bool isHost)
    {
        if (isHost) currentHost = Type;
        else currentClient = Type;

        NetworkObject prefabToSpawn = (Type == characterType.Bird) ? BirdPrefab : DuckPrefab;
        Vector2 spawnPos = isHost ? HostSpawnPos : ClientSpawnPos;

        if (hostRunner != null)
        {
            NetworkObject playerObj = hostRunner.Spawn(prefabToSpawn, spawnPos, Quaternion.identity, player, (runner, obj) =>
            {
                CharacterStats playerStats = obj.GetComponent<CharacterStats>();
                if (playerStats != null)
                {
                    playerStats.skinType = Type;
                }

                obj.name = $"{Type}";
                Debug.Log($"Initialized Network Data for: {obj.name}");
            });

            hostRunner.SetPlayerObject(player, playerObj);
        }
        else
        {
            Debug.LogError("Can't find Runner to spawn player");
        }
    }

    #endregion

    #region ReTry Zone

    private void TryGetComponentFromNetObject<T>(NetworkObject netObj, ref T com) where T : Component
    {
        com = netObj.GetComponent<T>();
    }

    private void TryGetComponentFromGameObject<T>(GameObject obj, ref T com) where T : Component
    {
        com = obj.GetComponent<T>();
    }

    private void TryReComponent<T>(ref T com) where T : Component
    {
        com = GetComponent<T>();
    }

    private T Refresh<T>() where T : Component
    {
        return GetComponent<T>();
    }

    #endregion
}

[Serializable]
public class ObjectTransform
{
    public string nameObj;
    public NetworkObject netObj;
    public GameObject oldObj;
    public Vector2 position;
    public Quaternion rotation;
}