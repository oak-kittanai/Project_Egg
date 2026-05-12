using Fusion;
using UnityEngine;

public class TriggerDialogue : NetworkBehaviour
{
    [Header("Main Settings")]
    [SerializeField] bool isOneTimeTrigger = true;
    private bool hasTriggeredLocal = false;

    [SerializeField] DialogueConfig[] dialogueSequence;
    private int currentIndex = 0;

    [Header("Character Specific Settings")]
    [SerializeField] bool differentCharacterDialogue;

    [SerializeField] DialogueConfig[] birdDialogueSequence;
    [SerializeField] DialogueConfig[] duckDialogueSequence;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isOneTimeTrigger && hasTriggeredLocal) return;

        MovementCharacter[] allCharacterMovement = other.GetComponents<MovementCharacter>();

        foreach (var character in allCharacterMovement)
        {
            if (character.enabled)
            {
                if (other.CompareTag("Player"))
                {
                    if (!character.HasInputAuthority) return;

                    if (differentCharacterDialogue)
                    {
                        if (character.isBird)
                        {
                            UpdateCurrentDialogue(birdDialogueSequence);
                        }
                        else
                        {
                            UpdateCurrentDialogue(duckDialogueSequence);
                        }
                    }
                    else
                    {
                        UpdateCurrentDialogue(dialogueSequence);
                    }
                }
            }
        }
    }

    private void UpdateCurrentDialogue(DialogueConfig[] config)
    {
        if (currentIndex < config.Length)
        {
            DialogueManager.Instance.StartDialogue(config[currentIndex]);
            currentIndex++;
            hasTriggeredLocal = true;
        }
        else
        {
            Debug.Log("Dialogue End");
        }
    }
}