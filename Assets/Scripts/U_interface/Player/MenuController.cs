using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;

public class MenuController : NetworkBehaviour
{
    public static MenuController Instance;

    [Header("Menu State")]
    [Networked, OnChangedRender(nameof(OnMenuStateChanged))]
    public NetworkBool IsMenuOpen { get; set; }

    public bool IsMenuOpenSafe => Object != null && Object.IsValid && IsMenuOpen;

    [Networked, OnChangedRender(nameof(OnQueueUpdated))]
    public NetworkBool HostResumeReady { get; set; }
    [Networked, OnChangedRender(nameof(OnQueueUpdated))]
    public NetworkBool ClientResumeReady { get; set; }

    [Networked, OnChangedRender(nameof(OnQueueUpdated))]
    public NetworkBool HostResetReady { get; set; }
    [Networked, OnChangedRender(nameof(OnQueueUpdated))]
    public NetworkBool ClientResetReady { get; set; }

    private GameObject pauseMenuPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }

        Instance = this;
    }

    public override void Spawned()
    {
        Invoke(nameof(SetupButtons), 0.5f);
    }

    private void SetupButtons()
    {
        if (PlayerInterface.Instance == null) return;

        if (PlayerInterface.Instance.resumeButton != null)
        {
            pauseMenuPanel = PlayerInterface.Instance.resumeButton.transform.parent.gameObject;
            PlayerInterface.Instance.resumeButton.onClick.AddListener(OnClickResume);
        }

        if (PlayerInterface.Instance.resetButton != null)
            PlayerInterface.Instance.resetButton.onClick.AddListener(OnClickReset);

        if (PlayerInterface.Instance.settingButton != null)
            PlayerInterface.Instance.settingButton.onClick.AddListener(OnClickSetting);

        if (PlayerInterface.Instance.quitButton != null)
            PlayerInterface.Instance.quitButton.onClick.AddListener(OnClickQuit);

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    public void ToggleMenu()
    {
        RPC_ToggleMenu();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ToggleMenu()
    {
        IsMenuOpen = !IsMenuOpen;

        ClearAllVotes();
    }

    private void ClearAllVotes()
    {
        HostResumeReady = false;
        ClientResumeReady = false;
        HostResetReady = false;
        ClientResetReady = false;
    }

    public void OnMenuStateChanged()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(IsMenuOpen);

            if (IsMenuOpen && PlayerInterface.Instance != null)
            {
                if (PlayerInterface.Instance.resumeButton) PlayerInterface.Instance.resumeButton.interactable = true;
                if (PlayerInterface.Instance.resetButton) PlayerInterface.Instance.resetButton.interactable = true;

                int activePlayers = Runner.ActivePlayers.Count();
                if (PlayerInterface.Instance.resumePlayerCheckText) PlayerInterface.Instance.resumePlayerCheckText.text = $"0/{activePlayers}";
                if (PlayerInterface.Instance.resetPlayerCheckText) PlayerInterface.Instance.resetPlayerCheckText.text = $"0/{activePlayers}";
            }
        }
    }

    private void OnClickResume()
    {
        if (PlayerInterface.Instance.resumeButton) PlayerInterface.Instance.resumeButton.interactable = false;
        RPC_SubmitVote(true, Runner.LocalPlayer);
    }

    private void OnClickReset()
    {
        if (PlayerInterface.Instance.resetButton) PlayerInterface.Instance.resetButton.interactable = false;
        RPC_SubmitVote(false, Runner.LocalPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SubmitVote(NetworkBool isResume, PlayerRef playerRef)
    {
        if (isResume)
        {
            if (playerRef == Runner.LocalPlayer) HostResumeReady = true; else ClientResumeReady = true;
        }
        else
        {
            if (playerRef == Runner.LocalPlayer) HostResetReady = true; else ClientResetReady = true;
        }

        int activePlayers = Runner.ActivePlayers.Count();
        int resumeCount = (HostResumeReady ? 1 : 0) + (ClientResumeReady ? 1 : 0);
        int resetCount = (HostResetReady ? 1 : 0) + (ClientResetReady ? 1 : 0);

        if (resumeCount >= activePlayers)
        {
            IsMenuOpen = false;
            ClearAllVotes();
        }
        else if (resetCount >= activePlayers)
        {
            IsMenuOpen = false;
            ClearAllVotes();

            MovementCharacter[] allPlayers = FindObjectsByType<MovementCharacter>(FindObjectsSortMode.None);
            foreach (var p in allPlayers)
            {
                if (!p.isDead) p.DeathMechanic_RPC(true);
            }
        }
    }

    public void OnQueueUpdated()
    {
        if (PlayerInterface.Instance == null) return;

        int activePlayers = Runner.ActivePlayers.Count();
        int resumeCount = (HostResumeReady ? 1 : 0) + (ClientResumeReady ? 1 : 0);
        int resetCount = (HostResetReady ? 1 : 0) + (ClientResetReady ? 1 : 0);

        if (PlayerInterface.Instance.resumePlayerCheckText)
            PlayerInterface.Instance.resumePlayerCheckText.text = $"{resumeCount}/{activePlayers}";

        if (PlayerInterface.Instance.resetPlayerCheckText)
            PlayerInterface.Instance.resetPlayerCheckText.text = $"{resetCount}/{activePlayers}";
    }

    private void OnClickSetting()
    {
        Debug.Log("Setting panel is not implemented yet!");
    }

    private void OnClickQuit()
    {
        RPC_QuitGame();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_QuitGame()
    {
        ExecuteQuitGameAsync();

        PlayerInterface._birdFlyUnlocked = false;
        PlayerInterface._birdThrowUnlocked = false;
        PlayerInterface._duckDiveUnlocked = false;
        PlayerInterface._duckSmashUnlocked = false;
    }

    private void ExecuteQuitGameAsync()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.ReStartNetworkRunner();
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.BackToSessionScene();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("SessionScene");
        }
    }
}