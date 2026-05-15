using Fusion;
using UnityEngine;

public class TriggerDialogue : NetworkBehaviour
{
    [Header("Main Settings")]
    [SerializeField] bool isOneTimeTrigger = true;
    private bool hasTriggeredLocal = false;

    [SerializeField] bool isPlayWithBGM;
    [SerializeField] bool isEndWithDialogue;

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
                            RPC_TriggerDialogueNetwork(1, currentIndex);
                        }
                        else
                        {
                            RPC_TriggerDialogueNetwork(2, currentIndex);
                        }
                    }
                    else
                    {
                        RPC_TriggerDialogueNetwork(0, currentIndex);
                    }
                }
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_TriggerDialogueNetwork(int sequenceType, int index)
    {
        DialogueConfig[] selectedConfig = null;

        if (isPlayWithBGM)
        {
            AudioManager.Instance.PlayBGM("");
        }

        if (sequenceType == 0) selectedConfig = dialogueSequence;
        else if (sequenceType == 1) selectedConfig = birdDialogueSequence;
        else if (sequenceType == 2) selectedConfig = duckDialogueSequence;

        if (selectedConfig != null && index < selectedConfig.Length)
        {
            DialogueManager.Instance.StartDialogue(selectedConfig[index]);
            currentIndex = index + 1;
            hasTriggeredLocal = true;
        }
        else
        {
            if (isEndWithDialogue)
            {
                AudioManager.Instance.StopBGM();
            }
            Debug.Log("Dialogue End");
        }
    }
}