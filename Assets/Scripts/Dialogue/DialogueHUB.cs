using UnityEngine;
using UnityEngine.UI;

public class DialogueHUB : MonoBehaviour
{
    public static DialogueHUB Instance { get; private set; }

    [SerializeField] private GameObject dialogueObject;

    [SerializeField] private GameObject miraBox;
    [SerializeField] private GameObject kaelBox;
    [SerializeField] private CustomTextGen miraAnimator;
    [SerializeField] private CustomTextGen kaelAnimator;

    [SerializeField] Button nextButton;
    [SerializeField] Button prevButton;

    private void Awake() => Instance = this;

    private void Start()
    {
        SetButton();
    }

    public void SetButton()
    {
        nextButton.onClick.AddListener(DialogueManager.Instance.NextLine);
        prevButton.onClick.AddListener(DialogueManager.Instance.PreviousLine);
    }

    public void DisplayLine(string speaker, string message, TextEffectType effect)
    {
        dialogueObject.SetActive(true);
        miraBox.SetActive(speaker == "Mira");
        kaelBox.SetActive(speaker == "Kael");

        if (speaker == "Mira") miraAnimator.StartEffect(message, effect);
        else if (speaker == "Kael") kaelAnimator.StartEffect(message, effect);
    }

    public void CloseDialogue()
    {
        dialogueObject.SetActive(false);
        miraBox.SetActive(false);
        kaelBox.SetActive(false);
    }

    private void OnDestroy()
    {
        DesetButton();
    }

    public void DesetButton()
    {
        nextButton.onClick.RemoveAllListeners();
        prevButton.onClick.RemoveAllListeners();
    }
}