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
        Instance = this;
        tutorialPanel.SetActive(false);
    }

    public void ShowTutorial(Sprite spriteToShow, Vector2 customSize, float duration = 5f)
    {
        if (spriteToShow != null)
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
        tutorialPanel.SetActive(false);
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        tutorialPanel.SetActive(false);
        hideCoroutine = null;
    }
}