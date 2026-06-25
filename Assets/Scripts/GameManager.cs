using Fusion;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class ItemMapping
{
    public string itemName;
    public NetworkObject itemPrefab;
    public Sprite itemSprite;
}

[System.Serializable]
public class IconMapping
{
    public string iconName;
    public Sprite iconSprite;
}

public class GameManager : SingletonNetwork<GameManager>
{
    [SerializeField] GameObject canvasObject;
    [SerializeField] GameObject coreManagerObject;

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

    [Header("Pause")]
    [Networked] public NetworkBool IsPaused { get; set; }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void SetPause_RPC(bool pause)
    {
        IsPaused = pause;
    }

    [Networked] public bool gameOver { get; set; }
    [Networked] public int TeamBlueKeys { get; set; }
    [Networked] public int TeamOrangeKeys { get; set; }

    [SerializeField] float playerReadyTimeout = 15f; // รอ Client นานสุด 15 วิ
    [Networked] TickTimer PlayerReadyTimeoutTimer { get; set; }

    [Header("Item")]
    [SerializeField] private LayerMask dropBlockLayer;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(HandleSceneLoaded());
    }

    private System.Collections.IEnumerator HandleSceneLoaded()
    {
        yield return new WaitUntil(() => FindFirstObjectByType<LevelData>() != null);

        LevelData levelData = FindFirstObjectByType<LevelData>();
        SetupLevelData(levelData);
        Debug.Log("[GameManager] LevelData setup complete");
    }

    public void MapAllPlayersFinishedLoading()
    {
        if (!HasStateAuthority) return;

        int playerCount = Runner.ActivePlayers.Count();
        MapsLoadedCount += playerCount;
        Debug.Log($"[GameManager] MapAllPlayersFinishedLoading +{playerCount} → {MapsLoadedCount}");
        CheckMapLoading();
    }


    #region Network

    public void GetNetworkRunner(NetworkRunner networkRunner)
    {
        NetworkRunner = networkRunner;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (!IsGameReady && isLoadMapDone)
        {
            bool allReady = isPlayerReady;
            bool timedOut = PlayerReadyTimeoutTimer.Expired(Runner);

            if ((allReady || timedOut) && !LoadingSceneTimer.IsRunning)
            {
                if (timedOut && !allReady)
                    Debug.LogWarning("[GameManager] Player ready timeout! Starting game anyway.");

                LoadingSceneTimer = TickTimer.CreateFromSeconds(Runner, loadingSceneCooldown);
                PlayerReadyTimeoutTimer = TickTimer.None;
            }

            if (LoadingSceneTimer.Expired(Runner))
            {
                IsGameReady = true;
                LoadingSceneTimer = TickTimer.None;
                Debug.Log("[GameManager] Game Start!");
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
        allowCloseUI = false;

        if (data.SpawnPosition != null)
            UpdateRespawnPos(data.SpawnPosition.position);

        checkPoints = data.levelCheckPoints;

        if (HasStateAuthority)
        {
            loadingSceneCooldown = data.introClip != null ? 1f : 4f;
            PlayerReadyTimeoutTimer = TickTimer.CreateFromSeconds(Runner, playerReadyTimeout);
        }
    }

    public void SpawnPlayersForNewScene()
    {
        if (!HasStateAuthority) return;

        MovementCharacter[] existingPlayers = FindObjectsByType<MovementCharacter>(FindObjectsSortMode.None);
        if (existingPlayers.Length > 0)
        {
            Debug.Log("[GameManager] Players already exist. Skip spawning.");
            return;
        }

        Vector3 spawnPos = GetRespawnPosition();
        activePlayers.Clear();

        foreach (var playerRef in Runner.ActivePlayers)
        {
            bool isHost = (playerRef == Runner.LocalPlayer);
            characterType type = isHost
                ? CenterHost.Instance.currentHost
                : CenterHost.Instance.currentClient;

            NetworkObject prefabToSpawn = (type == characterType.Bird)
                ? CenterHost.Instance.BirdPrefab
                : CenterHost.Instance.DuckPrefab;

            Runner.Spawn(prefabToSpawn, spawnPos, Quaternion.identity, playerRef, (runner, obj) =>
            {
                CharacterStats playerStats = obj.GetComponent<CharacterStats>();
                if (playerStats != null) playerStats.skinType = type;
                obj.name = $"Spawned_{type}";
            });
        }
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
        Debug.Log($"[GameManager] CheckMapLoading — count: {MapsLoadedCount}/2, done: {isLoadMapDone}");
        if (MapsLoadedCount >= 2 && !isLoadMapDone)
        {
            isLoadMapDone = true;
            Debug.Log("[GameManager] Map Ready!");
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

        if (isPlayerReady && isLoadMapDone && !IsGameReady && !LoadingSceneTimer.IsRunning)
        {
            LoadingSceneTimer = TickTimer.CreateFromSeconds(Runner, loadingSceneCooldown);
            Debug.Log($"Both Ready! Starting Delay Timer {loadingSceneCooldown}");
        }
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

    public async void BackToSessionScene()
    {
        CleanupPersistentGameplayObjects();

        if (Runner != null)
        {
            Debug.Log("GameManager: Shutting down NetworkRunner...");
            await Runner.Shutdown();
        }

        SceneManager.LoadScene("SessionScene");
    }

    private void CleanupPersistentGameplayObjects()
    {
        if (CameraCharacter.LocalCamera != null)
            Destroy(CameraCharacter.LocalCamera.transform.root.gameObject);

        DestroyByName("CoreManagerSceneHop");
        DestroyByName("PlayerInterfaceCanvas");
        if (SessionManager.Instance != null)
            Destroy(SessionManager.Instance.gameObject);
    }

    private void DestroyByName(string objName)
    {
        GameObject obj = GameObject.Find(objName);
        if (obj != null) Destroy(obj);
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
            if (OrangeKeys) TeamOrangeKeys++;
            else TeamBlueKeys++;

            if (IsQuestActive && QuestIsBar) AddQuestProgress(1);
        }
        else RPC_RequestAddKey(OrangeKeys);
    }

    public void UseKey(bool OrangeKeys)
    {
        if (HasStateAuthority)
        {
            if (OrangeKeys) TeamOrangeKeys--;
            else TeamBlueKeys--;
        }
        else RPC_RequestUseKey(OrangeKeys);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestAddKey(bool OrangeKeys)
    {
        if (OrangeKeys) TeamOrangeKeys++; else TeamBlueKeys++;
        if (IsQuestActive && QuestIsBar) AddQuestProgress(1);
    }

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

    public NetworkObject ProjectileSpawn(NetworkObject objToSpawn, Vector2 posToSpawn, Vector2 direction, Quaternion rota, float speed)
    {
        if (!HasStateAuthority) return null;

        Vector3 spawnPos = new Vector3(posToSpawn.x, posToSpawn.y, 0f);

        NetworkObject spawnedObj = Runner.Spawn(objToSpawn, spawnPos, rota);

        Rigidbody2D rb = spawnedObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        return spawnedObj;
    }

    public void SpawnDropItem(NetworkObject objToSpawn, Vector2 posToSpawn)
    {
        if (!HasStateAuthority) return;

        NetworkObject objIte = Runner.Spawn(objToSpawn, posToSpawn);

        Rigidbody2D rb = objIte.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(Vector2.up * 0.5f, ForceMode2D.Impulse);
        }
    }

    [Header("Team Inventory")]
    [Networked] public NetworkBool TeamHasOrangeStone { get; set; }
    [Networked] public NetworkBool TeamHasBlueStone { get; set; }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestAddStone(bool isOrange)
    {
        if (isOrange) TeamHasOrangeStone = true;
        else TeamHasBlueStone = true;

        if (IsQuestActive) AddQuestProgress(1);
    }

    [Header("Item Database")]
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

    public Sprite GetItemSprite(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return null;

        foreach (var mapping in itemDatabase)
        {
            if (mapping.itemName == itemName)
                return mapping.itemSprite;
        }
        return null;
    }

    #endregion

    #region Quest

    [Header("Quest Icons")]
    public List<IconMapping> questIconDatabase = new List<IconMapping>();

    public Sprite GetQuestIcon(string iconName)
    {
        foreach (var mapping in questIconDatabase)
        {
            if (mapping.iconName == iconName) return mapping.iconSprite;
        }
        return null;
    }

    [Header("Global Quest System")]
    [Networked, OnChangedRender(nameof(OnQuestStateChanged))]
    public NetworkBool IsQuestActive { get; set; }

    [Networked, OnChangedRender(nameof(OnQuestStateChanged))]
    public NetworkBool QuestIsBar { get; set; }

    [Networked, OnChangedRender(nameof(OnQuestStateChanged))]
    public NetworkString<_32> QuestIconName { get; set; }

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
                Sprite icon = GetQuestIcon(QuestIconName.ToString());

                PlayerInterface.Instance.UpdateQuestUI(
                    QuestDescription.ToString(),
                    QuestCurrentProgress,
                    QuestMaxProgress,
                    QuestIsBar,
                    icon
                );
            }
            else
            {
                PlayerInterface.Instance.HideQuestUI();
            }
        }
    }

    public void StartGlobalQuest(string iconName, string desc, bool isbar, int maxProgress = 0)
    {
        if (HasStateAuthority)
        {
            QuestIconName = iconName;
            QuestDescription = desc;
            QuestIsBar = isbar;
            QuestMaxProgress = isbar ? maxProgress : 0;
            QuestCurrentProgress = 0;
            IsQuestActive = true;
        }
        else RPC_StartGlobalQuest(iconName, desc, isbar, maxProgress);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_StartGlobalQuest(NetworkString<_32> iconName, NetworkString<_64> desc, NetworkBool isbar, int maxProgress)
    {
        StartGlobalQuest(iconName.ToString(), desc.ToString(), isbar, maxProgress);
    }

    public void AddQuestProgress(int amount = 1)
    {
        if (HasStateAuthority)
        {
            if (!IsQuestActive) return;

            QuestCurrentProgress += amount;

            if (QuestIsBar && QuestCurrentProgress >= QuestMaxProgress)
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

    public void ResetQuest()
    {
        if (HasStateAuthority)
        {
            IsQuestActive = false;
            QuestIconName = "";
            QuestDescription = "";
            QuestIsBar = false;
            QuestMaxProgress = 0;
            QuestCurrentProgress = 0;

            Debug.Log("Quest has been reset!");
        }
        else
        {
            RPC_ResetQuest();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ResetQuest()
    {
        ResetQuest();
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
            PlayerReadyTimeoutTimer = TickTimer.None;

            activePlayers.Clear();
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

    #region Scene Transition (Level Hop)

    public async Task LoadNextLevel(string nextSceneName)
    {
        if (!HasStateAuthority) return;

        ShowGlobalLoadingScreen();
        ResetLoadingStateForNextLevel();

        await Runner.LoadScene(nextSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
    #endregion
    #endregion
    #region Menu Control

    public void RequestOpenMenu()
    {
        if (!HasStateAuthority) return;

        if (MenuController.Instance != null && MenuController.Instance.Object != null && MenuController.Instance.Object.IsValid)
        {
            if (MenuController.Instance.IsMenuOpen) return;

            MenuController.Instance.OpenMenuState();
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

            if (PlayerInterface.Instance != null)
                PlayerInterface.Instance.StopAllVideos();

            allowCloseUI = false;
        }
    }

    #region Skill Unlock System

    [Header("Skill Unlock State")]
    [Networked, OnChangedRender(nameof(OnSkillUnlockChanged))]
    public NetworkBool SkillBirdFlyUnlocked { get; set; }
    [Networked, OnChangedRender(nameof(OnSkillUnlockChanged))]
    public NetworkBool SkillBirdThrowUnlocked { get; set; }
    [Networked, OnChangedRender(nameof(OnSkillUnlockChanged))]
    public NetworkBool SkillDuckDiveUnlocked { get; set; }
    [Networked, OnChangedRender(nameof(OnSkillUnlockChanged))]
    public NetworkBool SkillDuckSmashUnlocked { get; set; }
    public void OnSkillUnlockChanged()
    {
        // sync static bools
        PlayerInterface._birdFlyUnlocked = SkillBirdFlyUnlocked;
        PlayerInterface._birdThrowUnlocked = SkillBirdThrowUnlocked;
        PlayerInterface._duckDiveUnlocked = SkillDuckDiveUnlocked;
        PlayerInterface._duckSmashUnlocked = SkillDuckSmashUnlocked;
    }

    public void UnlockSkill_BirdFly() => RequestUnlockSkill(0);
    public void UnlockSkill_BirdThrow() => RequestUnlockSkill(1);
    public void UnlockSkill_DuckDive() => RequestUnlockSkill(2);
    public void UnlockSkill_DuckSmash() => RequestUnlockSkill(3);

    private void RequestUnlockSkill(int skillIndex)
    {
        if (HasStateAuthority) SetSkillUnlocked(skillIndex);
        else RPC_UnlockSkill(skillIndex);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_UnlockSkill(int skillIndex) => SetSkillUnlocked(skillIndex);

    private void SetSkillUnlocked(int skillIndex)
    {
        switch (skillIndex)
        {
            case 0: SkillBirdFlyUnlocked = true; break;
            case 1: SkillBirdThrowUnlocked = true; break;
            case 2: SkillDuckDiveUnlocked = true; break;
            case 3: SkillDuckSmashUnlocked = true; break;
        }
    }

    public void ResetAllSkillUnlocks()
    {
        if (!HasStateAuthority) return;
        SkillBirdFlyUnlocked = SkillBirdThrowUnlocked = false;
        SkillDuckDiveUnlocked = SkillDuckSmashUnlocked = false;
    }

    #endregion

}

[System.Serializable]
public class CheckPoint
{
    public Vector3 spawnPointPos;
    public float currentMapProgress;
}