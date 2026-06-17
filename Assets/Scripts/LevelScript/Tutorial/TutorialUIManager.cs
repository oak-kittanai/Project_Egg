using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialUIManager : MonoBehaviour
{
    public static TutorialUIManager Instance;

    [Header("Tutorial UI Elements")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private Image      backgroundFrame;
    [SerializeField] private Image      tutorialImage;
    [SerializeField] private TMP_Text   headerText;
    [SerializeField] private TMP_Text   descText;
    [SerializeField] private Slider     closeBar;

    private Coroutine _autoCloseCoroutine;

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

    public void RegisterCanvas(GameObject canvas)
    {
        var panel = canvas.transform.Find("TutorialPanel");
        if (panel == null) { Debug.LogWarning("TutorialPanel not found in Canvas"); return; }

        tutorialPanel = panel.gameObject;

        var bg = panel.Find("BackgroundFrame");
        if (bg != null) backgroundFrame = bg.GetComponent<Image>();

        var img = panel.Find("tutorialImage");
        if (img != null) tutorialImage = img.GetComponent<Image>();

        var header = panel.Find("Header");
        if (header != null) headerText = header.GetComponent<TMP_Text>();

        var desc = panel.Find("Desc");
        if (desc != null) descText = desc.GetComponent<TMP_Text>();

        var bar = panel.Find("CloseBar");
        if (bar != null) closeBar = bar.GetComponent<Slider>();

        Debug.Log("TutorialUIManager: canvas registered");
    }

    public void ShowTutorial(TutorialData data)
    {
        if (data == null || tutorialPanel == null) return;

        if (tutorialImage != null)  tutorialImage.sprite = data.tutorialSprite;
        if (headerText != null)     headerText.text      = data.header;
        if (descText != null)       descText.text        = data.desc;

        tutorialPanel.SetActive(true);

        if (_autoCloseCoroutine != null) StopCoroutine(_autoCloseCoroutine);
        _autoCloseCoroutine = StartCoroutine(AutoClose(data.displayDuration));
    }

    public void HideTutorial()
    {
        if (_autoCloseCoroutine != null) StopCoroutine(_autoCloseCoroutine);
        _autoCloseCoroutine = null;

        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }

    private IEnumerator AutoClose(float duration)
    {
        if (duration <= 0f)
        {
            HideTutorial();
            yield break;
        }

        float elapsed = 0f;
        if (closeBar != null) closeBar.value = 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (closeBar != null) closeBar.value = 1f - (elapsed / duration);
            yield return null;
        }

        HideTutorial();
    }
}
