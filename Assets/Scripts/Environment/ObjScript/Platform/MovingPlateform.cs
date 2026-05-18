using Fusion;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlateform : NetworkBehaviour
{
    [Header("Movement State")]
    [SerializeField] float speed = 1.5f;
    [SerializeField] float distance = 4f;
    [SerializeField] bool isVertical;

    private Vector3 startPosition;
    private Rigidbody2D rb;

    public override void Spawned()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        float sineValue = (Mathf.Sin((float)Runner.SimulationTime * speed) + 1f) / 2f;
        float movementOffset = sineValue * distance;

        Vector3 newPosition = startPosition;

        if (isVertical)
        {
            newPosition.y += movementOffset;
        }
        else
        {
            newPosition.x += movementOffset;
        }

        rb.MovePosition(newPosition);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 currentStart = Application.isPlaying ? startPosition : transform.position;
        Vector3 endPosition = currentStart;

        if (isVertical)
        {
            endPosition.y += distance;
        }
        else
        {
            endPosition.x += distance;
        }
        Gizmos.color = Color.red;
        Gizmos.DrawLine(currentStart, endPosition);
        Gizmos.DrawSphere(currentStart, 0.1f);
        Gizmos.DrawSphere(endPosition, 0.1f);
    }
}