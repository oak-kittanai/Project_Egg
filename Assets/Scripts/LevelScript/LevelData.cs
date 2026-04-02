using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

public class LevelData : MonoBehaviour
{
    public static LevelData Instance;

    [Header("Map Settings")]
    public Transform SpawnPosition;
    public CheckPoint[] levelCheckPoints;

    [Header("UI")]
    public GameObject loadingScreenUI;

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
        GameManager.Instance.MapFinishedLoading();
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