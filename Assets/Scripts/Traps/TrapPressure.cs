using UnityEngine;

public class TrapPressure : MonoBehaviour
{
    [Header("Stat")]
    [SerializeField] float pushForce = 3.3f;

    [SerializeField] bool _isActive = true;
    [SerializeField] Vector2 currentDirection;
    [SerializeField] Vector2 defaultDirection;

    private void Awake()
    {
        currentDirection = transform.up;
        defaultDirection = currentDirection;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!_isActive) return;

        if (collision.CompareTag("Player"))
        {
            Rigidbody2D playerRb = collision.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Debug.Log("Push");
                playerRb.AddForce(currentDirection.normalized * pushForce, ForceMode2D.Force);
            }
        }
    }

    public void SetTrapActive(bool active)
    {
        _isActive = active;
    }

    public void ChangeDirection(bool isRevers)
    {
        if (isRevers)
        {
            currentDirection = -defaultDirection;
        }
        else
        {
            currentDirection = defaultDirection;
        }
    }
}
