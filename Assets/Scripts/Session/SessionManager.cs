using Fusion;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionManager : SingletonNetwork<SessionManager>
{
    [Header("Session")]
    [SerializeField] string _sessionKey;
    [SerializeField] public bool _isAlreadyInRoom;

    [SerializeField] GameObject runnerPrefab;
    [NetworkPrefab] public NetworkObject runtimeUpdate;
    [SerializeField] NetworkObject runTime;

    [SerializeField] Transform _listTransform;

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

    [Header("GameManger")]
    [NetworkPrefab] public NetworkObject GameManagerPrefabs;
    [SerializeField] public NetworkObject GM;
    [SerializeField] public GameManager gameManager;

    public async void StartGame()
    {
        _isAlreadyInRoom = false;
        if (networkRunner.IsServer)
        {
            INetworkStructure networkStructure = networkRunner.GetComponent<INetworkStructure>();

            NetworkObject CHObject = networkRunner.Spawn(CenterHostObject);
            CenterHost CH = CHObject.GetComponent<CenterHost>();
            CH.AddComponent(networkRunner, networkStructure, PlayerPrefabs);

            await LoadStartGame("Stage1-S1"); // Load To Scene Beta Test
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

    public void SelfDestory()
    {
        Destroy(this.gameObject);
    }

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

    public async Task LoadStartGame(string sceneName)
    {
        await networkRunner.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public void Setup()
    {
        if (networkRunner == null)
        {
            try
            {
                ReStartNetworkRunner();
            }
            catch
            {
                ReStartNetworkRunner();
            }
        }
        else
        {
            ReStartNetworkRunner();
        }
    }


    #region SessionRoom

    public async void GenerateCode()
    {
        if (networkRunner == null)
        {
            Debug.LogError("can't find networkRunner");
        }
        else
        {
            _sessionKey = GenerateSessionCode();

            await StartSession(networkRunner, _sessionKey);

            _isAlreadyInRoom = true;
            if (!string.IsNullOrEmpty(_sessionKey))
            {

                SessionHub.Instance.GetKey(_sessionKey);
                Debug.Log($"Create session Key : {_sessionKey}");
            }
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

    public async Task StartSession(NetworkRunner runner, string sessionKey)
    {
        Debug.Log("Start session Successfully");
        if (runner == null)
        {
            Debug.Log("can't start seesion find runner");
            return;
        }
        else
        {
            _isAlreadyInRoom = true;

            var startSessionArgs = new StartGameArgs()
            {
                GameMode = GameMode.Host,
                SessionName = sessionKey,
                PlayerCount = 2
            };

            var res = await networkRunner.StartGame(startSessionArgs);
            if (!res.Ok)
            {
                SessionHub.Instance.ShowDebugText("Create Session Fail");
                Debug.LogError($"JoinSession failed: {res.ShutdownReason}");
                return;
            }
            else if (res.Ok)
            {
                SessionHub.Instance.ShowDebugText("Success Create Session");
            };
            
            runTime = runner.Spawn(runtimeUpdate);

            if (runTime != null)
            {
                RuntimeUpdate.Instance.UpdateCode(sessionKey);
            }

            shipType = runner.Spawn(shipTypePrefabs);
        }
    }

    public async void JoinRoom(string sessionKey)
    {
        if (string.IsNullOrEmpty(sessionKey))
        {
            Debug.LogError("Session Key is Empty!");
            return;
        }

        if (networkRunner == null) AddRunner();

        var startJoiningArgs = new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = sessionKey,
            SceneManager = networkRunner.GetComponent<NetworkSceneManagerDefault>()
        };

        var res = await networkRunner.StartGame(startJoiningArgs);

        if (!res.Ok)
        {
            Debug.LogError($"JoinSession failed: {res.ShutdownReason}");
            SessionHub.Instance.ShowDebugText($"Join Session Fail :{res.ShutdownReason}");
            return;
        }
        else if (res.Ok)
        {
            _sessionKey = sessionKey;
            SessionHub.Instance.ShowDebugText("Success Joining Session");
            SessionHub.Instance.DoneJoin();
            SessionHub.Instance.GetKey(_sessionKey);
            Debug.Log("Successfully joined");
        }
    }

    private string GenerateSessionCode(int length = 6)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var sb = new StringBuilder(length);
        var random = new System.Random();

        for (int i = 0; i < length; i++)
        {
            sb.Append(chars[random.Next(chars.Length)]);
        }

        return sb.ToString();
    }

    #endregion

    #region Runner

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
            networkRunner = null;
            Debug.Log("Runner shutdown complete");
        }

        if (runTime != null)
        {
            networkRunner.Despawn(runTime);
        }

        await Task.Delay(100);

        AddRunner();
    }

    #endregion


    public void UpdatePlayerCount(NetworkRunner runner)
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