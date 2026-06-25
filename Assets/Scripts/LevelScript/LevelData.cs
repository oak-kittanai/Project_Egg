using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class LevelData : MonoBehaviour
{
    public static LevelData Instance;

    [Header("Map Settings")]
    public Transform SpawnPosition;
    public CheckPoint[] levelCheckPoints;

    [Header("Intro Cutscene Setting")]
    public VideoClip introClip;

    public string cutsceneFolderName = "";

    public string cutsceneFileName = "Cutscene1.mp4";

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
    public bool isInteractShow, isJumpShow, isFlyingShow;
    public bool isDivingShow, isMovingShow, isCarryShow;
    [SerializeField] Image tutorialSlot;
    public TutorialData[] tutorials;
    [SerializeField] private TutorialProgressSO tutorialProgress;
    public List<TutorialData> SeenTutorials => tutorialProgress != null ? tutorialProgress.seenTutorials : null;

    private void Awake()
    {
        Instance = this;
    }

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => GameManager.Instance != null);
        yield return new WaitUntil(() => GameManager.Instance.Object != null && GameManager.Instance.Object.IsValid);
        GameManager.Instance.SetupLevelData(this);
    }

    public void RequestTutorialShow(string requestedName)
    {
        if (tutorialProgress == null) return;

        foreach (var tut in tutorials)
        {
            if (tut.tutorialName == requestedName)
            {
                bool isFirstTime = !tutorialProgress.seenTutorials.Contains(tut);
                if (isFirstTime) tutorialProgress.seenTutorials.Add(tut);

                int idx = tutorialProgress.seenTutorials.IndexOf(tut);
                TutorialUIManager.Instance?.ShowTutorialPanel(tutorialProgress.seenTutorials, idx, isFirstTime);
                return;
            }
        }
    }

    public void RequestTutorialHide() => TutorialUIManager.Instance?.HideTutorial();
}

[System.Serializable]
public class TutorialData
{
    public string tutorialName;
    public Vector2 RectTransform;
    public float displayDuration = 3f; // เวลา "ล็อคปิดด้วย Tab" ตอนเจอครั้งแรก (ไม่ใช่ auto-close แล้ว)
    public Sprite tutorialSprite;
}