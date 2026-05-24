using Fusion;
using UnityEngine;

public class ClimbingVine : NetworkBehaviour
{
    private void OnTriggerStay2D(Collider2D other)
    {
        Debug.Log("Player has enter");
        if (!HasStateAuthority) return;

        MovementCharacter[] all = other.GetComponents<MovementCharacter>();
        foreach (var c in all)
        {
            if (!c.enabled) continue;

            if (!c.isClimbing && Mathf.Abs(c.MoveInput.y) > 0.1f)
            {
                if (c is IClimbable climber)
                    climber.StartClimbing(); Debug.Log("Player try to climb");
            }
            break;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log("Player has exit");
        if (!HasStateAuthority) return;

        MovementCharacter[] all = other.GetComponents<MovementCharacter>();
        foreach (var c in all)
        {
            if (!c.enabled) continue;
            if (c.isClimbing && c is IClimbable climber)
                climber.StopClimbing(); Debug.Log("Player try to exit climb");
            break;
        }
    }
}