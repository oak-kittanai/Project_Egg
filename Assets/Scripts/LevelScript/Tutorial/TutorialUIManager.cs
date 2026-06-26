using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TutorialUIManager : MonoBehaviour
{
    public static TutorialUIManager Instance;

    public bool IsTutorialOpen => tutorialPanel != null && tutorialPanel.activeSelf;

    [Header("Tutorial Panel")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private Image backgroundFrame;
    [SerializeField] private Image tutorialImage;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Slider closeBar;
    [SerializeField] private GameObject closeHint;

    private List<TutorialData> currentList;
    private int currentIndex;
    private bool isCloseLocked;
    private Coroutine lockCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject.transform.root.gameObject);

            GameObject canvas = GameObject.Find("Canvas");
            if (canvas != null) RegisterCanvas(canvas);
        }
        else
        {
            Destroy(gameObject.transform.root.gameObject);
        }
    }

    private void Update()
    {
        if (tutorialPanel == null || !tutorialPanel.activeSelf || isCloseLocked) return;
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            HideTutorial();
    }

    public void RegisterCanvas(GameObject canvas)
    {
        var panel = canvas.transform.Find("TutorialPanel");
        if (panel == null) { Debug.LogWarning("TutorialPanel not found in Canvas"); return; }

        tutorialPanel = panel.gameObject;

        var bg = panel.Find("BackGround");
        if (bg != null) backgroundFrame = bg.GetComponent<Image>();

        var img = panel.Find("TutorialBackground");
        if (img != null) tutorialImage = img.GetComponent<Image>();

        prevButton = panel.Find("PrevButton")?.GetComponent<Button>();
        nextButton = panel.Find("NextButton")?.GetComponent<Button>();
        closeBar = panel.Find("SliderTimer")?.GetComponent<Slider>();
        closeHint = panel.Find("PressTabToClose")?.gameObject;

        if (prevButton != null) { prevButton.onClick.RemoveAllListeners(); prevButton.onClick.AddListener(ShowPrev); }
        if (nextButton != null) { nextButton.onClick.RemoveAllListeners(); nextButton.onClick.AddListener(ShowNext); }

        Debug.Log("TutorialUIManager: canvas registered");
    }

    public void ShowTutorialPanel(List<TutorialData> list, int index, bool isFirstTimeView)
    {
        if (list == null || list.Count == 0 || tutorialPanel == null) return;

        currentList = list;
        currentIndex = Mathf.Clamp(index, 0, list.Count - 1);
        tutorialPanel.SetActive(true);

        RenderCurrentPage();

        if (EventSystem.current != null && nextButton != null)
            EventSystem.current.SetSelectedGameObject(nextButton.gameObject);

        if (lockCoroutine != null) StopCoroutine(lockCoroutine);
        if (isFirstTimeView) lockCoroutine = StartCoroutine(LockCloseFor(list[currentIndex].displayDuration));
        else SetLocked(false);
    }

    public void HideTutorial()
    {
        if (lockCoroutine != null) StopCoroutine(lockCoroutine);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }

    public void ShowNext()
    {
        if (currentList == null || currentIndex >= currentList.Count - 1) return;
        currentIndex++;
        RenderCurrentPage();
    }

    public void ShowPrev()
    {
        if (currentList == null || currentIndex <= 0) return;
        currentIndex--;
        RenderCurrentPage();
    }

    private void RenderCurrentPage()
    {
        TutorialData data = currentList[currentIndex];
        if (tutorialImage != null) tutorialImage.sprite = data.tutorialSprite;

        if (prevButton != null) prevButton.interactable = currentIndex > 0;
        if (nextButton != null) nextButton.interactable = currentIndex < currentList.Count - 1;
    }

    private IEnumerator LockCloseFor(float duration)
    {
        SetLocked(true);
        float elapsed = 0f;
        if (closeBar != null)
        {
            closeBar.minValue = 0f;
            closeBar.maxValue = duration;
            closeBar.value = 0f;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (closeBar != null) closeBar.value = elapsed;
            yield return null;
        }

        SetLocked(false);
    }

    private void SetLocked(bool locked)
    {
        isCloseLocked = locked;
        if (closeBar != null) closeBar.gameObject.SetActive(locked);
        if (closeHint != null) closeHint.SetActive(!locked);
    }
}
