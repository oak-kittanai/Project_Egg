using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
            if (tutorialPanel != null) tutorialPanel.SetActive(false);

            DontDestroyOnLoad(gameObject.transform.root.gameObject);
        }
        else
        {
            Destroy(gameObject.transform.root.gameObject);
        }
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