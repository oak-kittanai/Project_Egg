using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialUIManager : MonoBehaviour
{
    public static TutorialUIManager Instance;

    [Header("UI Elements")]
    public GameObject tutorialPanel;
    public Image tutorialImageSlot;

    private Coroutine hideCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject.transform.root.gameObject);

            GameObject canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                RegisterCanvas(canvas);
            }
        }
        else
        {
            Destroy(gameObject.transform.root.gameObject);
        }
    }

    public void RegisterCanvas(GameObject canvas)
    {
        var panel = canvas.transform.Find("TutorialPanel");
        if (panel == null) { Debug.LogWarning("TutorialPanel not found"); return; }

        tutorialPanel = panel.gameObject;
        tutorialImageSlot = tutorialPanel.transform.Find("TutorialSlot").GetComponent<Image>();
        Debug.Log("TutorialManager Success Load UI");
    }

    public void ShowTutorial(Sprite spriteToShow, Vector2 customSize, float duration = 5f)
    {
        if (spriteToShow != null && tutorialPanel != null)
        {
            if (hideCoroutine != null) StopCoroutine(hideCoroutine);

            tutorialImageSlot.sprite = spriteToShow;

            RectTransform imgRect = tutorialImageSlot.GetComponent<RectTransform>();
            if (imgRect != null)
            {
                imgRect.sizeDelta = customSize;
            }

            tutorialPanel.SetActive(true);

            hideCoroutine = StartCoroutine(HideAfterDelay(duration));
        }
    }

    public void HideTutorial()
    {
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);

        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        hideCoroutine = null;
    }
}