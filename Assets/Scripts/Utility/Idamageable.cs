using UnityEngine;

interface IDamageable
{
    void TakeDamage(int dmg, float knockbackForce, Collision2D coll);
}
