using UnityEngine;
using Fusion;

public class NetworkWaterTrigger : NetworkBehaviour
{
    [Tooltip("Reference to the Water script")]
    public NetworkInteractableWater water;

    [Tooltip("Layers that trigger a splash (Player, PhysicsObject, etc)")]
    public LayerMask hitLayers;

    [Tooltip("Visual particles (Optional)")]
    public ParticleSystem splashParticles;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & hitLayers) == 0) return;

        NetworkObject netObj = collision.GetComponentInParent<NetworkObject>();

        if (netObj != null && netObj.HasStateAuthority)
        {
            Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float velocity = rb.velocity.y * rb.mass;

                water.RPC_Splash(collision.transform.position, velocity);

                if (splashParticles != null)
                {
                    Instantiate(splashParticles, collision.transform.position, Quaternion.identity);
                }
            }
        }
    }
}