using UnityEngine;

public class TrapPressure : MonoBehaviour
{
    [Header("Stat")]
    [SerializeField] float pushForce = 3.3f;

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Rigidbody2D playerRb = collision.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Debug.Log("Push");
                playerRb.AddForce(transform.up*pushForce, ForceMode2D.Impulse);
            }
        }
    }
}
