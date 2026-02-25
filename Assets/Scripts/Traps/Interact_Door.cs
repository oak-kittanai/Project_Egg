using UnityEngine;

public class Interact_Door : MonoBehaviour
{
    [Header("Movement Settings")]
    [Range(0.0f, 100f)]
    [SerializeField] float speed;
    [Range(0.0f, 100f)]
    [SerializeField] float distance;
    [SerializeField] bool isVertical = true;

    private Vector3 startPosition;
    private Vector3 endPosition;
    private Vector3 targetPosition;

    void Start()
    {
        startPosition = transform.position;

        if (isVertical)
            endPosition = startPosition + new Vector3(0, distance, 0);
        else
            endPosition = startPosition + new Vector3(distance, 0, 0);

        targetPosition = startPosition;
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
    }

    public void SetDoorState(bool open)
    {
        targetPosition = open ? endPosition : startPosition;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            collision.transform.SetParent(transform);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            collision.transform.SetParent(null);
    }
}