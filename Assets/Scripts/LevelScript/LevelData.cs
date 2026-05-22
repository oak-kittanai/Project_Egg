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
    public VideoPlayer videoLoadingPlayer;

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

        GameObject canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            RegisterCanvas(canvas);
        }
    }

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => GameManager.Instance != null);
        yield return new WaitUntil(() => GameManager.Instance.Object != null && GameManager.Instance.Object.IsValid);

        GameManager.Instance.SetupLevelData(this);
    }

    public void RegisterCanvas(GameObject canvas)
    {
        Transform loadingScene = canvas.transform.Find("LoadingScene");
        if (loadingScene != null)
        {
            loadingScreenUI = loadingScene.gameObject;

            Transform vp = loadingScene.Find("videoPlayer");
            if (vp != null) introVideoPlayer = vp.GetComponent<VideoPlayer>();

            Transform lv = loadingScene.Find("LoadingVideo");
            if (lv != null) videoLoadingPlayer = lv.GetComponent<VideoPlayer>();
        }
    }

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