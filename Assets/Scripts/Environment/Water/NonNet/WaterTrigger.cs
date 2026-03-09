using UnityEngine;

public class WaterTrigger : MonoBehaviour
{
    [Tooltip("Reference to the Water script")]
    public InteractableWater water;

    [Tooltip("Layers that trigger a splash (Player, PhysicsObject, etc)")]
    public LayerMask hitLayers;

    [Tooltip("Visual particles (Optional)")]
    public ParticleSystem splashParticles;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & hitLayers) == 0) return;

        Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float fallVelocity = rb.linearVelocityY;

            float impactForce = fallVelocity * rb.mass;

            float clampedVelocity = Mathf.Clamp(impactForce, -10f, -1f);

            water.Splash(collision.transform.position, clampedVelocity);

            if (splashParticles != null)
            {
                Instantiate(splashParticles, collision.transform.position, Quaternion.identity);
            }
        }
    }
}