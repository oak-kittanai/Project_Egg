using UnityEngine;

public class TriggerDialogue : MonoBehaviour
{
    [SerializeField] private DialogueConfig[] dialogueSequence;
    private int currentIndex = 0;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && currentIndex < dialogueSequence.Length)
        {
            DialogueManager.Instance.StartDialogue(dialogueSequence[currentIndex]);
            currentIndex++;
        }
    }
}