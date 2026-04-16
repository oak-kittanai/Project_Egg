using Fusion;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.CullingGroup;

public enum SessionState
{
    MainMenu,
    Setting,
    SessionSelect,
    Join,
    CharacterSelect
}

public class SessionManager : SingletonNetwork<SessionManager>
{
    #region Variables & Data Structures
    [Header("Session State")]
    public SessionState currentState;
    public event Action<SessionState> OnStateChanged;

    [Header("Session")]
    [SerializeField] string _sessionKey;
    [SerializeField] public bool _isAlreadyInRoom;

    [SerializeField] GameObject runnerPrefab;
    [NetworkPrefab] public NetworkObject runtimeUpdate;
    [SerializeField] NetworkObject runTime;

    [NetworkPrefab] public NetworkObject shipTypePrefabs;
    [SerializeField] NetworkObject shipType;

    public NetworkRunner networkRunner;
    [NetworkPrefab] public NetworkObject CenterHostObject;
    [NetworkPrefab] public NetworkObject PlayerPrefabs;

    [Header("StoreToSpawn")]
    public List<PlayersData> Players = new List<PlayersData>();

    [System.Serializable]
    public class PlayersData
    {
        public int playerNum;
        public NetworkRunner runner;
        public PlayerRef playerRef;
    }

    [Header("GameManager")]
    [NetworkPrefab] public NetworkObject GameManagerPrefabs;
    [SerializeField] public NetworkObject GM;
    [SerializeField] public GameManager gameManager;

    [Header("UI System")]
    [SerializeField] public GameObject globalLoadingScreen;
    #endregion

    #region Initialization & Setup
    public void Setup()
    {
        if (networkRunner == null)
        {
            try { ReStartNetworkRunner(); }
            catch { ReStartNetworkRunner(); }
        }
        else
        {
            ReStartNetworkRunner();
        }
    }

    private void Start()
    {
        ChangeState(SessionState.MainMenu);
    }
    #endregion

    #region State & UI Management
    public void ChangeState(SessionState newState)
    {
        currentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    public void ShowLoadingScreen(bool show)
    {
        if (globalLoadingScreen != null)
        {
            globalLoadingScreen.SetActive(show);
        }
    }
    #endregion

    #region Player Management
    public PlayersData GetPlayerByNum(int playerNum)
    {
        return Players.Find(p => p.playerNum == playerNum);
    }

    public void RemovePlayer(int playerNum)
    {
        PlayersData players = GetPlayerByNum(playerNum);
        if (players != null)
        {
            Players.Remove(players);
            Debug.Log($"Remove player :{players.playerNum}");
        }
    }

    public void GetData(int userID, PlayerRef player, NetworkRunner runner)
    {
        PlayersData newPlayers = new PlayersData()
        {
            playerNum = userID,
            runner = runner,
            playerRef = player
        };

        Players.Add(newPlayers);
        Debug.Log($"Player added: {player.PlayerId} and add {runner}");
    }

    public void UpdatePlayerCount(NetworkRunner runner)
    {
        if (runner != null && runner.SessionInfo != null)
        {
            if (runner.IsServer && runner.SessionInfo.PlayerCount == 2)
            {
                runner.SessionInfo.IsOpen = false;
                runner.SessionInfo.IsVisible = false;
            }
            else
            {
                runner.SessionInfo.IsOpen = true;
                runner.SessionInfo.IsVisible = true;
            }
        }
    }
    #endregion

    #region Session & Room Flow
    public async void GenerateCode()
    {
        if (networkRunner == null)
        {
            Debug.LogError("can't find networkRunner");
            return;
        }

        // can add loading code

        _sessionKey = GenerateSessionCode();

        bool isSuccess = await StartSession(networkRunner, _sessionKey);

        ShowLoadingScreen(false);

        if (isSuccess)
        {
            _isAlreadyInRoom = true;
            if (!string.IsNullOrEmpty(_sessionKey))
            {
                SessionHub.Instance.GetKey(_sessionKey);
                Debug.Log($"Create session Key : {_sessionKey}");
            }

            ChangeState(SessionState.CharacterSelect);
        }
        else
        {
            SessionHub.Instance.ResetMenuButtons();
        }
    }

    public bool JoinSession(string key)
    {
        JoinRoom(key);
        _isAlreadyInRoom = true;
        return _isAlreadyInRoom;
    }

    public void LeaveSession(bool inroom)
    {
        if (inroom)
        {
            ReStartNetworkRunner();
            _isAlreadyInRoom = false;
        }
    }

    public void DisconnedFromServer()
    {
        _isAlreadyInRoom = false;
        SessionHub.Instance.onDisconnected();
    }

    public async Task<bool> StartSession(NetworkRunner runner, string sessionKey)
    {
        Debug.Log("Starting Host Session...");
        if (runner == null) return false;

        _isAlreadyInRoom = true;
        var sceneManager = runner.GetComponent<NetworkSceneManagerDefault>() ?? runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

        var startSessionArgs = new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = sessionKey,
            PlayerCount = 2,
            SceneManager = sceneManager
        };

        var res = await runner.StartGame(startSessionArgs);

        if (!res.Ok)
        {
            SessionHub.Instance.ShowDebugText("Create Session Fail");
            Debug.LogError($"StartSession failed: {res.ShutdownReason}");
            _isAlreadyInRoom = false;
            return false;
        }

        SessionHub.Instance.ShowDebugText("Success Create Session");
        Debug.Log("Start session Successfully");

        runTime = runner.Spawn(runtimeUpdate);
        if (runTime != null && runTime.TryGetComponent<RuntimeUpdate>(out var rtUpdate))
        {
            rtUpdate.UpdateCode(sessionKey);
        }
        shipType = runner.Spawn(shipTypePrefabs);

        return true;
    }

