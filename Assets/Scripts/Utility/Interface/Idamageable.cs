using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int dmg, float knockbackForce, Vector2 vec);
}
