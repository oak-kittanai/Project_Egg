using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

public class MoveableRock : NetworkBehaviour , MoveableObject
{
    [SerializeField] float knockbackForce;
    [SerializeField] Rigidbody2D rb2D;
    [SerializeField] NetworkRigidbody2D netRb2D;

    [Header("Stats")]
    [Networked] public Vector2 seltTrans {  get; set; }

    private void Awake()
    {
        Setup();
    }

    private void Setup()
    {
        rb2D = GetComponent<Rigidbody2D>();
        netRb2D = GetComponent<NetworkRigidbody2D>();
    }

    public void MoveInteract(Vector2 pos)
    {
        if (HasStateAuthority)
        {
            seltTrans = new Vector2(transform.position.x, transform.position.y);
            Vector2 direction = (pos - seltTrans).normalized;
            Vector2 knockbackDir = -direction * knockbackForce;

            Debug.Log("coll pos is : " + pos);
            Debug.Log("transform pos is : " + transform.position);

            rb2D.AddForce(knockbackDir, ForceMode2D.Force);
        }
    }
}
