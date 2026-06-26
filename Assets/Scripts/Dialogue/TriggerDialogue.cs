using Fusion;
using UnityEngine;

public class TriggerDialogue : NetworkBehaviour
{
    [Header("Main Settings")]
    [SerializeField] bool isOneTimeTrigger = true;
    private bool hasTriggeredLocal = false;

    [Header("Dialogue Type")]
    [SerializeField] bool isSubDialogue = false;

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

        if (!other.CompareTag("Player")) return;
        MovementCharacter character = other.GetComponent<Bird_Moveset>() as MovementCharacter
                                   ?? other.GetComponent<Duck_Moveset>() as MovementCharacter;
        if (character != null)
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
                int sequenceType = character.isBird ? 1 : 2;
                int idx = character.isBird ? birdIndex : duckIndex;
                if (isSubDialogue) RPC_TriggerSubDialogueNetwork(sequenceType, idx);
                else RPC_TriggerDialogueNetwork(sequenceType, idx);
            }
            else
            {
                if (isSubDialogue) RPC_TriggerSubDialogueNetwork(0, normalIndex);
                else RPC_TriggerDialogueNetwork(0, normalIndex);
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

    // ใช้ชั่วคราวสำหรับการทริกเกอร์จากข้างนอก
    public void TriggerFromExternal(MovementCharacter character)
    {
        if (isOneTimeTrigger && hasTriggeredLocal) return;

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
            int sequenceType = character.isBird ? 1 : 2;
            int idx = character.isBird ? birdIndex : duckIndex;
            if (isSubDialogue) RPC_TriggerSubDialogueNetwork(sequenceType, idx);
            else RPC_TriggerDialogueNetwork(sequenceType, idx);
        }
        else
        {
            if (isSubDialogue) RPC_TriggerSubDialogueNetwork(0, normalIndex);
            else RPC_TriggerDialogueNetwork(0, normalIndex);
        }
    }


    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_UnlockSkillsBoth(MovementCharacter triggerer)
    {
        MovementCharacter[] allPlayers = FindObjectsByType<MovementCharacter>(FindObjectsSortMode.None);
        foreach (var p in allPlayers)
        {
            p.RPC_UnlockSkill(skillToUnlock);
            if (skillToUnlock2 != MovementCharacter.SkillType.None)
                p.RPC_UnlockSkill(skillToUnlock2);
        }
        PersistSkillUnlock(skillToUnlock);
        if (skillToUnlock2 != MovementCharacter.SkillType.None)
            PersistSkillUnlock(skillToUnlock2);
    }

    private DialogueConfig[] ResolveSequence(int sequenceType)
    {
        if (sequenceType == 0) return dialogueSequence;
        if (sequenceType == 1) return birdDialogueSequence;
        if (sequenceType == 2) return duckDialogueSequence;
        return null;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_TriggerDialogueNetwork(int sequenceType, int index)
    {
        if (isPlayWithBGM)
        {
            //AudioManager.Instance.PlayBGM("DialogueTheme");
        }

        DialogueConfig[] selectedConfig = ResolveSequence(sequenceType);

        if (selectedConfig != null && index < selectedConfig.Length)
        {
            DialogueManager.Instance.StartDialogueSequence(selectedConfig);
            DialogueVoteManager.Instance?.StartVoteSession();

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
                //AudioManager.Instance.StopBGM();
            }
            Debug.Log("Dialogue End");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_TriggerSubDialogueNetwork(int sequenceType, int index)
    {
        DialogueConfig[] selectedConfig = ResolveSequence(sequenceType);

        if (selectedConfig != null && index < selectedConfig.Length)
        {
            ShowSubDialogueOnLocalPlayer(selectedConfig);

            if (sequenceType == 0) normalIndex = index + 1;
            else if (sequenceType == 1) birdIndex = index + 1;
            else if (sequenceType == 2) duckIndex = index + 1;

            isOneTimeTrigger = true;
            hasTriggeredLocal = true;
        }
    }

    private void ShowSubDialogueOnLocalPlayer(DialogueConfig[] sequence)
    {
        // ส่งให้ MiniDialogueManager คุมจังหวะ/ลำดับสลับ speaker เอง (รันบนทุก client เหมือนกัน
        // เพราะ RPC เป็น RpcTargets.All) — Manager จะหาตัวละครที่ชื่อตรงกับ Speaker ของแต่ละบรรทัด
        // แล้วโชว์ popup เหนือหัวตัวละครนั้นจริงๆ ไม่ใช่โชว์ทั้ง sequence บนตัวละครของผู้กดอย่างเดียว
        MiniDialogueManager.Instance?.PlayMiniDialogueSequence(sequence);
    }
}