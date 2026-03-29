using UnityEngine;

public class GlobalLoadingManager : MonoBehaviour
{
    public static GlobalLoadingManager Instance;

    [Header("UI Reference")]
    [SerializeField] private GameObject loadingPanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowLoading()
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);
    }

    public void HideLoading()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }
}