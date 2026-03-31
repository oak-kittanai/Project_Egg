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
        if (_joinSessionButton != null) _joinSessionButton.onClick.AddListener(JoinSessionRoom);
        if (_CreateSessionButton != null) _CreateSessionButton.onClick.AddListener(CreateRoom);
        if (_JoinRoomButton != null) _JoinRoomButton.onClick.AddListener(JoinRoom);

        if (_leaveButton != null) _leaveButton.onClick.AddListener(LeaveRoom);
        if (_leaveButton2 != null) _leaveButton2.onClick.AddListener(LeaveRoom);

        if (ChangeCharacterButtonForHost != null) ChangeCharacterButtonForHost.onClick.AddListener(AddFromHost);
        if (ChangeCharacterButtonForClient != null) ChangeCharacterButtonForClient.onClick.AddListener(AddFromClient);
        if (ChangeCharacterButtonForHost2 != null) ChangeCharacterButtonForHost2.onClick.AddListener(AddFromHost);
        if (ChangeCharacterButtonForClient2 != null) ChangeCharacterButtonForClient2.onClick.AddListener(AddFromClient);

        if (_clipBoardCopy != null) _clipBoardCopy.onClick.AddListener(CopyKeyToClipboard);

        if (_startButton != null)
        {
            _startButton.onClick.AddListener(StartTheGame);
            _startButton.interactable = false;
        }

        if (JoinSession != null) JoinSession.SetActive(false);
        if (_CreateSessionButton != null) _CreateSessionButton.gameObject.SetActive(true);
        if (_joinSessionButton != null) _joinSessionButton.gameObject.SetActive(true);
        if (LobbyGameObject != null) LobbyGameObject.gameObject.SetActive(false);
        if (DebugTextObj != null) DebugTextObj.SetActive(false);
    }

    public void StartTheGame()
    {
        if (CharacterTypeShip.Instance != null)
        {
            CharacterTypeShip.Instance.UpdateType(hostType, true);
            CharacterTypeShip.Instance.UpdateType(clientType, false);
        }
        DesetButton();
        if (SessionManager.Instance != null) SessionManager.Instance.StartGame();
    }

    public void ReadyToPlay()
    {
        if (networkRunner != null && networkRunner.IsServer)
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
        if (runner == null) return;

        if (runner.IsServer) hostType = characterType.unknow;
        else clientType = characterType.unknow;
    }

    public void AddFromHost()
    {
        if (RuntimeUpdate.Instance != null) RuntimeUpdate.Instance.ChangeTypeRequest_RPC(true);
        else Debug.Log("not ready");
    }

    public void AddFromClient()
    {
        if (RuntimeUpdate.Instance != null) RuntimeUpdate.Instance.ChangeTypeRequest_RPC(false);
        else Debug.Log("not ready");
    }

    public void ShowDebugText(string text)
    {
        if (DebugTextObj != null && DebugText != null)
        {
            DebugTextObj.SetActive(true);
            DebugText.text = text;
            StartCoroutine(ShowDebugTextTime());
        }
    }

    IEnumerator ShowDebugTextTime()
    {
        yield return new WaitForSeconds(3);
        if (DebugText != null) DebugText.text = null;
        if (DebugTextObj != null) DebugTextObj.SetActive(false);
    }

    public void UpdateOverTime()
    {
        if (AlreadyJoin)
        {
            if (LobbyGameObject != null) LobbyGameObject.SetActive(true);
            if (RoomCode != null) RoomCode.text = _sessionKey;
            if (JoinSession != null) JoinSession.SetActive(false);
        }
        else
        {
            if (LobbyGameObject != null) LobbyGameObject.SetActive(false);
        }

        CheckCurrentType();
        ReadyToPlay();
    }

    public void UpdateCode(string code)
    {
        if (RoomCode != null) RoomCode.text = code;
        _sessionKey = code;
    }

    public void SetupButtonOnline(bool playerHost)
    {
        if (playerHost)
        {
            if (ChangeCharacterButtonForHost != null) ChangeCharacterButtonForHost.interactable = true;
            if (ChangeCharacterButtonForClient != null) ChangeCharacterButtonForClient.interactable = false;
            if (ChangeCharacterButtonForHost2 != null) ChangeCharacterButtonForHost2.interactable = true;
            if (ChangeCharacterButtonForClient2 != null) ChangeCharacterButtonForClient2.interactable = false;
        }
        else
        {
            if (ChangeCharacterButtonForHost != null) ChangeCharacterButtonForHost.interactable = false;
            if (ChangeCharacterButtonForClient != null) ChangeCharacterButtonForClient.interactable = true;
            if (ChangeCharacterButtonForHost2 != null) ChangeCharacterButtonForHost2.interactable = false;
            if (ChangeCharacterButtonForClient2 != null) ChangeCharacterButtonForClient2.interactable = true;
        }
    }

    public void UpdateTMPText(int playerNum)
    {
        if (playerNum == 1)
        {
            s_playerHostName = "Player1";
            if (playerHostName != null) playerHostName.text = s_playerHostName;
        }

        if (playerNum == 2)
        {
            s_playerClientName = "Player2";
            if (playerClientName != null) playerClientName.text = s_playerClientName;
        }
    }

    public void CheckCurrentType()
    {
        if (networkRunner != null && networkRunner.IsServer)
        {
            if (hostType != characterType.unknow && clientType != characterType.unknow)
            {
                isReady = (hostType != clientType);
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
            if (CharacterNameText_h != null)
            {
                switch (hostType)
                {
                    case characterType.unknow: CharacterNameText_h.text = "???"; break;
                    case characterType.Duck: CharacterNameText_h.text = characterDuck; break;
                    case characterType.Bird: CharacterNameText_h.text = characterBird; break;
                }
            }
        }
        else
        {
            clientType = newType;
            UpdateIcon(false);
            if (CharacterNameText_c != null)
            {
                switch (clientType)
                {
                    case characterType.unknow: CharacterNameText_c.text = "???"; break;
                    case characterType.Duck: CharacterNameText_c.text = characterDuck; break;
                    case characterType.Bird: CharacterNameText_c.text = characterBird; break;
                }
            }
        }

        if (RuntimeUpdate.Instance != null) RuntimeUpdate.Instance.UpdateType_RPC();
    }

    public void UpdateIcon(bool isHost)
    {
        if (isHost && CharacterHostIcon != null)
        {
            CharacterHostIcon.sprite = GetSpriteFromType(hostType);
        }

        if (!isHost && CharacterClientIcon != null)
        {
            CharacterClientIcon.sprite = GetSpriteFromType(clientType);
        }
    }

    private Sprite GetSpriteFromType(characterType type)
    {
        switch (type)
        {
            case characterType.Duck: return _playerDuckImage;
            case characterType.Bird: return _playerBirdImage;
            default: return unknownImage;
        }
    }

    public void SetMainButtonOff(bool o)
    {
        if (_joinSessionButton != null) _joinSessionButton.gameObject.SetActive(o);
        if (_CreateSessionButton != null) _CreateSessionButton.gameObject.SetActive(o);
    }

    public void JoinSessionRoom()
    {
        if (JoinSession != null) JoinSession.SetActive(true);
        SetMainButtonOff(false);
        if (_JoinRoomButton != null) _JoinRoomButton.interactable = true;
    }

    public void JoinRoom()
    {
        if (_sessionNumberInsertField == null) return;

        if (_sessionNumberInsertField.text.Length < 6 || _sessionNumberInsertField.text.Length > 6)
        {
            _sessionNumberInsertField.text = "Wrong Session key";
        }
        else if (_sessionNumberInsertField.text.Length == 6)
        {
            if (_JoinRoomButton != null) _JoinRoomButton.interactable = false;

            string key = _sessionNumberInsertField.text;
            if (SessionManager.Instance != null) SessionManager.Instance.JoinSession(key);
        }
    }

    public void onDisconnected()
    {
        if (_startButton != null) _startButton.gameObject.SetActive(false);
        if (LobbyGameObject != null)
        {
            LobbyGameObject.SetActive(false);
            SetMainButtonOff(true);
        }

        ResetMenuButtons();
    }

    public void DoneJoin()
    {
        if (AlreadyJoin)
        {
            if (JoinSession != null) JoinSession.SetActive(false);
            if (_startButton != null) _startButton.gameObject.SetActive(false);
            if (LobbyGameObject != null) LobbyGameObject.SetActive(true);
            SetMainButtonOff(false);
        }
        else
        {
            if (_startButton != null) _startButton.gameObject.SetActive(false);
        }
    }

    public void OnJoinFailed()
    {
        if (_JoinRoomButton != null) _JoinRoomButton.interactable = true;
        if (_sessionNumberInsertField != null) _sessionNumberInsertField.text = "Connection Failed";
    }

    public void LeaveRoom()
    {
        if (_leaveButton != null) _leaveButton.interactable = false;
        if (_leaveButton2 != null) _leaveButton2.interactable = false;

        if (AlreadyJoin)
        {
            if (SessionManager.Instance != null) SessionManager.Instance.LeaveSession(AlreadyJoin);
            SetMainButtonOff(true);
        }

        if (!AlreadyJoin)
        {
            if (_startButton != null) _startButton.gameObject.SetActive(false);
            if (JoinSession != null) JoinSession.SetActive(false);
            if (LobbyGameObject != null) LobbyGameObject.SetActive(false);
            SetMainButtonOff(true);
        }

        if (!string.IsNullOrEmpty(_sessionKey))
        {
            if (_sessionNumberInsertField != null)
            {
                _sessionNumberInsertField.text = "";
                _sessionNumberInsertField.interactable = true;
            }
            _sessionKey = "";

            if (string.IsNullOrEmpty(_sessionKey))
            {
                if (LobbyGameObject != null) LobbyGameObject.SetActive(false);
                if (_joinSessionButton != null) _joinSessionButton.gameObject.SetActive(true);
                if (_CreateSessionButton != null) _CreateSessionButton.gameObject.SetActive(true);
            }
        }

        ResetMenuButtons();
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
        if (_CreateSessionButton != null) _CreateSessionButton.interactable = false;
        if (_joinSessionButton != null) _joinSessionButton.interactable = false;

        if (SessionManager.Instance != null) SessionManager.Instance.GenerateCode();

        if (_joinSessionButton != null) _joinSessionButton.gameObject.SetActive(false);
        if (_leaveButton != null)
        {
            _leaveButton.gameObject.SetActive(true);
            _leaveButton.interactable = true;
        }
        if (_leaveButton2 != null) _leaveButton2.interactable = true;

        if (_startButton != null) _startButton.gameObject.SetActive(true);
    }

    public void UpdateList(int playerCount)
    {
        _playerCount = playerCount;
    }

    public void ResetMenuButtons()
    {
        if (_CreateSessionButton != null) _CreateSessionButton.interactable = true;
        if (_joinSessionButton != null) _joinSessionButton.interactable = true;
        if (_JoinRoomButton != null) _JoinRoomButton.interactable = true;

        if (_leaveButton != null) _leaveButton.interactable = true;
        if (_leaveButton2 != null) _leaveButton2.interactable = true;

        if (_startButton != null) _startButton.interactable = false;
    }

    public void DesetButton()
    {
        if (_joinSessionButton != null) _joinSessionButton.onClick.RemoveAllListeners();
        if (_CreateSessionButton != null) _CreateSessionButton.onClick.RemoveAllListeners();
        if (_JoinRoomButton != null) _JoinRoomButton.onClick.RemoveAllListeners();
        if (_clipBoardCopy != null) _clipBoardCopy.onClick.RemoveAllListeners();
        if (_leaveButton != null) _leaveButton.onClick.RemoveAllListeners();
        if (_leaveButton2 != null) _leaveButton2.onClick.RemoveAllListeners();
        if (_startButton != null) _startButton.onClick.RemoveAllListeners();

        StopAllCoroutines();
    }
}