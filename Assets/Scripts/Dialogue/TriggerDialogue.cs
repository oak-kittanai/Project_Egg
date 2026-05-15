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
    private int normalIndex = 0;

    [Header("Character Specific Settings")]
    [SerializeField] bool differentCharacterDialogue;

    [SerializeField] DialogueConfig[] birdDialogueSequence;
    private int birdIndex = 0;

    [SerializeField] DialogueConfig[] duckDialogueSequence;
    private int duckIndex = 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isOneTimeTrigger && hasTriggeredLocal) return;

        MovementCharacter[] allCharacterMovement = other.GetComponents<MovementCharacter>();

        foreach (var character in allCharacterMovement)
        {
            if (character.enabled && other.CompareTag("Player"))
            {
                if (!character.HasInputAuthority) return;

                if (differentCharacterDialogue)
                {
                    if (character.isBird)
                    {
                        RPC_TriggerDialogueNetwork(1, birdIndex);
                    }
                    else
                    {
                        RPC_TriggerDialogueNetwork(2, duckIndex);
                    }
                }
                else
                {
                    RPC_TriggerDialogueNetwork(0, normalIndex);
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
            //AudioManager.Instance.PlayBGM("DialogueTheme");
        }

        if (sequenceType == 0) selectedConfig = dialogueSequence;
        else if (sequenceType == 1) selectedConfig = birdDialogueSequence;
        else if (sequenceType == 2) selectedConfig = duckDialogueSequence;

        if (selectedConfig != null && selectedConfig.Length > 0)
        {
            DialogueManager.Instance.StartDialogueSequence(selectedConfig);

            isOneTimeTrigger = true;
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