    public async void JoinRoom(string sessionKey)
    {
        if (string.IsNullOrEmpty(sessionKey))
        {
            Debug.LogError("Session Key is Empty!");
            return;
        }

        if (networkRunner == null) AddRunner();

        if (GlobalLoadingManager.Instance != null) GlobalLoadingManager.Instance.ShowLoading();
        ShowLoadingScreen(true);

        var sceneManager = networkRunner.GetComponent<NetworkSceneManagerDefault>();
        if (sceneManager == null)
        {
            sceneManager = networkRunner.gameObject.AddComponent<NetworkSceneManagerDefault>();
        }

        var startJoiningArgs = new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = sessionKey,
            SceneManager = sceneManager
        };

        var res = await networkRunner.StartGame(startJoiningArgs);

        if (!res.Ok)
        {
            Debug.LogError($"JoinSession failed: {res.ShutdownReason}");
            SessionHub.Instance.ShowDebugText($"Join Session Fail: {res.ShutdownReason}");
            SessionHub.Instance.OnJoinFailed();
            ShowLoadingScreen(false);
            _isAlreadyInRoom = false;
            return;
        }

        _sessionKey = sessionKey;
        _isAlreadyInRoom = true;
        SessionHub.Instance.ShowDebugText("Success Joining Session");
        SessionHub.Instance.DoneJoin();
        SessionHub.Instance.GetKey(_sessionKey);
        Debug.Log("Successfully joined");
    }

    private string GenerateSessionCode(int length = 6)
    {
        const string chars = "0123456789";
        var sb = new StringBuilder(length);
        var random = new System.Random();

        for (int i = 0; i < length; i++)
        {
            sb.Append(chars[random.Next(chars.Length)]);
        }

        return sb.ToString();
    }
    #endregion

    #region Core Game Loop
    public async Task LoadStartGame(string sceneName)
    {
        await networkRunner.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public async void StartGame()
    {
        _isAlreadyInRoom = false;

        ShowLoadingScreen(true);

        if (GlobalLoadingManager.Instance != null) GlobalLoadingManager.Instance.ShowLoading();

        if (networkRunner.IsServer)
        {
            INetworkStructure networkStructure = networkRunner.GetComponent<INetworkStructure>();

            NetworkObject CHObject = networkRunner.Spawn(CenterHostObject);
            CenterHost CH = CHObject.GetComponent<CenterHost>();
            CH.AddComponent(networkRunner, networkStructure, PlayerPrefabs);

            await LoadStartGame("Stage1-S1");
            _isAlreadyInRoom = false;

            GM = networkRunner.Spawn(GameManagerPrefabs);
            gameManager = GM.GetComponent<GameManager>();
            gameManager.GetNetworkRunner(networkRunner);

            foreach (PlayersData player in Players)
            {
                NetworkRunner playerRun = player.runner;
                PlayerRef playerRef = player.playerRef;

                if (RuntimeUpdate.Instance != null)
                {
                    if (playerRef == playerRun.LocalPlayer)
                    {
                        CH.SpawnPlayer(playerRef, CharacterTypeShip.Instance.currentHost, true);
                    }
                    else
                    {
                        CH.SpawnPlayer(playerRef, CharacterTypeShip.Instance.currentClient, false);
                    }
                }
                else
                {
                    Debug.Log("runtime is null");
                }
            }
        }
        SessionHub.Instance.DesetButton();
    }
    #endregion

    #region Network Runner Management
    public void AddRunner()
    {
        Debug.Log("Add runner");
        networkRunner = Instantiate(runnerPrefab).GetComponent<NetworkRunner>();
        INetworkStructure structure = networkRunner.GetComponent<INetworkStructure>();
        SessionHub.Instance.networkRunner = networkRunner.GetComponent<NetworkRunner>();
        if (structure != null)
        {
            networkRunner.AddCallbacks(structure);
            Debug.Log("success add Callbacks");
        }
        else Debug.Log("can't find INetworkStructure");

        if (runnerPrefab == null)
        {
            Debug.LogError("can't find runner Prefab");
        }
        else
        {
            Debug.Log("create runner");
        }
    }

    public async void ReStartNetworkRunner()
    {
        Debug.Log("StartRestart runner");
        if (networkRunner != null)
        {
            Debug.Log("Shutting down runner...");
            await networkRunner.Shutdown();

            if (networkRunner.gameObject != null)
            {
                Destroy(networkRunner.gameObject);
            }

            networkRunner = null;
            Debug.Log("Runner shutdown complete");
        }

        runTime = null;
        shipType = null;
        GM = null;
        Players.Clear();

        await Task.Delay(200);

        AddRunner();
    }
    #endregion

    #region Utility
    public void SelfDestory()
    {
        Destroy(this.gameObject);
    }
    #endregion
}