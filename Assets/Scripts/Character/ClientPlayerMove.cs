using Fusion;
using UnityEngine;

public class ClientPlayerMove : NetworkBehaviour
{
    [Header("Referent")]
    CharacterStats stats;
    InputControl input;
    CharacterAction action;
    CharacterAnimation cAnimation;
    MovementCharacter movement;

    NetworkRunner runner;

    private void Awake()
    {
        stats.enabled = false;
        input.enabled = false;
        action.enabled = false;
        movement.enabled = false;
        cAnimation.enabled = false;
    }

    public override void Spawned()
    {
        if (runner.IsClient)
        {
            stats.enabled = true;
            input.enabled = true;
            action.enabled = true;
            movement.enabled = true;
            cAnimation.enabled = true;
        }
    }
}
