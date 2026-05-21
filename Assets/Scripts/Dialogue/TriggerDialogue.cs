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

    [Header("Skill Unlock Settings")]
    [SerializeField] MovementCharacter.SkillType skillToUnlock = MovementCharacter.SkillType.None;
    [SerializeField] MovementCharacter.SkillType skillToUnlock2 = MovementCharacter.SkillType.None;

    [SerializeField] bool doubleCharacterSkillUnlock;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!HasStateAuthority) return;
        if (isOneTimeTrigger && hasTriggeredLocal) return;

        MovementCharacter[] allCharacterMovement = other.GetComponents<MovementCharacter>();

        foreach (var character in allCharacterMovement)
        {
            if (character.enabled && other.CompareTag("Player"))
            {
                if (skillToUnlock != MovementCharacter.SkillType.None)
                {
                    if (doubleCharacterSkillUnlock)
                    {
                        RPC_UnlockSkillsBoth(character);
                        PersistSkillUnlock(skillToUnlock);
                        PersistSkillUnlock(skillToUnlock2);
                    }
                    else
                    {
                        character.RPC_UnlockSkill(skillToUnlock);
                        PersistSkillUnlock(skillToUnlock);
                    }
                }

                if (differentCharacterDialogue)
                {
                    if (character.isBird) RPC_TriggerDialogueNetwork(1, birdIndex);
                    else RPC_TriggerDialogueNetwork(2, duckIndex);
                }
                else
                {
                    RPC_TriggerDialogueNetwork(0, normalIndex);
                }
                break;
            }
        }
    }

    private void PersistSkillUnlock(MovementCharacter.SkillType skill)
    {
        if (GameManager.Instance == null) return;

        switch (skill)
        {
            case MovementCharacter.SkillType.Bird_Fly: GameManager.Instance.UnlockSkill_BirdFly(); break;
            case MovementCharacter.SkillType.Bird_Throw: GameManager.Instance.UnlockSkill_BirdThrow(); break;
            case MovementCharacter.SkillType.Duck_Dive: GameManager.Instance.UnlockSkill_DuckDive(); break;
            case MovementCharacter.SkillType.Duck_Smash: GameManager.Instance.UnlockSkill_DuckSmash(); break;
        }
    }


    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_UnlockSkillsBoth(MovementCharacter triggerer)
    {
        MovementCharacter[] allPlayers = FindObjectsByType<MovementCharacter>(FindObjectsSortMode.None);
        foreach (var p in allPlayers)
        {
            if (p.enabled)
            {
                p.RPC_UnlockSkill(skillToUnlock);
                if (skillToUnlock2 != MovementCharacter.SkillType.None)
                    p.RPC_UnlockSkill(skillToUnlock2);
            }
        }
        PersistSkillUnlock(skillToUnlock);
        if (skillToUnlock2 != MovementCharacter.SkillType.None)
            PersistSkillUnlock(skillToUnlock2);
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

        if (selectedConfig != null && index < selectedConfig.Length)
        {
            DialogueManager.Instance.StartDialogueSequence(selectedConfig);

            if (sequenceType == 0) normalIndex = index + 1;
            else if (sequenceType == 1) birdIndex = index + 1;
            else if (sequenceType == 2) duckIndex = index + 1;

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