using UnityEngine;
using Fusion;

public class NetworkWaterTrigger : NetworkBehaviour
{
    [Tooltip("Reference to the Water script")]
    public NetworkInteractableWater water;

    [Tooltip("Layers that trigger a splash (Player, PhysicsObject, etc)")]
    public LayerMask hitLayers;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (Runner == null || !Runner.IsRunning) return;
        if (Object == null || !Object.IsValid) return;
        if (!HasStateAuthority) return;
        if (collision == null) return;
        if (water == null) return;

        if (((1 << collision.gameObject.layer) & hitLayers) == 0) return;

        Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float fallVelocity = rb.linearVelocity.y;
            float impactForce = fallVelocity * rb.mass;

            float clampedVelocity = Mathf.Clamp(impactForce, -10f, -1f);

            water.Splash(collision.transform.position, clampedVelocity);
        }
    }
}