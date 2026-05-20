using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class LevelData : MonoBehaviour
{
    public static LevelData Instance;

    [Header("Map Settings")]
    public Transform SpawnPosition;
    public CheckPoint[] levelCheckPoints;

    [Header("UI")]
    public GameObject loadingScreenUI;

    [Header("Cutscene Settings (Intro)")]
    public VideoClip introClip;
    public VideoPlayer introVideoPlayer;
    public GameObject videoUIPanel;

    [Header("SpawnPoints")]
    public GameObject[] spawnPointsInMap;

    [Header("Platforms")]
    public GameObject[] movingPlatforms;
    public GameObject[] timeLimitedPlatforms;

    [Header("Interact Objects")]
    public GameObject[] holdStepButtons;
    public GameObject[] toggleStepButtons;
    public GameObject[] rockTriggerDoors;

    [Header("Traps")]
    public GameObject[] bearTraps;
    public GameObject[] jellyfishes;
    public GameObject[] pressureAndPulls;
    public GameObject[] iceTraps;

    [Header("Tutorial")]
    [SerializeField] bool isTutorialAvailable;

    public bool isInteractShow;
    public bool isJumpShow;
    public bool isFlyingShow;
    public bool isDivingShow;
    public bool isMovingShow;
    public bool isCarryShow;

    // Render
    [SerializeField] Image tutorialSlot;

    public TutorialData[] tutorials;

    private void Awake()
    {
        Instance = this;
    }

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => GameManager.Instance != null);
        yield return new WaitUntil(() => GameManager.Instance.Object != null && GameManager.Instance.Object.IsValid);

        GameManager.Instance.SetupLevelData(this);

        if (introClip != null && introVideoPlayer != null)
        {
            if (videoUIPanel != null) videoUIPanel.SetActive(true);

            introVideoPlayer.clip = introClip;
            introVideoPlayer.Play();

            introVideoPlayer.loopPointReached += OnVideoEnd;
        }
        else
        {
            GameManager.Instance.MapFinishedLoading();
        }
    }

    #region Video

    private void OnVideoEnd(VideoPlayer vp)
    {
        introVideoPlayer.loopPointReached -= OnVideoEnd;

        if (videoUIPanel != null) videoUIPanel.SetActive(false);

        GameManager.Instance.MapFinishedLoading();
    }

    #endregion

    public void RequestTutorialShow(string requestedName)
    {
        foreach (var tut in tutorials)
        {
            if (tut.tutorialName == requestedName)
            {
                if (TutorialUIManager.Instance != null)
                {
                    TutorialUIManager.Instance.ShowTutorial(tut.tutorialSprite, tut.RectTransform, tut.displayDuration);
                }
                return;
            }
        }
        Debug.LogWarning($"can't find Tutorial name : {requestedName} in LevelData!");
    }

    public void RequestTutorialHide()
    {
        if (TutorialUIManager.Instance != null)
        {
            TutorialUIManager.Instance.HideTutorial();
        }
    }
}

[System.Serializable]
public class TutorialData
{
    public string tutorialName;
    public Vector2 RectTransform;
    public Sprite tutorialSprite;
    public float displayDuration = 5f;
}