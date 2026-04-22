using Fusion;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionHub : SingletonNetwork<SessionHub>
{
    #region Variables & UI Components
    [SerializeField] public NetworkRunner networkRunner;

    [Header("Session State")]
    public SessionState currentState;
    public event Action<SessionState> OnStateChanged;

    [Header("SessionHub")]
    [SerializeField] TMP_Text _sessionKeyText;
    [SerializeField] Button _clipBoardCopy;
    [SerializeField] string _sessionKey;
    [SerializeField] Button _leaveButton;
    [SerializeField] Button _leaveButton2;
    [SerializeField] string _inputRoomKey;
    [SerializeField] bool AlreadyJoin => SessionManager.Instance != null && SessionManager.Instance._isAlreadyInRoom;
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

    [SerializeField] public Button ChangeCharacterButtonForHostAdd;
    [SerializeField] public Button ChangeCharacterButtonForHostReduce;

    [SerializeField] public Button ChangeCharacterButtonForClientAdd;
    [SerializeField] public Button ChangeCharacterButtonForClientReduce;

    [SerializeField] TMP_Text RoomCode;
    [SerializeField] Button _startButton;
    
    [SerializeField] public Button _clientReadyButton;
    [SerializeField] public Button _hostReadyButton;

    [SerializeField] Sprite _isready;
    [SerializeField] Sprite _notReady;

    [SerializeField] bool _gameReadyToStart => RuntimeUpdate.Instance.isHostReady && RuntimeUpdate.Instance.isClientReady;

    [Header("Support_Text")]
    [SerializeField] GameObject DebugTextObj;
    [SerializeField] TMP_Text DebugText;
    #endregion

    #region Initialization & State Management
    public void Setup()
    {
        if (_joinSessionButton != null) _joinSessionButton.onClick.AddListener(JoinSessionRoom);
        if (_CreateSessionButton != null) _CreateSessionButton.onClick.AddListener(CreateRoom);
        if (_JoinRoomButton != null) _JoinRoomButton.onClick.AddListener(JoinRoom);

        if (_leaveButton != null) _leaveButton.onClick.AddListener(LeaveRoom);
        if (_leaveButton2 != null) _leaveButton2.onClick.AddListener(LeaveRoom);

        if (ChangeCharacterButtonForHostAdd != null) ChangeCharacterButtonForHostAdd.onClick.AddListener(AddFromHost);
        if (ChangeCharacterButtonForClientAdd != null) ChangeCharacterButtonForClientAdd.onClick.AddListener(AddFromClient);
        if (ChangeCharacterButtonForHostReduce != null) ChangeCharacterButtonForHostReduce.onClick.AddListener(ReduceFromHost);
        if (ChangeCharacterButtonForClientReduce != null) ChangeCharacterButtonForClientReduce.onClick.AddListener(RreduceFromClient);
        if (_clientReadyButton != null) _clientReadyButton.onClick.AddListener(ClientReady);
        if (_hostReadyButton != null) _hostReadyButton.onClick.AddListener(HostReady);

        if (_clipBoardCopy != null) _clipBoardCopy.onClick.AddListener(CopyKeyToClipboard);

        if (_startButton != null)
        {
            _startButton.onClick.AddListener(StartTheGame);
            _startButton.interactable = false;
        }

        if (DebugTextObj != null) DebugTextObj.SetActive(false);
    }

    private void Start()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.OnStateChanged += HandleStateChanged;
            HandleStateChanged(SessionManager.Instance.currentState);
        }
    }

    private void OnDestroy()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.OnStateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(SessionState newState)
    {
        Debug.Log($"Current State : {newState}");
        currentState = newState;

        switch (newState)
        {
            case SessionState.MainMenu:
                OpenMainMenuUI();
                break;

            case SessionState.Join:
                OpenJoinUI();
                break;

            case SessionState.CharacterSelect:
            case SessionState.SessionSelect:
                OpenLobbyUI();
                break;

            case SessionState.Setting:
                // OpenSettingUI();
                break;
        }
    }
    #endregion

    #region UI Panel Management
    private void HideAllPanels()
    {
        if (JoinSession != null) JoinSession.SetActive(false);
        if (LobbyGameObject != null) LobbyGameObject.SetActive(false);
        SetMainButtonOff(false);
        if (_startButton != null) _startButton.gameObject.SetActive(false);
    }

    public void OpenMainMenuUI()
    {
        HideAllPanels();
        SetMainButtonOff(true);
        ResetMenuButtons();
    }

    public void OpenJoinUI()
    {
        HideAllPanels();
        if (JoinSession != null) JoinSession.SetActive(true);
        if (_JoinRoomButton != null) _JoinRoomButton.interactable = true;
    }

    public void OpenLobbyUI()
    {
        HideAllPanels();
        if (LobbyGameObject != null) LobbyGameObject.SetActive(true);

        if (networkRunner != null && networkRunner.IsServer)
        {
            if (_startButton != null) _startButton.gameObject.SetActive(true);
        }

        if (_leaveButton != null) { _leaveButton.gameObject.SetActive(true); _leaveButton.interactable = true; }
        if (_leaveButton2 != null) { _leaveButton2.gameObject.SetActive(true); _leaveButton2.interactable = true; }
    }
    #endregion

    #region Room & Connection Flow
    public void CreateRoom()
    {
        if (_CreateSessionButton != null) _CreateSessionButton.interactable = false;
        if (_joinSessionButton != null) _joinSessionButton.interactable = false;

        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.GenerateCode();
        }
    }

    public void JoinSessionRoom()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.ChangeState(SessionState.Join);
        }
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

    public void DoneJoin()
    {
        if (AlreadyJoin && SessionManager.Instance != null)
        {
            SessionManager.Instance.ChangeState(SessionState.SessionSelect);
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

        if (AlreadyJoin && SessionManager.Instance != null)
        {
            SessionManager.Instance.LeaveSession(AlreadyJoin);
        }

        if (_sessionNumberInsertField != null)
        {
            _sessionNumberInsertField.text = "";
            _sessionNumberInsertField.interactable = true;
        }
        _sessionKey = "";

        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.ChangeState(SessionState.MainMenu);
        }
    }

    public void onDisconnected()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.ChangeState(SessionState.MainMenu);
        }
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
    #endregion

    #region Lobby & Character Selection
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

    public void ReadyToPlay()
    {
        if (networkRunner != null && networkRunner.IsServer)
        {
            if (_startButton != null)
            {
                if (isReady && _gameReadyToStart)
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

    public void SetDefault(NetworkRunner runner)
    {
        if (runner == null) return;

        if (runner.IsServer) hostType = characterType.unknow;
        else clientType = characterType.unknow;
    }

    public void HostReady()
    {
        if (RuntimeUpdate.Instance != null)
        {
            if (!RuntimeUpdate.Instance.isHostReady)
            {
                if (hostType == characterType.unknow || hostType == clientType)
                {
                    ShowDebugText("same Character || not choose character");
                    return;
                }
            }
            RuntimeUpdate.Instance.PlayerReady_RPC(true);
        }
    }

    public void ClientReady()
    {
        if (RuntimeUpdate.Instance != null)
        {
            if (!RuntimeUpdate.Instance.isClientReady)
            {
                if (clientType == characterType.unknow || clientType == hostType)
                {
                    ShowDebugText("same Character || not choose character");
                    return;
                }
            }
            RuntimeUpdate.Instance.PlayerReady_RPC(false);
        }
    }

    public void AddFromHost()
    {
        if (RuntimeUpdate.Instance != null) RuntimeUpdate.Instance.ChangeTypeRequest_RPC(true, false);
    }

    public void AddFromClient()
    {
        if (RuntimeUpdate.Instance != null) RuntimeUpdate.Instance.ChangeTypeRequest_RPC(false, false);
    }

    public void ReduceFromHost()
    {
        if (RuntimeUpdate.Instance != null) RuntimeUpdate.Instance.ChangeTypeRequest_RPC(true, true);
    }

    public void RreduceFromClient()
    {
        if (RuntimeUpdate.Instance != null) RuntimeUpdate.Instance.ChangeTypeRequest_RPC(false, true);
    }


    public void SetupButtonOnline(bool playerHost)
    {
        if (playerHost)
        {
            if (ChangeCharacterButtonForHostAdd != null) ChangeCharacterButtonForHostAdd.interactable = true;
            if (ChangeCharacterButtonForClientAdd != null) ChangeCharacterButtonForClientAdd.interactable = false;
            if (ChangeCharacterButtonForHostReduce != null) ChangeCharacterButtonForHostReduce.interactable = true;
            if (ChangeCharacterButtonForClientReduce != null) ChangeCharacterButtonForClientReduce.interactable = false;
            if (_hostReadyButton != null) _hostReadyButton.interactable = true;
            if (_clientReadyButton != null) _clientReadyButton.interactable = false;
        }
        else
        {
            if (ChangeCharacterButtonForHostAdd != null) ChangeCharacterButtonForHostAdd.interactable = false;
            if (ChangeCharacterButtonForClientAdd != null) ChangeCharacterButtonForClientAdd.interactable = true;
            if (ChangeCharacterButtonForHostReduce != null) ChangeCharacterButtonForHostReduce.interactable = false;
            if (ChangeCharacterButtonForClientReduce != null) ChangeCharacterButtonForClientReduce.interactable = true;
            if (_hostReadyButton != null) _hostReadyButton.interactable = false;
            if (_clientReadyButton != null) _clientReadyButton.interactable = true;
        }
    }

    public void ChangeReadySprite(bool isHost, bool o)
    {
        if (isHost)
        {
            if (o) _hostReadyButton.image.sprite = _isready;
            else _hostReadyButton.image.sprite = _notReady;
        }
        else
        {
            if (o) _clientReadyButton.image.sprite = _isready;
            else _clientReadyButton.image.sprite = _notReady;
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
    #endregion

    #region Utility & UI Updates
    public void UpdateOverTime()
    {
        if (AlreadyJoin && RoomCode != null)
        {
            RoomCode.text = _sessionKey;
        }

        CheckCurrentType();
        ReadyToPlay();
    }

    public void UpdateCode(string code)
    {
        if (RoomCode != null) RoomCode.text = code;
        _sessionKey = code;
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

    public void UpdateList(int playerCount)
    {
        _playerCount = playerCount;
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

    public void SetMainButtonOff(bool o)
    {
        if (_joinSessionButton != null) _joinSessionButton.gameObject.SetActive(o);
        if (_CreateSessionButton != null) _CreateSessionButton.gameObject.SetActive(o);
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
    #endregion
}