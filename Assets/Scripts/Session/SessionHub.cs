using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum characterType
{
    Duck,
    Bird,
    unknow
}

public class SessionHub : SingletonNetwork<SessionHub>
{
    [Header("SessionHub")]
    [SerializeField] TMP_Text _sessionKeyText;
    [SerializeField] string _sessionKey;

    [SerializeField] Button _leaveButton;

    // JoinRoomZone
    [SerializeField] TMP_InputField _sessionNumberInsertField;
    [SerializeField] string _inputRoomKey;
    [SerializeField] Button _joinRoomButton;

    [SerializeField] bool AlreadyJoin;

    [SerializeField] int _playerCount;

    [Header("JoinSession")]
    [SerializeField] GameObject JoinSession;

    [Header("Lobby")]
    [SerializeField] Button _CreateRoomNumber;

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

    [SerializeField] TMP_Text RoomCode;

    [SerializeField] Button _startButton;

    public void SetupButton()
    {
        _joinRoomButton.onClick.AddListener(JoinSessionRoom);
        _CreateRoomNumber.onClick.AddListener(CreateRoom);
    }

    public void SetupAssets()
    {
        Sprite Duck = Resources.Load<Sprite>("UI_Assets/SessionRoom/Profile/Kael_Profile.png");
        _playerDuckImage = Duck;

        Sprite Bird = Resources.Load<Sprite>("UI_Assets/SessionRoom/Profile/Mira_Profile.png");
        _playerBirdImage = Bird;

        Sprite unknow = Resources.Load<Sprite>("UI_Assets/SessionRoom/Profile/Unknow_Character.png");
        unknownImage = unknow;
    }

    public override void FixedUpdateNetwork()
    {
        if (AlreadyJoin)
        {
            LobbyGameObject.SetActive(true);
        }
        else
        {
            LobbyGameObject.SetActive(false);
        }
    }

    public void UpdateTMPText(int playerNum)
    {
        RoomCode.text = _sessionKey;
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

    public void UpdateIcon(int playerNum) // Update Icon when change
    {
        if (playerNum == 1)
        {
            switch (hostType)
            {
                case characterType.Duck:
                    CharacterHostIcon.sprite = _playerDuckImage;
                    break;
                case characterType.Bird:
                    CharacterHostIcon.sprite = _playerDuckImage;
                    break;
                default:
                    CharacterHostIcon.sprite = unknownImage;
                    break;
            }
        }

        if (playerNum == 2)
        {
            switch (clientType)
            {
                case characterType.Duck:
                    CharacterClientIcon.sprite = _playerDuckImage;
                    break;
                case characterType.Bird:
                    CharacterClientIcon.sprite = _playerDuckImage;
                    break;
                default:
                    CharacterClientIcon.sprite = unknownImage;
                    break;
            }
        }

    }

    public void SetMainButtonOff(bool o)
    {
        _joinRoomButton.gameObject.SetActive(o);
        _CreateRoomNumber.gameObject.SetActive(o);
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

            AlreadyJoin = SessionManager.Instance.JoinSession(key);

            if (AlreadyJoin)
            {
                _startButton.gameObject.SetActive(false);
                SetMainButtonOff(false);
            }
            else
            {
                _startButton.gameObject.SetActive(true);
            }
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
            _startButton.gameObject.SetActive(true);
        }

        if (!string.IsNullOrEmpty(_sessionKey))
        {
            _sessionNumberInsertField.text = "";
            _sessionNumberInsertField.interactable = true;
            _sessionKey = "";
            if (string.IsNullOrEmpty(_sessionKey))
            {
                _joinRoomButton.gameObject.SetActive(true);
                _sessionKeyText.text = "Session key is : " + _sessionKey;
            }
        }
    }

    public void CreateRoom()
    {
        string roomKey = SessionManager.Instance.GenerateCode();

        roomKey = _sessionKey;
        LobbyGameObject.SetActive(true);

        _joinRoomButton.gameObject.SetActive(false);
        _leaveButton.gameObject.SetActive(true);

        _startButton.gameObject.SetActive(true);

        if (!string.IsNullOrEmpty(roomKey))
        {
            Debug.Log($"Create session Key : {_sessionKey}");
        }
        else
        {
            Debug.Log($"Session key is : {_sessionKey}");
        }
    }

    public void UpdateList(int playerCount)
    {
        _playerCount = playerCount;
    }

    public void StartTheGame()
    {
        SessionManager.Instance.StartGame();
    }
}
