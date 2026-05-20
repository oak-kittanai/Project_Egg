using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class MovingPlateform : NetworkBehaviour
{
    [SerializeField] float speed = 1.5f;
    [SerializeField] float distance = 4f;
    [SerializeField] bool isVertical;

    [Header("Player Detect")]
    [SerializeField] private Vector2 boxSize = new Vector2(2f, 0.5f);
    [SerializeField] private Vector2 boxOffset = new Vector2(0f, 0.5f);

    private Vector3 startPosition;
    private Rigidbody2D rb;

    private Collider2D[] hitResults = new Collider2D[10];

    public override void Spawned()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
    }

    public override void FixedUpdateNetwork()
    {
        float sineValue = (Mathf.Sin((float)Runner.SimulationTime * speed) + 1f) / 2f;
        float movementOffset = sineValue * distance;

        Vector3 newPosition = startPosition;

        if (isVertical) newPosition.y += movementOffset;
        else newPosition.x += movementOffset;

        Vector3 deltaMovement = newPosition - transform.position;

        rb.MovePosition(newPosition);

        Vector2 checkPos = (Vector2)transform.position + boxOffset;

        int hitCount = Runner.GetPhysicsScene2D().OverlapBox(
            checkPos,
            boxSize,
            0, 
            hitResults,
            LayerMask.GetMask("Player")
        );

        List<MovementCharacter> processedPlayers = new List<MovementCharacter>();

        for (int i = 0; i < hitCount; i++)
        {
            var hit = hitResults[i];
            if (hit != null && hit.TryGetComponent<MovementCharacter>(out var player))
            {
                if (processedPlayers.Contains(player)) continue;
                processedPlayers.Add(player);

                if (player.rb2D != null)
                {
                    player.rb2D.position += (Vector2)deltaMovement;

                    if (isVertical && deltaMovement.y < 0)
                    {
                        Vector2 vel = player.rb2D.linearVelocity;
                        if (vel.y > 0) vel.y = 0;
                        player.rb2D.linearVelocity = vel;
                    }
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 currentStart = Application.isPlaying ? startPosition : transform.position;
        Vector3 endPosition = currentStart;

        if (isVertical) endPosition.y += distance;
        else endPosition.x += distance;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(currentStart, endPosition);
        Gizmos.DrawSphere(currentStart, 0.1f);
        Gizmos.DrawSphere(endPosition, 0.1f);

        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
        Vector3 gizmoPos = Application.isPlaying ? transform.position + (Vector3)boxOffset : currentStart + (Vector3)boxOffset;
        Gizmos.DrawCube(gizmoPos, boxSize);
    }
}