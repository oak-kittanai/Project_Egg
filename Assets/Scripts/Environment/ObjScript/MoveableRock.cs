using UnityEngine;

public class MoveableRock : MonoBehaviour , MoveableObject
{
    [SerializeField] float knockbackForce;
    [SerializeField] Rigidbody2D rb2D;

    private void Start()
    {
        Setup();
    }

    private void Setup()
    {
        rb2D = GetComponent<Rigidbody2D>();
    }

    public void MoveInteract(Vector2 pos)
    {
        Vector2 self = new Vector2(transform.position.x, transform.position.y);
        Vector2 direction = (pos - self).normalized;
        Vector2 knockbackDir = -direction * knockbackForce;

        Debug.Log("coll pos is : " + pos);
        Debug.Log("transform pos is : " + transform.position);

        rb2D.AddForce(knockbackDir, ForceMode2D.Force);
    }
}
