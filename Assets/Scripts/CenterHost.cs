using Fusion;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class CenterHost : SingletonNetwork<CenterHost>
{
    [Header("Spawning")]
    [SerializeField] Vector2 SpawnPos;
    [SerializeField] NetworkObject PlayerPrefab;

    [Header("Set Player")] // has Access to go everywhere
    [SerializeField] NetworkRunner hostRunner;
    [SerializeField] INetworkStructure netStructure;
    [SerializeField] PlayerRef hostPlayer;
    [SerializeField] CharacterStats hostStats;

    [SerializeField] NetworkRunner clientRunner;
    [SerializeField] PlayerRef clientPlayer;
    [SerializeField] CharacterStats clientStats;

    [Header("Object")]
    public List<ObjectTransform> ObjTrans;

    public NetworkObject Rock;

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

        Debug.Log("SpawnObject");
    }


    public void CheckForTrapAndMoveAbleObject()
    {
        // Check The game for the object and then respawn them again 
    }

    public override void FixedUpdateNetwork()
    {
        
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
                    StartCoroutine(WaitForSecToSpawn(player));
                }

                /*if (hostStats == null)
                {
                    Debug.LogError("can't find PlayerStats of host");
                    TryGetComponentFromNetObject(playerObjectPrefabs, ref hostStats);
                    if (hostStats == null)
                    {
                        Debug.LogError("can't get stats from host");
                        return;
                    }
                }
                else
                {
                    
                    // Do spawn host player
                    //SpawnHostCharacter();
                }*/
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
        SpawnPlayer(player);
    }


    public void SpawnPlayer(PlayerRef player)
    {
        Debug.Log("Try Spawn Player");
        Vector2 spawnPos = SpawnPos;
        if (hostRunner != null)
        {
            NetworkObject playerObj = hostRunner.Spawn(PlayerPrefab, spawnPos, Quaternion.identity, player, InitializeObjBeforeSpawn);
            hostRunner.SetPlayerObject(player, playerObj);
        }
        else Debug.Log("can't find Runner to spawn player");
    }

    private void InitializeObjBeforeSpawn(NetworkRunner runner, NetworkObject playerObj)
    {
        CharacterStats playerStats = playerObj.GetComponent<CharacterStats>();
        CharacterAnimation playerAnimation = playerObj.GetComponent<CharacterAnimation>();
        if (playerObj.InputAuthority == runner.LocalPlayer)
        {
            playerStats.skinType = SkinType.Bird;
            playerObj.name = $"Player ({playerStats.skinType})Host";
            Debug.Log("Spawn Player Host");

            hostStats = playerStats;
        }
        else
        {
            playerStats.skinType = SkinType.Duck;
            playerObj.name = $"Player ({playerStats.skinType})Client";
            Debug.Log("Spawn Player Client");

            clientStats = playerStats;
        }

        Debug.Log("Local player configured: " + playerObj.name);
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


public class ObjectTransform
{
    public Transform OldPosition;
    public Transform SpawnPosition;
}