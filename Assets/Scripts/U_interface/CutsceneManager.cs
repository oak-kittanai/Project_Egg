using Fusion;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;
using System.Collections;

public class CutsceneManager : NetworkBehaviour
{
    public static CutsceneManager Instance;

    [Networked, OnChangedRender(nameof(OnVoteChanged))]
    public NetworkBool HostSkipReady { get; set; }

    [Networked, OnChangedRender(nameof(OnVoteChanged))]
    public NetworkBool ClientSkipReady { get; set; }

    [Networked] public NetworkBool IsCutscenePlaying { get; set; }

    private void Awake()
    {
        Instance = this;
    }

    public override void Spawned()
    {
        base.Spawned();
        StartCoroutine(SetupWhenLevelDataReady());
    }

    private IEnumerator SetupWhenLevelDataReady()
    {
        yield return new WaitUntil(() => LevelData.Instance != null);
        yield return null;

        Setup();
    }

    public void Setup()
    {
        if (LevelData.Instance == null)
        {
            Debug.LogWarning("[CutsceneManager] LevelData.Instance is null");
            return;
        }

        if (LevelData.Instance.introClip == null)
        {
            Debug.Log("[CutsceneManager] No introClip → PlayLoadingOnly");
            PlayLoadingOnly();
            return;
        }

        if (LevelData.Instance.introVideoPlayer == null)
        {
            Debug.LogWarning("[CutsceneManager] introVideoPlayer null → fallback PlayLoadingOnly");
            PlayLoadingOnly();
            return;
        }

        if (LevelData.Instance.videoLoadingPlayer != null)
            LevelData.Instance.videoLoadingPlayer.enabled = false;

        Debug.Log("[CutsceneManager] Has introClip → SetupAndPlayCutscene");
        SetupAndPlayCutscene();
    }

    private void PlayLoadingOnly()
    {
        if (LevelData.Instance.videoLoadingPlayer != null)
        {
            LevelData.Instance.videoLoadingPlayer.enabled = true;
            LevelData.Instance.videoLoadingPlayer.Play();
        }

        if (PlayerInterface.Instance != null)
            PlayerInterface.Instance.SetSkipButtonActive(false);

        Debug.Log($"[CutsceneManager] PlayLoadingOnly (HasAuth: {HasStateAuthority})");
        if (HasStateAuthority)
        {
            GameManager.Instance.MapsLoadedCount++;
            GameManager.Instance.CheckMapLoadingPublic();
        }
        else
        {
            RPC_NotifyMapReady();
        }
    }

    private void SetupAndPlayCutscene()
    {
        IsCutscenePlaying = true;

        if (PlayerInterface.Instance?.skipButton != null)
        {
            PlayerInterface.Instance.skipButton.onClick.RemoveAllListeners();
            PlayerInterface.Instance.skipButton.onClick.AddListener(OnClickSkip);
            PlayerInterface.Instance.SetSkipButtonActive(true);
            PlayerInterface.Instance.skipButton.interactable = true;
            UpdateVoteUI();
        }

        if (LevelData.Instance.loadingScreenUI != null)
            LevelData.Instance.loadingScreenUI.SetActive(true);

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
            PlayerInterface.Instance.skipButton.interactable = false;

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

        Debug.Log($"[CutsceneManager] CheckVoteProgress {skipCount}/{activePlayers}");

        if (skipCount >= activePlayers)
            RPC_EndCutsceneAll();
    }

    private void OnVideoPlaybackFinished(VideoPlayer vp)
    {
        LevelData.Instance.introVideoPlayer.loopPointReached -= OnVideoPlaybackFinished;
        Debug.Log("[CutsceneManager] Video finished → RPC_EndCutsceneAll");
        RPC_EndCutsceneAll();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_EndCutsceneAll()
    {
        Debug.Log("[CutsceneManager] RPC_EndCutsceneAll received → FinishCutsceneAndStartGame");
        FinishCutsceneAndStartGame();
    }

    private void FinishCutsceneAndStartGame()
    {
        if (LevelData.Instance?.introVideoPlayer != null)
            LevelData.Instance.introVideoPlayer.Stop();

        if (LevelData.Instance?.loadingScreenUI != null)
            LevelData.Instance.loadingScreenUI.SetActive(false);

        if (PlayerInterface.Instance != null)
            PlayerInterface.Instance.SetSkipButtonActive(false);

        if (HasStateAuthority)
        {
            int playerCount = Runner.ActivePlayers.Count();
            Debug.Log($"[CutsceneManager] StateAuthority: counting {playerCount} players as map ready");
            GameManager.Instance.MapsLoadedCount += playerCount;
            GameManager.Instance.CheckMapLoadingPublic();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_NotifyMapReady()
    {
        Debug.Log("[CutsceneManager] RPC_NotifyMapReady received");
        GameManager.Instance.MapsLoadedCount++;
        Debug.Log($"[CutsceneManager] MapsLoadedCount: {GameManager.Instance.MapsLoadedCount}");
        GameManager.Instance.CheckMapLoadingPublic();
    }

    public void OnVoteChanged() => UpdateVoteUI();

    private void UpdateVoteUI()
    {
        if (PlayerInterface.Instance == null) return;

        int activePlayers = Runner.ActivePlayers.Count();
        int skipCount = (HostSkipReady ? 1 : 0) + (ClientSkipReady ? 1 : 0);

        if (PlayerInterface.Instance.skipPlayerCheckText != null)
            PlayerInterface.Instance.skipPlayerCheckText.text = $"{skipCount}/{activePlayers}";
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (LevelData.Instance?.introVideoPlayer != null)
            LevelData.Instance.introVideoPlayer.loopPointReached -= OnVideoPlaybackFinished;
    }
}