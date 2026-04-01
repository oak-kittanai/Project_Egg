using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Menu_Interface : NetworkBehaviour
{
    public static Menu_Interface Instance;

    [Header("UI Panels")]
    [SerializeField] GameObject menuContainer;
    [SerializeField] GameObject settingObject;

    [SerializeField] GameObject hostVotePanel;

    [Header("Buttons")]
    [SerializeField] Button voteResumeButton;
    [SerializeField] Button localResumeButton;
    [SerializeField] Button resetButton;
    [SerializeField] Button settingButton;
    [SerializeField] Button quitButton;

    [Header("Queue Texts")]
    [SerializeField] TMP_Text currentResumeQueueShow;
    [SerializeField] TMP_Text currentResetQueueShow;

    [Networked, OnChangedRender(nameof(OnGlobalMenuStateChanged))]
    public NetworkBool IsHostMenuForced { get; set; }

    [Networked, OnChangedRender(nameof(OnQueueUpdated))]
    public int ResumeVotes { get; set; }

    [Networked, OnChangedRender(nameof(OnQueueUpdated))]
    public int ResetVotes { get; set; }

    private void Awake()
    {
        Instance = this;
    }

    public override void Spawned()
    {
        menuContainer.SetActive(false);
        settingObject.SetActive(false);

        voteResumeButton.onClick.AddListener(OnVoteResumeClicked);
        localResumeButton.onClick.AddListener(OnLocalResumeClicked);
        resetButton.onClick.AddListener(OnResetClicked);
        settingButton.onClick.AddListener(OnSettingClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void HostToggleMenu_RPC()
    {
        if (!IsHostMenuForced)
        {
            IsHostMenuForced = true;
            ResumeVotes = 0;
            ResetVotes = 0;
        }
    }

    public void ClientToggleLocalMenu()
    {
        if (IsHostMenuForced) return;

        bool isCurrentlyOpen = menuContainer.activeSelf;
        menuContainer.SetActive(!isCurrentlyOpen);

        if (!isCurrentlyOpen)
        {
            hostVotePanel.SetActive(false);
            voteResumeButton.gameObject.SetActive(false);
            localResumeButton.gameObject.SetActive(true);
        }
    }

    public void OnGlobalMenuStateChanged()
    {
        menuContainer.SetActive(IsHostMenuForced);
        settingObject.SetActive(false);

        if (IsHostMenuForced)
        {
            hostVotePanel.SetActive(true);
            voteResumeButton.gameObject.SetActive(true);
            localResumeButton.gameObject.SetActive(false);

            voteResumeButton.interactable = true;
            resetButton.interactable = true;
        }
    }

    public void OnQueueUpdated()
    {
        if (currentResumeQueueShow != null) currentResumeQueueShow.text = $"Resume: {ResumeVotes}/2";
        if (currentResetQueueShow != null) currentResetQueueShow.text = $"Reset: {ResetVotes}/2";
    }

    private void OnVoteResumeClicked()
    {
        if (IsHostMenuForced)
        {
            voteResumeButton.interactable = false;
            RPC_SubmitVote(true);
        }
    }

    private void OnLocalResumeClicked()
    {
        menuContainer.SetActive(false);
        settingObject.SetActive(false);
    }

    private void OnResetClicked()
    {
        if (IsHostMenuForced)
        {
            resetButton.interactable = false;
            RPC_SubmitVote(false);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SubmitVote(NetworkBool isResume)
    {
        if (isResume)
        {
            ResumeVotes++;
            if (ResumeVotes >= 2) IsHostMenuForced = false;
        }
        else
        {
            ResetVotes++;
            if (ResetVotes >= 2)
            {
                IsHostMenuForced = false;
                if (GameManager.Instance != null) GameManager.Instance.ResetAllPlayersToSpawn();
            }
        }
    }

    private void OnSettingClicked()
    {
        settingObject.SetActive(!settingObject.activeSelf);
    }

    private void OnQuitClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.BackToSessionScene();
    }
}