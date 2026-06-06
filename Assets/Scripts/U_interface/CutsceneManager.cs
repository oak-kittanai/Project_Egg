using Fusion;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
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

#if UNITY_WEBGL && !UNITY_EDITOR
    yield return new WaitUntil(() => PlayerInterface.Instance.introVideoPlayer != null);
#else
        if (LevelData.Instance.introClip != null)
        {
            yield return new WaitUntil(() => PlayerInterface.Instance.introVideoPlayer != null);
        }
#endif

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

        if (PlayerInterface.Instance == null || PlayerInterface.Instance.introVideoPlayer == null)
        {
            PlayLoadingOnly();
            return;
        }

        var data = LevelData.Instance;

#if UNITY_WEBGL && !UNITY_EDITOR
    // WebGL build → ใช้ไฟล์จาก StreamingAssets
    Debug.Log("[CutsceneManager] Platform: WebGL → StreamingAssets file");
    SetupAndPlayCutsceneFromFile();
#else
        if (data.introClip != null)
        {
            Debug.Log("[CutsceneManager] Platform: Standalone → VideoClip");
            SetupAndPlayCutsceneFromClip(data.introClip);
        }
        else
        {
            Debug.Log("[CutsceneManager] No introClip → loading only");
            PlayLoadingOnly();
        }
#endif
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

    // ==== Mode 1: VideoClip (in-build) ====
    private void SetupAndPlayCutsceneFromClip(VideoClip clip)
    {
        SetupSkipButton();
        PlayerInterface.Instance.PlayIntroCutscene(clip);
        HookVideoEvents();
    }

    // ==== Mode 2: StreamingAssets file ====
    private void SetupAndPlayCutsceneFromFile()
    {
        string folderName = LevelData.Instance.cutsceneFolderName;
        if (string.IsNullOrEmpty(folderName))
            folderName = SceneManager.GetActiveScene().name;

        string fileName = LevelData.Instance.cutsceneFileName;
        if (string.IsNullOrEmpty(fileName))
            fileName = "Cutscene1.mp4";

        Debug.Log($"[CutsceneManager] Loading: StreamingAssets/{folderName}/{fileName}");

        SetupSkipButton();
        PlayerInterface.Instance.PlayIntroCutsceneFromFile(folderName, fileName);
        HookVideoEvents();
    }

    // ==== Shared helpers ====
    private void SetupSkipButton()
    {
        if (PlayerInterface.Instance.skipButton == null) return;

        PlayerInterface.Instance.skipButton.onClick.RemoveAllListeners();
        PlayerInterface.Instance.skipButton.onClick.AddListener(OnClickSkip);
        PlayerInterface.Instance.SetSkipButtonActive(true);
        PlayerInterface.Instance.skipButton.interactable = true;
        UpdateVoteUI();
    }

    private void HookVideoEvents()
    {
        if (!HasStateAuthority) return;
        if (PlayerInterface.Instance.introVideoPlayer == null) return;

        var vp = PlayerInterface.Instance.introVideoPlayer;

        vp.loopPointReached -= OnVideoPlaybackFinished;
        vp.loopPointReached += OnVideoPlaybackFinished;

        vp.errorReceived -= OnVideoError;
        vp.errorReceived += OnVideoError;
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogError($"[CutsceneManager] Video error: {message} → skip cutscene");

        if (PlayerInterface.Instance?.introVideoPlayer != null)
            PlayerInterface.Instance.introVideoPlayer.errorReceived -= OnVideoError;

        if (HasStateAuthority)
            RPC_EndCutsceneAll();
    }

    // ==== Skip vote ====
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

    // ==== Vote UI ====
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
        {
            PlayerInterface.Instance.introVideoPlayer.loopPointReached -= OnVideoPlaybackFinished;
            PlayerInterface.Instance.introVideoPlayer.errorReceived -= OnVideoError;
        }
    }
}