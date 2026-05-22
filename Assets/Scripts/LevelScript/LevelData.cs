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

    [Header("Cutscene")]
    public VideoClip introClip;

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
        foreach (var tut in tutorials)
        {
            if (tut.tutorialName == requestedName)
            {
                TutorialUIManager.Instance?.ShowTutorial(
                    tut.tutorialSprite, tut.RectTransform, tut.displayDuration);
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
    public Sprite tutorialSprite;
    public float displayDuration = 5f;
}