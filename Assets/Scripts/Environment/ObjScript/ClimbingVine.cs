using UnityEngine;
using Fusion;

public class ClimbingVine : MonoBehaviour, IClimbable
{
    [Header("Climb Bounds")]
    [SerializeField] float topY;
    [SerializeField] float bottomY;
    [SerializeField] float climbUpSpeed = 3f;
    [SerializeField] float climbDownSpeed = 4f;

    public bool TryStartClimb(MovementCharacter player)
    {
        float py = player.transform.position.y;
        if (py < bottomY || py > topY) return false;

        player.StartClimbing();
        return true;
    }

    public void OnClimbTick(MovementCharacter player, NetworkInputData input)
    {
        float py = player.transform.position.y;
        float v = 0f;

        if (input.vertical > 0.1f && py < topY) v = climbUpSpeed;
        else if (input.vertical < -0.1f && py > bottomY) v = -climbDownSpeed;

        player.ApplyClimbVelocity(v);
    }

    public void OnStopClimb(MovementCharacter player)
    {
        player.StopClimbing();
    }
}