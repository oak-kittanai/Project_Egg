using Fusion;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TMPro;
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

    [Header("List")]
    [SerializeField] TMP_Text _listText;
    [SerializeField] GameObject _listGameObject;
    [SerializeField] GameObject _namePrefab;

    [SerializeField] Transform _listTransform;

    public NetworkRunner networkRunner;
    [SerializeField] NetworkObject CenterHostObject;

    [Header("StoreToSpawn")]
    public List<PlayersData> Players = new List<PlayersData>();

    [System.Serializable]
    public class PlayersData
    {
        public int playerNum;
        public NetworkRunner runner;
        public PlayerRef playerRef;
    }

    public async void StartGame()
    {
        // Load Scene First

        /*await LoadStartGame("Game");

        NetworkObject CHObject = networkRunner.Spawn(CenterHostObject);
        CenterHost CH = CHObject.GetComponent<CenterHost>();

        foreach (PlayersData player in Players)
        {
            NetworkRunner playerRun = player.runner;
            PlayerRef playerRef = player.playerRef;

            if (playerRef == playerRun.LocalPlayer)
            {
                CH.SpawnPlayer(playerRef);

            }
            else
            {
                CH.SpawnPlayer(playerRef);
            }
        }*/
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
                networkRunner = GameObject.FindGameObjectWithTag("runner").GetComponent<NetworkRunner>();
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


    public void JoinSession(string key)
    {
        JoinRoom(key);
        _isAlreadyInRoom = true;
    }

    public void LeaveSession(bool inroom)
    {
        if (inroom)
        {
            ReStartNetworkRunner();
            _isAlreadyInRoom = false;
        }
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

            await runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Host,
                SessionName = sessionKey,
                PlayerCount = 2
            });

            runTime = runner.Spawn(runtimeUpdate);
        }

    }

    public async void JoinRoom(string sessionKey)
    {
        var startArgs = new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = sessionKey
        };

        var res = await networkRunner.StartGame(startArgs);
        if (!res.Ok)
        {
            Debug.LogError($"JoinSession failed: {res.ShutdownReason}");
            return;
        }
        else if (res.Ok)
        {
            SessionHub.Instance.GetKey(_sessionKey);
            Debug.Log("Successfully joined");
        };
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
        if (runner.SessionInfo.PlayerCount == 0)
        {
            _listText.text = "";
        }
        else
        {
            _listText.text = $"Player list ({runner.SessionInfo.PlayerCount}/2)";
            if (runner.IsServer && runner.SessionInfo.PlayerCount == 2)
            {
                
            }
        }

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
    public void InstantiatePlayerList(string name)
    {
        Instantiate(_namePrefab, _listTransform);
        TMP_Text text = _namePrefab.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.text = name;
        }
    }

    public void ResetList()
    {
        foreach (GameObject item in _listTransform)
        {
            Destroy(item);
        }
    }
}