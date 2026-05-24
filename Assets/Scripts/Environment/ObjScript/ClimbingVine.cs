using UnityEngine;
using Fusion;

public class ClimbingVine : NetworkBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!HasStateAuthority) return;

        MovementCharacter[] all = other.GetComponents<MovementCharacter>();
        foreach (var c in all)
        {
            if (!c.enabled) continue;

            if (!c.isClimbing && c.MoveInput.y > 0.1f)
            {
                if (c is IClimbable climber)
                {
                    climber.StartClimbing();
                    Debug.Log("Player started climbing");
                }
            }
            break;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!HasStateAuthority) return;

        MovementCharacter[] all = other.GetComponents<MovementCharacter>();
        foreach (var c in all)
        {
            if (!c.enabled) continue;

            if (c.isClimbing && c is IClimbable climber)
            {
                climber.StopClimbing();
                Debug.Log("Player exited vine — stop climbing");
            }
            break;
        }
    }
}