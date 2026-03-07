using UnityEngine;

public class TrapPressure : MonoBehaviour
{
    [Header("Stat")]
    [SerializeField] float pushForce = 3.3f;

    [SerializeField] bool _isActive = true;
    [SerializeField] bool _isRevers = false;
    [SerializeField] Vector2 currentDirection;
    [SerializeField] Vector2 defaultDirection;

    [Header("Visuals")]
    [SerializeField] Animator anim;

    public void Awake()
    {
        currentDirection = transform.up;
        defaultDirection = currentDirection;

        if (anim == null) anim = GetComponent<Animator>();
    }

    private void Start()
    {
        // สั่งให้เริ่มทำงานตามค่าที่ตั้งไว้ใน Inspector ทันทีที่เริ่มเกม
        ChangeDirection(_isRevers);
    }

    public void OnTriggerStay2D(Collider2D collision)
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
            _isRevers = true;
            currentDirection = -defaultDirection;
            if (anim != null) anim.SetBool("isPulling", true);
        }
        else
        {
            _isRevers = false;
            currentDirection = defaultDirection;
            if (anim != null) anim.SetBool("isPulling", false);
        }

        // Animation Update
        if (anim != null)
        {
            anim.SetBool("isReversed", isRevers);
        }
    }
}
