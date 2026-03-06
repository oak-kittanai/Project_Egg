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

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetupLevelData(this);
        }
    }
}
