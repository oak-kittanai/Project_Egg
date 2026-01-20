using Fusion;
using UnityEngine;

public class CharacterStats : NetworkBehaviour, IDamageable
{
    [Header("Ref")]
    private Rigidbody2D rb2D;

    [Header("Networked Stats")]
    [Networked] public int CurrentHealth { get; set; }

    [Header("Base Config")]
    public int s_maxHealth = 5;
    public float s_maxStamina = 30f;

    [Header("Movement Config")]
    public float s_walkSpeed = 10f;
    public float maxSpeed = 20f;
    public float s_jumpForce = 12f;
    public float acceleration = 5f;
    public float deceleration = 5f;

    public float s_flySpeed = 8f;

    [Header("Identity")]
    [Networked] public characterType skinType { get; set; }

    public override void Spawned()
    {
        rb2D = GetComponent<Rigidbody2D>();

        if (Object.HasStateAuthority)
        {
            CurrentHealth = s_maxHealth;
        }
    }

    public void TakeDamage(int dmg, float knockbackForce, Vector2 vec)
    {
        if (Object.HasStateAuthority)
        {
            CurrentHealth -= dmg;
            Debug.Log($"HP: {CurrentHealth}");

            if (rb2D != null)
            {
                Vector2 pushDirection = ((Vector2)transform.position - vec).normalized;

                rb2D.AddForce(pushDirection * knockbackForce, ForceMode2D.Impulse);
            }
        }
    }
}