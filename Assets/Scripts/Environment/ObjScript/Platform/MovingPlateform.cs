using Fusion;
using UnityEngine;

public class MovingPlateform : NetworkBehaviour
{
    [Header("Movement State")]
    [SerializeField] float speed = 1.5f;
    [SerializeField] float distance = 4f;
    [SerializeField] bool isVertical;

    private Vector3 startPosition;
    private Rigidbody2D rb;
    private BoxCollider2D boxCol;

    public override void Spawned()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
        boxCol = GetComponent<BoxCollider2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
    }

    public override void FixedUpdateNetwork()
    {
        float sineValue = (Mathf.Sin((float)Runner.SimulationTime * speed) + 1f) / 2f;
        float movementOffset = sineValue * distance;

        Vector2 targetPosition = startPosition;
        if (isVertical) targetPosition.y += movementOffset;
        else targetPosition.x += movementOffset;

        Vector2 deltaMovement = targetPosition - rb.position;

        rb.position = targetPosition;

        if (boxCol != null)
        {
            Vector2 checkPos = rb.position + boxCol.offset + new Vector2(0, (boxCol.size.y / 2f) + 0.1f);

            Vector2 checkSize = new Vector2(boxCol.size.x * 0.95f, 0.2f);

            Collider2D[] hitResults = new Collider2D[10];
            int hitCount = Runner.GetPhysicsScene2D().OverlapBox(checkPos, checkSize, 0, hitResults, LayerMask.GetMask("Player"));

            for (int i = 0; i < hitCount; i++)
            {
                if (hitResults[i] != null && hitResults[i].TryGetComponent<MovementCharacter>(out var player))
                {
                    if (player.rb2D != null)
                    {
                        player.rb2D.position += deltaMovement;
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

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
            Vector2 pos = Application.isPlaying ? rb.position : (Vector2)transform.position;
            Vector2 checkPos = pos + col.offset + new Vector2(0, (col.size.y / 2f) + 0.1f);
            Vector2 checkSize = new Vector2(col.size.x * 0.95f, 0.2f);
            Gizmos.DrawCube(checkPos, checkSize);
        }
    }
}