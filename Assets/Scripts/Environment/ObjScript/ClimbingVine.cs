using UnityEngine;
using Fusion;

[RequireComponent(typeof(Collider2D))]
public class ClimbingVine : MonoBehaviour, IClimbable
{
    [Header("Climb Speed")]
    [SerializeField] float climbSideSpeed = 2f;

    [SerializeField] float climbUpSpeed = 3f;
    [SerializeField] float climbDownSpeed = 4f;

    private Collider2D climbCollider;

    [SerializeField] float climbMargin = 0.5f;

    private float MinY => climbCollider.bounds.min.y - climbMargin;
    private float MaxY => climbCollider.bounds.max.y + climbMargin;

    private void Awake()
    {
        climbCollider = GetComponent<Collider2D>();
    }

    public bool TryStartClimb(MovementCharacter player)
    {
        float py = player.transform.position.y;
        Debug.Log($"[Vine] player.y={py:F2}, range={MinY:F2}~{MaxY:F2}");

        if (py < MinY || py > MaxY)
        {
            Debug.Log("[Vine] ❌ player outside");
            return false;
        }

        player.StartClimbing();
        Debug.Log("[Vine] ✓ Start climbing");
        return true;
    }

    public void OnClimbTick(MovementCharacter player, NetworkInputData input)
    {
        float py = player.transform.position.y;

        float vy = 0f;
        if (input.vertical > 0.1f && py < MaxY) vy = climbUpSpeed;
        else if (input.vertical < -0.1f && py > MinY) vy = -climbDownSpeed;

        float vx = input.horizontal * climbSideSpeed;

        player.ApplyClimbVelocity(vx, vy);
    }

    public void OnStopClimb(MovementCharacter player)
    {
        player.StopClimbing();
    }
}