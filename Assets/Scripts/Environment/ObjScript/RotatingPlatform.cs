using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class RotatingPlatform : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("ความเร็วในการหมุน")]
    [SerializeField] float rotationSpeed = 100.0f;
    [SerializeField] bool alwaysRotating = false;

    [SerializeField] bool rotateRevert = true;
    public bool isStanding = false;
    [SerializeField] private int playersCount = 0;

    private void Update()
    {
        // มีคนเหยียบหรือติ๊กเปิดหมุนตลอด
        if (alwaysRotating || playersCount > 0)
        {
            RotationActive();
        }
    }

    private void RotationActive()
    {
        // ทิศ
        float direction = rotateRevert ? -1f : 1f;

        transform.Rotate(0, 0, direction * rotationSpeed * Time.deltaTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        isStanding = true;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.transform.position.y > transform.position.y)
            {
                playersCount++;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        isStanding = false;

        if (collision.gameObject.CompareTag("Player"))
        {
            playersCount = Mathf.Max(0, playersCount - 1);
        }
    }
}