using Fusion;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class ItemMapping
{
    public string itemName;
    public NetworkObject itemPrefab;
}

public class GameManager : SingletonNetwork<GameManager>
{
    [SerializeField] NetworkRunner NetworkRunner;

    [SerializeField] GameObject playerHost;
    [SerializeField] GameObject playerClient;

    [Networked] public Vector3 respawnPos { get; set; }

    [Header("Game Setting")]
    [Networked] public int MapsLoadedCount { get; set; }
    [Networked] public NetworkBool isPlayerReady { get; set; }
    [Networked] public NetworkBool isLoadMapDone { get; set; }
    [Networked] public NetworkBool IsGameReady { get; set; }
    [Networked] public int PlayersReadyCount { get; set; }

    [Header("Time Delay")]
    [Networked] TickTimer LoadingSceneTimer { get; set; }
    [SerializeField] float loadingSceneCooldown = 4f;

    // Loading scene
    private GameObject currentLoadingUI;
    private bool allowCloseUI = false;

    [Networked] public bool gameOver { get; set; }
    [Networked] public int TeamBlueKeys { get; set; }
    [Networked] public int TeamOrangeKeys { get; set; }


    public override void Spawned()
    {
        base.Spawned();
        DontDestroyOnLoad(gameObject);
    }

    #region Network

    public void GetNetworkRunner(NetworkRunner networkRunner)
    {
        NetworkRunner = networkRunner;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (!IsGameReady && isPlayerReady && isLoadMapDone)
        {
            if (LoadingSceneTimer.Expired(Runner))
            {
                IsGameReady = true;
                LoadingSceneTimer = TickTimer.None;
                Debug.Log("Game Start!");

                ResetAllPlayersToSpawn();
            }
        }
    }

    #region PlayerData

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
        currentLoadingUI = data.loadingScreenUI;
        if (currentLoadingUI != null) currentLoadingUI.SetActive(true);

        allowCloseUI = false;

