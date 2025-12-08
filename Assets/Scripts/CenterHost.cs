using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("Check&Spawn")]
    [SerializeField] bool doneScan;
    [SerializeField] bool readySpawn;
    [SerializeField] bool firstStart;
    [SerializeField] public List<ObjectTransform> ObjMoveAbleTrans = new List<ObjectTransform>();
    [SerializeField] public List<ObjectTransform> ObjTrapTrans = new List<ObjectTransform>();
    [SerializeField] public List<ObjectTransform> ObjEnemyTrans = new List<ObjectTransform>();

    [SerializeField] bool RemoveOldObj;

    [Header("Object")]
    [Networked] NetworkObject RockPrefabs { get; set; }
    [Networked] NetworkObject JellyPrefabs { get; set; }
    [Networked] NetworkObject BearTrapPrefabs { get; set; }
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

        if (HasStateAuthority)
        {
            firstStart = true;
            StartCoroutine(StartCheckAndAdd());
        }

        if (!RemoveOldObj & !HasStateAuthority)
        {
            DestroyAllObjectOfflien();
            Destroy(this);
        }
    }

    IEnumerator StartCheckAndAdd()
    {
        // Check The game for the object and then respawn them again
        GameObject[] targetMoveAbleObject = GameObject.FindGameObjectsWithTag("MoveAble");

        foreach (GameObject obj in targetMoveAbleObject)
        {
            NetworkObject netObj = obj.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                var Objecttransform = new ObjectTransform()
                {
                    obj = netObj,
                    oldObj = obj,
                    position = new Vector2(netObj.transform.position.x, netObj.transform.position.y),
                    rotation = netObj.transform.rotation
                };

                ObjMoveAbleTrans.Add(Objecttransform);
            }
            else
            {
                Debug.Log(obj.name + " not network object");
            }
        }
        bool firstLoop = true;

        GameObject[] targetTrapObject = GameObject.FindGameObjectsWithTag("Trap");
        foreach (GameObject obj in targetTrapObject)
        {
            NetworkObject netObj = obj.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                var Objecttransform = new ObjectTransform()
                {
                    obj = netObj,
                    oldObj = obj,
                    position = new Vector2(netObj.transform.position.x, netObj.transform.position.y),
                    rotation = netObj.transform.rotation
                };

                ObjTrapTrans.Add(Objecttransform);
            }
            else
            {
                Debug.Log(obj.name + " not network object");
            }
        }
        bool secondLoop = true;

        GameObject[] targetEnemyObject = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject obj in targetEnemyObject)
        {
            NetworkObject netObj = obj.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                var Objecttransform = new ObjectTransform()
                {
                    obj = netObj,
                    oldObj = obj,
                    position = new Vector2(netObj.transform.position.x, netObj.transform.position.y),
                    rotation = netObj.transform.rotation
                };

                ObjEnemyTrans.Add(Objecttransform);
            }
            else
            {
                Debug.Log(obj.name + " not network object");
            }
        }
        bool thirdLoop = true;

        if (firstLoop && secondLoop && thirdLoop)
        {
            doneScan = true;
        }

        yield return new WaitUntil(() => doneScan == true);

    }

    public void DestroyAllObjectOfflien()
    {
        GameObject[] targetMoveAbleObject = GameObject.FindGameObjectsWithTag("MoveAble");

        foreach (GameObject obj in targetMoveAbleObject)
        {
            Destroy(obj);
        }

        GameObject[] targetTrapObject = GameObject.FindGameObjectsWithTag("Trap");
        foreach (GameObject obj in targetTrapObject)
        {
            Destroy(obj);
        }

        GameObject[] targetEnemyObject = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject obj in targetEnemyObject)
        {
            Destroy(obj);
        }
        RemoveOldObj = true;
    }

    public void CheckForTrapAndMoveAbleObject()
    {
        if (doneScan)
        {
            bool firstLoad;
            bool secondLoad;
            bool thirdLoad;
            if (readySpawn && Runner != null && HasStateAuthority)
            {
                Debug.Log("Start Spawn");
                foreach (ObjectTransform obj in ObjMoveAbleTrans)
                {
                    SpawnNetworkObject(obj.obj, obj.position, obj.rotation);
                    RemoveOldObject(obj.oldObj);
                }
                firstLoad = true;

                foreach (ObjectTransform obj in ObjTrapTrans)
                {
                    SpawnNetworkObject(obj.obj, obj.position, obj.rotation);
                    RemoveOldObject(obj.oldObj);
                }
                secondLoad = true;

                foreach (ObjectTransform obj in ObjEnemyTrans)
                {
                    SpawnNetworkObject(obj.obj, obj.position, obj.rotation);
                    RemoveOldObject(obj.oldObj);
                }
                thirdLoad = true;

                if (firstLoad && secondLoad && thirdLoad)
                {
                    Debug.Log("Success check & spawn item");
                }
            }
        }
    }

    public void SpawnNetworkObject(NetworkObject obj, Vector2 pos, Quaternion rotation)
    {
        if (Runner != null) // Make it sreach for prefabs before spawn
        {
            Vector3 spawnPos = new Vector3(pos.x, pos.y, 0f);
            Quaternion spawnRot = rotation;
            Runner.Spawn(obj, spawnPos, spawnRot);
        }
    }

    public void RemoveOldObject(GameObject obj)
    {
        Destroy(obj);
    }

    public override void FixedUpdateNetwork()
    {
        if (Runner.SessionInfo.PlayerCount == 2)
        {
            if (firstStart)
            {
                StartCoroutine(WaitForLoad());
            }
        }
    }

    IEnumerator WaitForLoad()
    {
        firstStart = false;
        yield return new WaitForSeconds(5f);
        readySpawn = true;
        CheckForTrapAndMoveAbleObject();
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

[Serializable]
public class ObjectTransform
{
    public NetworkObject obj;
    public GameObject oldObj;
    public Vector2 position;
    public Quaternion rotation;
}