using Fusion;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class CutsceneManager : NetworkBehaviour
{
    public static CutsceneManager Instance;

    [Networked, OnChangedRender(nameof(OnVoteChanged))]
    public NetworkBool HostSkipReady { get; set; }
    [Networked, OnChangedRender(nameof(OnVoteChanged))]
    public NetworkBool ClientSkipReady { get; set; }

    private bool hasFinishedLocal = false;

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

        yield return new WaitUntil(() => PlayerInterface.Instance != null);

        if (LevelData.Instance.introClip != null)
        {
            yield return new WaitUntil(() => PlayerInterface.Instance.introVideoPlayer != null);
        }

        yield return null;

        Setup();
    }

    private void Setup()
    {
        if (LevelData.Instance == null)
        {
            Debug.LogWarning("[CutsceneManager] LevelData null");
            return;
        }

        VideoClip clip = LevelData.Instance.introClip;

        bool hasCutscene = clip != null && PlayerInterface.Instance != null && PlayerInterface.Instance.introVideoPlayer != null;

        if (hasCutscene)
        {
            Debug.Log("[CutsceneManager] Has introClip → cutscene");
            SetupAndPlayCutscene(clip);
        }
        else
        {
            Debug.Log("[CutsceneManager] No introClip → loading only");
            PlayLoadingOnly();
        }
    }

    private void PlayLoadingOnly()
    {
        if (PlayerInterface.Instance != null)
        {
            PlayerInterface.Instance.PlayLoadingVideo();
            PlayerInterface.Instance.SetSkipButtonActive(false);
        }
        NotifyMapReady();
    }

    private void SetupAndPlayCutscene(VideoClip clip)
    {
        if (PlayerInterface.Instance != null)
        {
            if (PlayerInterface.Instance.skipButton != null)
            {
                PlayerInterface.Instance.skipButton.onClick.RemoveAllListeners();
                PlayerInterface.Instance.skipButton.onClick.AddListener(OnClickSkip);
                PlayerInterface.Instance.SetSkipButtonActive(true);
                PlayerInterface.Instance.skipButton.interactable = true;
                UpdateVoteUI();
            }

            PlayerInterface.Instance.PlayIntroCutscene(clip);

            if (HasStateAuthority && PlayerInterface.Instance.introVideoPlayer != null)
                PlayerInterface.Instance.introVideoPlayer.loopPointReached += OnVideoPlaybackFinished;
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
        if (playerRef == Runner.LocalPlayer) HostSkipReady = true;
        else ClientSkipReady = true;

        int activePlayers = Runner.ActivePlayers.Count();
        int skipCount = (HostSkipReady ? 1 : 0) + (ClientSkipReady ? 1 : 0);

        if (skipCount >= activePlayers)
            RPC_EndCutsceneAll();
    }

    private void OnVideoPlaybackFinished(VideoPlayer vp)
    {
        if (PlayerInterface.Instance?.introVideoPlayer != null)
            PlayerInterface.Instance.introVideoPlayer.loopPointReached -= OnVideoPlaybackFinished;

        RPC_EndCutsceneAll();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_EndCutsceneAll()
    {
        FinishCutscene();
    }


    private void FinishCutscene()
    {
        if (hasFinishedLocal) return;
        hasFinishedLocal = true;

        if (PlayerInterface.Instance != null)
        {
            PlayerInterface.Instance.StopIntroCutscene();
            PlayerInterface.Instance.SetSkipButtonActive(false);
            PlayerInterface.Instance.PlayLoadingVideo();
        }

        if (HasStateAuthority)
        {
            GameManager.Instance?.MapAllPlayersFinishedLoading();
        }
    }

    private void NotifyMapReady()
    {
        Debug.Log($"[CutsceneManager] NotifyMapReady (HasAuth: {HasStateAuthority})");
        GameManager.Instance?.MapFinishedLoading();
    }


    public void OnVoteChanged() => UpdateVoteUI();

    private void UpdateVoteUI()
    {
        if (PlayerInterface.Instance?.skipPlayerCheckText == null) return;

        int activePlayers = Runner.ActivePlayers.Count();
        int skipCount = (HostSkipReady ? 1 : 0) + (ClientSkipReady ? 1 : 0);
        PlayerInterface.Instance.skipPlayerCheckText.text = $"{skipCount}/{activePlayers}";
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (PlayerInterface.Instance?.introVideoPlayer != null)
            PlayerInterface.Instance.introVideoPlayer.loopPointReached -= OnVideoPlaybackFinished;
    }
}