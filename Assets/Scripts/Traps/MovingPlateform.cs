using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class MovingPlateform : MonoBehaviour
{
    [Header("Movement State")]
    [SerializeField] float speed = 1.5f;
    [SerializeField] float distance = 4f;

    [SerializeField] bool isVertical = false;
    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        float movementOffset = Mathf.PingPong(Time.time * speed, distance);
        Vector3 newPosition = startPosition;

        if (isVertical)
        {
            newPosition.y += movementOffset;
        }
        else
        {
            newPosition.x += movementOffset;
        }

        transform.position = newPosition;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {

        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
        }
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(currentStart, endPosition);
        Gizmos.DrawSphere(currentStart, 0.1f);
        Gizmos.DrawSphere(endPosition, 0.1f);
    }
}
