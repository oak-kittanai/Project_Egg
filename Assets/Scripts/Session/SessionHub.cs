using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Collections.Unicode;

public enum characterType
{
    unknow,
    Duck,
    Bird
}

public class SessionHub : SingletonNetwork<SessionHub>
{
    [Header("SessionHub")]
    [SerializeField] TMP_Text _sessionKeyText;
    [SerializeField] string _sessionKey;

    [SerializeField] Button _leaveButton;
    [SerializeField] Button _leaveButton2;

    [SerializeField] string _inputRoomKey;

    [SerializeField] bool AlreadyJoin => SessionManager.Instance._isAlreadyInRoom;
    [SerializeField] int _playerCount;

    [SerializeField] Canvas _canvas;

    [Header("Lobby")]
    [SerializeField] Button _CreateSessionButton;
    [SerializeField] Button _joinSessionButton;

    [Header("JoinSession")]
    [SerializeField] GameObject JoinSession;
    [SerializeField] TMP_InputField _sessionNumberInsertField;

    [SerializeField] Button _JoinRoomButton;

    [Header("InLobby")]
    [SerializeField] GameObject LobbyGameObject;

    [SerializeField] GameObject _playerHost;
    [SerializeField] TMP_Text playerHostName;

    [SerializeField] GameObject _playerClient;
    [SerializeField] TMP_Text playerClientName;

    [SerializeField] TMP_Text CharacterNameText_h;
    [SerializeField] string CHT_right;
    [SerializeField] TMP_Text CharacterNameText_c;
    [SerializeField] string CHT_left;

    [SerializeField] string s_playerHostName;
    [SerializeField] string s_playerClientName;

    [SerializeField] characterType hostType;
    [SerializeField] characterType clientType;

    [SerializeField] Image CharacterHostIcon;
    [SerializeField] Image CharacterClientIcon;

    [SerializeField] string characterDuck = "Kael";
    [SerializeField] string characterBird = "Mira";

    [SerializeField] public Sprite _playerDuckImage;
    [SerializeField] public Sprite _playerBirdImage;
    [SerializeField] public Sprite unknownImage;

    [SerializeField] public Button ChangeCharacterButtonForHost;
    [SerializeField] public Button ChangeCharacterButtonForClient;

    [SerializeField] TMP_Text RoomCode;

    [SerializeField] Button _startButton;

    public void Setup()
    {
        _joinSessionButton.onClick.AddListener(JoinSessionRoom);
        _CreateSessionButton.onClick.AddListener(CreateRoom);
        _JoinRoomButton.onClick.AddListener(JoinRoom);

        _leaveButton.onClick.AddListener(LeaveRoom);
        _leaveButton2.onClick.AddListener(LeaveRoom);
        ChangeCharacterButtonForHost.onClick.AddListener(AddFromHost);
        ChangeCharacterButtonForClient.onClick.AddListener(AddFromClient);

        SetupAssets();
        JoinSession.SetActive(false);

        _CreateSessionButton.gameObject.SetActive(true);
        _joinSessionButton.gameObject.SetActive(true);
        LobbyGameObject.gameObject.SetActive(false);
    }

    public void SetupAssets()
    {
        /*Sprite Duck = Resources.Load<Sprite>("UI_Assets/SessionRoom/Profile/Kael_Profile.png");
        _playerDuckImage = Duck;

        Sprite Bird = Resources.Load<Sprite>("UI_Assets/SessionRoom/Profile/Mira_Profile.png");
        _playerBirdImage = Bird;

        Sprite unknow = Resources.Load<Sprite>("UI_Assets/SessionRoom/Profile/Unknow_Character.png");
        unknownImage = unknow;*/
    }

    public void SetDefault(NetworkRunner runner)
    {
        if (runner.IsServer)
        {
            hostType = characterType.unknow;
        }
        else
        {
            clientType = characterType.unknow;
        }
    }

    public void AddFromHost()
    {
        if (RuntimeUpdate.Instance != null)
        {
            RuntimeUpdate.Instance.ChangeTypeRequest_RPC(true);
        }
        else
        {
            Debug.Log("not ready");
        }
    }

    public void AddFromClient()
    {
        if (RuntimeUpdate.Instance != null)
        {
            RuntimeUpdate.Instance.ChangeTypeRequest_RPC(false);
        }
        else
        {
            Debug.Log("not ready");
        }
    }

    public void UpdateOverTime()
    {
        if (AlreadyJoin)
        {
            LobbyGameObject.SetActive(true);
            RoomCode.text = _sessionKey;
            JoinSession.SetActive(false);
        }
        else
        {
            LobbyGameObject.SetActive(false);
        }
    }

    public void UpdateCode(string code)
    {
        RoomCode.text = code;
        _sessionKey = code;
    }

