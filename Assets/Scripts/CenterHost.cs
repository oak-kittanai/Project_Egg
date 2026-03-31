using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CenterHost : SingletonNetwork<CenterHost>
{
    [Header("Spawning")]
    [SerializeField] Vector2 HostSpawnPos;
    [SerializeField] Vector2 ClientSpawnPos;
    [SerializeField] NetworkObject PlayerPrefab;

    [Header("Set Player")] // has Access to go everywhere
    [SerializeField] NetworkRunner hostRunner;
    [SerializeField] INetworkStructure netStructure;
    [SerializeField] PlayerRef hostPlayer;
    [SerializeField] CharacterStats hostStats;

    [SerializeField] NetworkRunner clientRunner;
    [SerializeField] PlayerRef clientPlayer;
    [SerializeField] CharacterStats clientStats;

    [Networked] public bool RemoveOldObj { get; set; }

    [Networked] characterType currentHost { get; set; }
    [Networked] characterType currentClient { get; set; }
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
        if (hostRunner != null)
        {
            Debug.Log("Host runner ready");
        }

        DontDestroyOnLoad(gameObject);
    }

    public override void FixedUpdateNetwork()
    {
        /*if (Runner.SessionInfo.PlayerCount == 2)
        {
            if (firstStart)
            {
                StartCoroutine(WaitForLoad());
            }
        }*/
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

    public void AddComponent(NetworkRunner runner, INetworkStructure iNetStruct, NetworkObject playerObject)
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

        if (playerObject != null)
        {
            PlayerPrefab = playerObject;
        }
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

        if (player == hostPlayer)
        {
            if (PlayerPrefab == null)
            {
                Debug.LogWarning("can't find NetworkObject of host");
                TryReComponent(ref PlayerPrefab);
                if (PlayerPrefab == null)
                {
                    Debug.LogWarning("can't get NetworkObject of host");
                    return;
                }
                else
                {
                    if (PlayerPrefab != null)
                    {
                        StartCoroutine(WaitForSecToSpawn(player));
                    }
                }
            }
            else
            {
                Debug.Log("Host Player Ready to spawn");
                if (PlayerPrefab != null)
                {
                    //StartCoroutine(WaitForSecToSpawn(player));
                }
            }
        }

        if (player == clientPlayer)
        {
            Debug.Log("Client Player Ready to spawn");
        }
    }

    #endregion

    #region SpawnZone
    IEnumerator WaitForSecToSpawn(PlayerRef player)
    {
        yield return new WaitForSeconds(0.4f);
        //SpawnPlayer(player);
    }


    public void SpawnPlayer(PlayerRef player, characterType Type, bool isHost)
    {
        Vector2 spawnPos = isHost ? HostSpawnPos : ClientSpawnPos;
        Debug.Log($"Try Spawn Player: {Type} at {spawnPos}");

        if (hostRunner != null)
        {
            NetworkObject playerObj = hostRunner.Spawn(PlayerPrefab, spawnPos, Quaternion.identity, player, (runner, obj) =>
            {
                CharacterStats playerStats = obj.GetComponent<CharacterStats>();
                if (playerStats != null)
                {
                    playerStats.skinType = Type;
                }

                obj.name = $"Player ({Type}) {(isHost ? "Host" : "Client")}";
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

    private void TryGetComponentFromGameObject<T>(GameObject obj,ref T com) where T : Component
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