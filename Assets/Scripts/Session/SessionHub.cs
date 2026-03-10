using Fusion;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionHub : SingletonNetwork<SessionHub>
{
    [SerializeField] public NetworkRunner networkRunner;

    [Header("SessionHub")]
    [SerializeField] TMP_Text _sessionKeyText;
    [SerializeField] Button _clipBoardCopy;
    [SerializeField] string _sessionKey;

    [SerializeField] Button _leaveButton;
    [SerializeField] Button _leaveButton2;

    [SerializeField] string _inputRoomKey;

    [SerializeField] bool AlreadyJoin => SessionManager.Instance._isAlreadyInRoom;
    [SerializeField] int _playerCount;
    [SerializeField] bool isReady;

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
    [SerializeField] TMP_Text CharacterNameText_c;

    [SerializeField] string s_playerHostName;
    [SerializeField] string s_playerClientName;

    [SerializeField] public characterType hostType;
    [SerializeField] public characterType clientType;

    [SerializeField] Image CharacterHostIcon;
    [SerializeField] Image CharacterClientIcon;

    [SerializeField] string characterDuck = "Kael";
    [SerializeField] string characterBird = "Mira";

    [SerializeField] public Sprite _playerDuckImage;
    [SerializeField] public Sprite _playerBirdImage;
    [SerializeField] public Sprite unknownImage;

    [SerializeField] public Button ChangeCharacterButtonForHost;
    [SerializeField] public Button ChangeCharacterButtonForHost2;
    [SerializeField] public Button ChangeCharacterButtonForClient;
    [SerializeField] public Button ChangeCharacterButtonForClient2;

    [SerializeField] TMP_Text RoomCode;

    [SerializeField] Button _startButton;
    [SerializeField] Button _readyButton;

    [Header("Support_Text")]
    [SerializeField] GameObject DebugTextObj;
    [SerializeField] TMP_Text DebugText;

    public void Setup()
    {
        _joinSessionButton.onClick.AddListener(JoinSessionRoom);
        _CreateSessionButton.onClick.AddListener(CreateRoom);
        _JoinRoomButton.onClick.AddListener(JoinRoom);

        _leaveButton.onClick.AddListener(LeaveRoom);
        _leaveButton2.onClick.AddListener(LeaveRoom);
        ChangeCharacterButtonForHost.onClick.AddListener(AddFromHost);
        ChangeCharacterButtonForClient.onClick.AddListener(AddFromClient);
        ChangeCharacterButtonForHost2.onClick.AddListener(AddFromHost);
        ChangeCharacterButtonForClient2.onClick.AddListener(AddFromClient);

        _clipBoardCopy.onClick.AddListener(CopyKeyToClipboard);

        _startButton.onClick.AddListener(StartTheGame);
        _startButton.interactable = false;

        JoinSession.SetActive(false);

        _CreateSessionButton.gameObject.SetActive(true);
        _joinSessionButton.gameObject.SetActive(true);
        LobbyGameObject.gameObject.SetActive(false);

        DebugTextObj.SetActive(false);
    }

    public void StartTheGame()
    {
        CharacterTypeShip.Instance.UpdateType(hostType, true);
        CharacterTypeShip.Instance.UpdateType(clientType, false);
        DesetButton();
        SessionManager.Instance.StartGame();
    }

    public void ReadyToPlay()
    {
        if (networkRunner.IsServer)
        {
            if (_startButton != null)
            {
                if (isReady)
                {
                    _startButton.gameObject.SetActive(true);

                    _startButton.interactable = true;
                }
                else
                {
                    _startButton.interactable = false;
                }
            }
        }
    }

    public void CopyKeyToClipboard()
    {
        if (AlreadyJoin)
        {
            GUIUtility.systemCopyBuffer = _sessionKey;

            Debug.Log($"Copied to clipboard: {_sessionKey}");
        }
        else
        {
            Debug.Log("No code to copy");
        }
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

    public void ShowDebugText(string text)
    {
        DebugTextObj.SetActive(true);
        DebugText.text = text;
        StartCoroutine(ShowDebugTextTime());
    }

    IEnumerator ShowDebugTextTime()
    {
        yield return new WaitForSeconds(3);
        DebugText.text = null;
        DebugTextObj.SetActive(false);
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
            if (LobbyGameObject != null)
            {
                LobbyGameObject.SetActive(false);
            }
        }

        CheckCurrentType();
        ReadyToPlay();
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

            ChangeCharacterButtonForHost2.interactable = true;
            ChangeCharacterButtonForClient2.interactable = false;
        }
        else
        {
            ChangeCharacterButtonForHost.interactable = false;
            ChangeCharacterButtonForClient.interactable = true;

            ChangeCharacterButtonForHost2.interactable = false;
            ChangeCharacterButtonForClient2.interactable = true;
        }
    }

    public void UpdateTMPText(int playerNum)
    {
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

    public void CheckCurrentType()
    {
        if (networkRunner.IsServer)
        {
            if (hostType != characterType.unknow && clientType != characterType.unknow)
            {
                if (hostType != clientType)
                {
                    isReady = true;
                }
                else
                {
                    isReady = false;
                }
            }
        }
    }

    public void ChangeType(bool isHost, int o)
    {
        characterType newType = (characterType)o;

        if (isHost)
        {
            hostType = newType;
            UpdateIcon(true);
            switch (hostType)
            {
                case characterType.unknow:
                    CharacterNameText_h.text = "???";
                    break;
                case characterType.Duck:
                    CharacterNameText_h.text = characterDuck;
                    break;
                case characterType.Bird:
                    CharacterNameText_h.text = characterBird;
                    break;
            }
            Debug.Log("HostType = " + hostType);
        }
        else
        {
            clientType = newType;
            UpdateIcon(false);
            switch (clientType)
            {
                case characterType.unknow:
                    CharacterNameText_c.text = "???";
                    break;
                case characterType.Duck:
                    CharacterNameText_c.text = characterDuck;
                    break;
                case characterType.Bird:
                    CharacterNameText_c.text = characterBird;
                    break;
            }
            Debug.Log("ClientType = " + clientType);
        }
        RuntimeUpdate.Instance.UpdateType_RPC();
    }
    public void UpdateIcon(bool isHost)
    {
        if (isHost)
        {
            CharacterHostIcon.sprite = GetSpriteFromType(hostType);
        }

        if (!isHost)
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
        _JoinRoomButton.interactable = true;
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

            _JoinRoomButton.interactable = SessionManager.Instance.JoinSession(key);
        }
    }

    public void onDisconnected()
    {
        if (_startButton != null)
        {
            _startButton.gameObject.SetActive(false);
        }
        if (LobbyGameObject != null)
        {
            LobbyGameObject.SetActive(false);
            SetMainButtonOff(true);
        }
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
            JoinSession.SetActive(false);
            LobbyGameObject.SetActive(false);
            SetMainButtonOff(true);
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

    public void DesetButton()
    {
        _joinSessionButton.onClick.RemoveAllListeners();
        _CreateSessionButton.onClick.RemoveAllListeners();
        _JoinRoomButton.onClick.RemoveAllListeners();
        _clipBoardCopy.onClick.RemoveAllListeners();
        _leaveButton.onClick.RemoveAllListeners();
        _leaveButton2.onClick.RemoveAllListeners();
        _startButton.onClick.RemoveAllListeners();

        StopAllCoroutines();
    }
}