        if (data.SpawnPosition != null)
        {
            UpdateRespawnPos(data.SpawnPosition.position);
        }
        checkPoints = data.levelCheckPoints;
    }

    #endregion
    #endregion

    #region InGameConfig

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
        // --- TEST CODE --- for sample scene
        /*if (SceneManager.GetActiveScene().name == "SampleScene" && !isLoadMapDone && !isPlayerReady)
        {
            if (!isLoadMapDone) isLoadMapDone = true;
            if (!isPlayerReady) isPlayerReady = true;
        }*/

        if (MapsLoadedCount >= 2 && !isLoadMapDone)
        {
            isLoadMapDone = true;
            Debug.Log("Map Ready");
            CheckGameStart();

            CheckMapLoading();
        }
    }

    private void CheckGameStart()
    {
        if (PlayersReadyCount >= 2 && !isPlayerReady)
        {
            isPlayerReady = true;
            Debug.Log("Player Ready");
        }

        if (isPlayerReady && isLoadMapDone && !IsGameReady && !LoadingSceneTimer.IsRunning)
        {
            LoadingSceneTimer = TickTimer.CreateFromSeconds(Runner, loadingSceneCooldown);
            Debug.Log($"Both Ready! Starting Delay Timer {loadingSceneCooldown}");
        }

        // Quest For Test
        StartGlobalQuest("Find an exit", 1);
    }

    public void ResetAllPlayersToSpawn()
    {
        if (HasStateAuthority)
        {
            Debug.Log("Try Resetting all players");

            MovementCharacter[] allPlayers = FindObjectsByType<MovementCharacter>(FindObjectsSortMode.None);

            foreach (var player in allPlayers)
            {
                if (player.Object != null && player.Object.IsValid)
                {
                    player.Respawn();
                }
            }
        }
    }

    public void BackToSessionScene()
    {
        if (Runner != null)
        {
            Runner.Shutdown();
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("SessionScene");
    }

    #region GameSetting

    public Vector3 GetRespawnPosition()
    {
        return respawnPos;
    }

    public void UpdateRespawnPos(Vector3 newPos)
    {
        if (HasStateAuthority)
        {
            respawnPos = newPos;
            Debug.Log($"Checkpoint try to update new Pos: {newPos}");
        }
        else
        {
            RPC_UpdateRespawnPos(newPos);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_UpdateRespawnPos(Vector3 newPos)
    {
        respawnPos = newPos;
        Debug.Log($"Host confirm Checkpoint update new Pos: {newPos}");
    }

    #endregion
    #endregion

    #region ItemSetting

    // Key
    public void AddKey(bool OrangeKeys)
    {
        if (HasStateAuthority)
        {
            if (OrangeKeys)
            {
                TeamOrangeKeys++;
            }
            else
                TeamBlueKeys++;
        }
        else RPC_RequestAddKey(OrangeKeys);
    }

    public void UseKey(bool OrangeKeys)
    {
        if (HasStateAuthority)
        {
            if (OrangeKeys)
            {
                TeamOrangeKeys--;
            }
            else
                TeamBlueKeys--;
        }
        else RPC_RequestUseKey(OrangeKeys);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestAddKey(bool OrangeKeys) { if (OrangeKeys) TeamOrangeKeys++; else TeamBlueKeys++; }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestUseKey(bool OrangeKeys) { if (OrangeKeys) TeamOrangeKeys--; else TeamBlueKeys--; }

    public void RequestDespawn(NetworkObject objToDespawn)
    {
        if (objToDespawn == null) return;

        if (HasStateAuthority)
        {
            Runner.Despawn(objToDespawn);
        }
        else
        {
            RPC_Despawn(objToDespawn.Id);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_Despawn(NetworkId objId)
    {
        if (Runner.TryFindObject(objId, out var obj))
        {
            Runner.Despawn(obj);
        }
    }

    public void ProjectileSpawn(NetworkObject objToSpawn, Vector2 posToSpawn, Vector2 direction, Quaternion rota, float speed)
    {
        if (!HasStateAuthority) return;

        Vector3 spawnPos = new Vector3(posToSpawn.x, posToSpawn.y, 0f);

        NetworkObject spawnedObj = Runner.Spawn(objToSpawn, spawnPos, rota);

        Rigidbody2D rb = spawnedObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
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

    [Header("Item Database Settings")]
    [SerializeField] public List<ItemMapping> itemDatabase = new List<ItemMapping>();

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_DropItemByName(string itemName, Vector2 dropPosition, MovementCharacter player)
    {
        if (string.IsNullOrEmpty(itemName) || player == null) return;

        NetworkObject prefabToSpawn = null;

        foreach (var mapping in itemDatabase)
        {
            if (mapping.itemName == itemName)
            {
                prefabToSpawn = mapping.itemPrefab;
                break;
            }
        }

        if (prefabToSpawn != null)
        {
            SpawnDropItem(prefabToSpawn, dropPosition);
            Debug.Log($"[GameManager] Successfully spawned dropped item: {itemName}");
        }
        else
        {
            Debug.LogError($"[GameManager] Cannot find prefab for item name: {itemName} in Database!");
        }

        if (player.Object != null && player.Object.IsValid)
        {
            player.HeldItemName = "";
        }
    }

    #endregion

    #region Quest

    [Header("Global Quest System")]
    [Networked, OnChangedRender(nameof(OnQuestStateChanged))]
    public NetworkBool IsQuestActive { get; set; }

    [Networked, OnChangedRender(nameof(OnQuestStateChanged))]
    public int QuestCurrentProgress { get; set; }

    [Networked, OnChangedRender(nameof(OnQuestStateChanged))]
    public int QuestMaxProgress { get; set; }

    [Networked, OnChangedRender(nameof(OnQuestStateChanged))]
    public NetworkString<_64> QuestDescription { get; set; }
    public void OnQuestStateChanged()
    {
        if (PlayerInterface.Instance != null)
        {
            if (IsQuestActive)
            {
                PlayerInterface.Instance.UpdateQuestUI(QuestDescription.ToString(), QuestCurrentProgress, QuestMaxProgress);
            }
            else
            {
                PlayerInterface.Instance.HideQuestUI();
            }
        }
    }

    public void StartGlobalQuest(string desc, int maxProgress)
    {
        if (HasStateAuthority)
        {
            QuestDescription = desc;
            QuestMaxProgress = maxProgress;
            QuestCurrentProgress = 0;
            IsQuestActive = true;
        }
        else RPC_StartGlobalQuest(desc, maxProgress);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_StartGlobalQuest(NetworkString<_64> desc, int maxProgress)
    {
        StartGlobalQuest(desc.ToString(), maxProgress);
    }

    public void AddQuestProgress(int amount = 1)
    {
        if (HasStateAuthority)
        {
            if (!IsQuestActive) return;

            QuestCurrentProgress += amount;

            if (QuestCurrentProgress >= QuestMaxProgress)
            {
                IsQuestActive = false;
                Debug.Log("Quest Completed!");
            }
        }
        else RPC_AddQuestProgress(amount);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_AddQuestProgress(int amount)
    {
        AddQuestProgress(amount);
    }

    #endregion

    #region Scene

    // Reset Loading State For Next Level
    public void ResetLoadingStateForNextLevel()
    {
        if (HasStateAuthority)
        {
            MapsLoadedCount = 0;
            PlayersReadyCount = 0;
            isPlayerReady = false;
            isLoadMapDone = false;
            IsGameReady = false;
            LoadingSceneTimer = TickTimer.None;
        }

        currentLoadingUI = null;
    }

    // Loading Screen Zone
    public void ShowGlobalLoadingScreen()
    {
        if (GlobalLoadingManager.Instance != null)
        {
            GlobalLoadingManager.Instance.ShowLoading();
        }
    }

    public void HideGlobalLoadingScreen()
    {
        if (GlobalLoadingManager.Instance != null)
        {
            GlobalLoadingManager.Instance.HideLoading();
        }
    }

    #endregion


    public override void Render()
    {
        if (!IsGameReady)
        {
            allowCloseUI = true;
        }

        if (allowCloseUI && IsGameReady)
        {
            HideGlobalLoadingScreen();

            if (currentLoadingUI != null && currentLoadingUI.activeSelf)
            {
                currentLoadingUI.SetActive(false);
            }

            allowCloseUI = false;
        }
    }

}

[System.Serializable]
public class CheckPoint
{
    public Vector3 spawnPointPos;
    public float currentMapProgress;
}