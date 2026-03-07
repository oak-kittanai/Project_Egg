using UnityEngine;

public class LevelData : MonoBehaviour
{
    [Header("Map Settings")]
    public Vector3 startingSpawnPosition;
    public CheckPoint[] levelCheckPoints;

    [Header("Important Objects")]
    public NetworkDoor mainExitDoor; // to go next map

    [Header("UI")]
    public GameObject loadingScreenUI;

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

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetupLevelData(this);
        }
    }
}
