using Fusion;
using UnityEngine;

public class SkillUnlockTrigger : NetworkBehaviour
{
    [SerializeField] SkillUnlockType skillToUnlock;

    [Networked] private NetworkBool hasTriggered { get; set; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!HasStateAuthority) return;
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        MovementCharacter[] allCharacters = other.GetComponents<MovementCharacter>();
        foreach (var character in allCharacters)
        {
            if (!character.enabled) continue;

            bool isBird = character is Bird_Moveset;
            bool isDuck = character is Duck_Moveset;

            switch (skillToUnlock)
            {
                case SkillUnlockType.BirdFly:
                    if (isBird) RPC_UnlockSkill(0); break;

                case SkillUnlockType.BirdThrow:
                    if (isBird) RPC_UnlockSkill(1); break;

                case SkillUnlockType.DuckDive:
                    if (isDuck) RPC_UnlockSkill(2); break;

                case SkillUnlockType.DuckSmash:
                    if (isDuck) RPC_UnlockSkill(3); break;

                case SkillUnlockType.AnyPlayerBird:
                    RPC_UnlockSkill(0); break;
            }

            hasTriggered = true;
            break;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UnlockSkill(int skillIndex)
    {
        switch (skillIndex)
        {
            case 0: PlayerInterface.Instance?.UnlockBirdFly(); break;
            case 1: PlayerInterface.Instance?.UnlockBirdThrow(); break;
            case 2: PlayerInterface.Instance?.UnlockDuckDive(); break;
            case 3: PlayerInterface.Instance?.UnlockDuckSmash(); break;
        }
    }
}

public enum SkillUnlockType
{
    BirdFly,
    BirdThrow,
    DuckDive,
    DuckSmash,
    AnyPlayerBird
}