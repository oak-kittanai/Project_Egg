using UnityEngine;

public class DialogueHUB : MonoBehaviour
{
    public static DialogueHUB Instance { get; private set; }

    [SerializeField] private GameObject miraBox;
    [SerializeField] private GameObject kaelBox;
    [SerializeField] private CustomTextGen miraAnimator;
    [SerializeField] private CustomTextGen kaelAnimator;

    private void Awake() => Instance = this;

    public void DisplayLine(string speaker, string message, TextEffectType effect)
    {
        miraBox.SetActive(speaker == "Mira");
        kaelBox.SetActive(speaker == "Kael");

        if (speaker == "Mira") miraAnimator.StartEffect(message, effect);
        else if (speaker == "Kael") kaelAnimator.StartEffect(message, effect);
    }

    public void CloseDialogue()
    {
        miraBox.SetActive(false);
        kaelBox.SetActive(false);
    }
}