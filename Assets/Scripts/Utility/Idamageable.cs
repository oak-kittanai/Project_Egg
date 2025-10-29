using UnityEngine;

interface IDamageable
{
    void TakeDamage(int dmg, float knockbackForce, Collision2D coll);

    void TakeDamage(int dmg, float knockbackForce, Vector2 vec);
}
