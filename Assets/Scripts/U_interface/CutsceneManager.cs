using Fusion;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;

public class CutsceneManager : NetworkBehaviour
{
    public static CutsceneManager Instance;

    [Networked, OnChangedRender(nameof(OnVoteChanged))]
    public NetworkBool HostSkipReady { get; set; }

    [Networked, OnChangedRender(nameof(OnVoteChanged))]
    public NetworkBool ClientSkipReady { get; set; }

    [Networked]
    public NetworkBool IsCutscenePlaying { get; set; }

    private void Awake()
    {
        Instance = this;
    }

    public override void Spawned()
    {
        if (LevelData.Instance == null) return;

        if (LevelData.Instance.introClip != null && LevelData.Instance.introVideoPlayer != null)
        {
            if (LevelData.Instance.videoLoadingPlayer != null)
            {
                LevelData.Instance.videoLoadingPlayer.enabled = false;
            }

            SetupAndPlayCutscene();
        }
        else
        {
            if (LevelData.Instance.videoLoadingPlayer != null)
            {
                LevelData.Instance.videoLoadingPlayer.enabled = true;
                LevelData.Instance.videoLoadingPlayer.Play();
            }

            if (PlayerInterface.Instance != null)
            {
                PlayerInterface.Instance.SetSkipButtonActive(false);
            }

            if (HasStateAuthority)
            {
                GameManager.Instance?.MapFinishedLoading();
            }
        }
    }

    private void SetupAndPlayCutscene()
    {
        IsCutscenePlaying = true;

        if (PlayerInterface.Instance != null)
        {
            if (PlayerInterface.Instance.skipButton != null)
            {
                PlayerInterface.Instance.skipButton.onClick.RemoveAllListeners();
                PlayerInterface.Instance.skipButton.onClick.AddListener(OnClickSkip);
                PlayerInterface.Instance.SetSkipButtonActive(true);
                PlayerInterface.Instance.skipButton.interactable = true;
            }
            UpdateVoteUI();
        }

        if (LevelData.Instance.loadingScreenUI != null)
        {
            LevelData.Instance.loadingScreenUI.SetActive(true);
        }

        LevelData.Instance.introVideoPlayer.clip = LevelData.Instance.introClip;
        LevelData.Instance.introVideoPlayer.Play();

        if (HasStateAuthority)
        {
            LevelData.Instance.introVideoPlayer.loopPointReached += OnVideoPlaybackFinished;
        }
    }

    private void OnClickSkip()
    {
        if (PlayerInterface.Instance?.skipButton != null)
        {
            PlayerInterface.Instance.skipButton.interactable = false;
        }

        RPC_SubmitSkipVote(Runner.LocalPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SubmitSkipVote(PlayerRef playerRef)
    {
        if (playerRef == Runner.LocalPlayer)
            HostSkipReady = true;
        else
            ClientSkipReady = true;

        CheckVoteProgress();
    }

    private void CheckVoteProgress()
    {
        int activePlayers = Runner.ActivePlayers.Count();
        int skipCount = (HostSkipReady ? 1 : 0) + (ClientSkipReady ? 1 : 0);

        if (skipCount >= activePlayers)
        {
            RPC_EndCutsceneAll();
        }
    }

    private void OnVideoPlaybackFinished(VideoPlayer vp)
    {
        LevelData.Instance.introVideoPlayer.loopPointReached -= OnVideoPlaybackFinished;
        RPC_EndCutsceneAll();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_EndCutsceneAll()
    {
        FinishCutsceneAndStartGame();
    }

    private void FinishCutsceneAndStartGame()
    {
        if (!IsCutscenePlaying) return;
        IsCutscenePlaying = false;

        if (LevelData.Instance?.introVideoPlayer != null)
            LevelData.Instance.introVideoPlayer.Stop();

        if (LevelData.Instance?.videoLoadingPlayer != null)
        {
            LevelData.Instance.videoLoadingPlayer.enabled = true;
            LevelData.Instance.videoLoadingPlayer.Play();
        }

        if (PlayerInterface.Instance != null)
            PlayerInterface.Instance.SetSkipButtonActive(false);

        if (HasStateAuthority)
        {
            GameManager.Instance?.MapFinishedLoading();
        }
    }

    public void OnVoteChanged()
    {
        UpdateVoteUI();
    }

    private void UpdateVoteUI()
    {
        if (PlayerInterface.Instance == null) return;

        int activePlayers = Runner.ActivePlayers.Count();
        int skipCount = (HostSkipReady ? 1 : 0) + (ClientSkipReady ? 1 : 0);

        if (PlayerInterface.Instance.skipPlayerCheckText != null)
        {
            PlayerInterface.Instance.skipPlayerCheckText.text = $"{skipCount}/{activePlayers}";
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (LevelData.Instance?.introVideoPlayer != null)
        {
            LevelData.Instance.introVideoPlayer.loopPointReached -= OnVideoPlaybackFinished;
        }
    }
}