    public void SetupButtonOnline(bool playerHost)
    {
        if (playerHost)
        {
            ChangeCharacterButtonForHost.interactable = true;
            ChangeCharacterButtonForClient.interactable = false;
        }
        else
        {
            ChangeCharacterButtonForHost.interactable = false;
            ChangeCharacterButtonForClient.interactable = true;
        }
    }

    public void UpdateTMPText(int playerNum)
    {
        if (HasStateAuthority)
        {
            CharacterNameText_h.text = CHT_left;
            CharacterNameText_c.text = CHT_right;
        }

        if (playerNum == 1)
        {
            s_playerHostName = "Player1";
            playerHostName.text = s_playerHostName;
        }

        if (playerNum == 2)
        {
            s_playerClientName = "Player2";
            playerClientName.text = s_playerClientName;
        }
    }

    public void ChangeType(int playerNum ,int o)
    {
        characterType newType = (characterType)o;

        if (playerNum == 1)
        {
            hostType = newType;
            UpdateIcon(1);
            Debug.Log("HostType = " + hostType);
        }
        else if (playerNum == 2)
        {
            clientType = newType;
            UpdateIcon(2);
            Debug.Log("ClientType = " + clientType);
        }
        else
        {
            Debug.Log("can't find playerNum");
        }
    }
    public void UpdateIcon(int playerNum)
    {
        if (playerNum == 1)
        {
            CharacterHostIcon.sprite = GetSpriteFromType(hostType);
        }
        else if (playerNum == 2)
        {
            CharacterClientIcon.sprite = GetSpriteFromType(clientType);
        }
    }

    private Sprite GetSpriteFromType(characterType type)
    {
        switch (type)
        {
            case characterType.Duck:
                return _playerDuckImage;

            case characterType.Bird:
                return _playerBirdImage;

            default:
                return unknownImage;
        }
    }

    public void SetMainButtonOff(bool o)
    {
        _joinSessionButton.gameObject.SetActive(o);
        _CreateSessionButton.gameObject.SetActive(o);
    }

    public void JoinSessionRoom()
    {
        JoinSession.SetActive(true);
        SetMainButtonOff(false);
    }

    public void JoinRoom()
    {
        if (_sessionNumberInsertField.text.Length < 6 || _sessionNumberInsertField.text.Length > 6)
        {
            _sessionNumberInsertField.text = "Wrong Session key";
        }
        else if (_sessionNumberInsertField.text.Length == 6)
        {
            string key = _sessionNumberInsertField.text;

            SessionManager.Instance.JoinSession(key);
        }
    }

    public void onDisconnected()
    {
        _startButton.gameObject.SetActive(false);
        LobbyGameObject.SetActive(false);
        SetMainButtonOff(true);
    }

    public void DoneJoin()
    {
        if (AlreadyJoin)
        {
            JoinSession.SetActive(false);
            _startButton.gameObject.SetActive(false);
            LobbyGameObject.SetActive(true);
            SetMainButtonOff(false);
        }
        else
        {
            _startButton.gameObject.SetActive(false);
        }
    }

    public void LeaveRoom()
    {
        if (AlreadyJoin)
        {
            SessionManager.Instance.LeaveSession(AlreadyJoin);
            SetMainButtonOff(true);
        }

        if (!AlreadyJoin)
        {
            _startButton.gameObject.SetActive(false);
        }

        if (!string.IsNullOrEmpty(_sessionKey))
        {
            _sessionNumberInsertField.text = "";
            _sessionNumberInsertField.interactable = true;
            _sessionKey = "";
            if (string.IsNullOrEmpty(_sessionKey))
            {
                LobbyGameObject.SetActive(false);
                _joinSessionButton.gameObject.SetActive(true);
                _CreateSessionButton.gameObject.SetActive(true);
            }
        }
    }

    public void GetKey(string key)
    {
        if (!string.IsNullOrEmpty(key))
        {
            _sessionKey = key;
            Debug.Log($"Create session Key : {_sessionKey}");
        }
        else
        {
            Debug.Log("Session key is : Null");
        }
    }

    public void CreateRoom()
    {
        SessionManager.Instance.GenerateCode();

        _joinSessionButton.gameObject.SetActive(false);
        _leaveButton.gameObject.SetActive(true);

        _startButton.gameObject.SetActive(true);
    }

    public void UpdateList(int playerCount)
    {
        _playerCount = playerCount;
    }

    public void StartTheGame()
    {
        SessionManager.Instance.StartGame();
    }

    private void OnDestroy()
    {
        DesetButton();
    }

    public void DesetButton()
    {
        _joinSessionButton.onClick.RemoveAllListeners();
        _CreateSessionButton.onClick.RemoveAllListeners();
        _JoinRoomButton.onClick.RemoveAllListeners();

        _leaveButton.onClick.RemoveAllListeners();
        _leaveButton2.onClick.RemoveAllListeners();
    }
}
