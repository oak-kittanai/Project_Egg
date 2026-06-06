using Fusion;
using UnityEngine;

public class CheckpointTrigger : NetworkBehaviour
{
    [Header("Checkpoint")]
    [SerializeField] private Transform customSpawnPoint;
    [Networked] private NetworkBool isActivated { get; set; }

    [Header("Vis&Audi")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Sprite activeSprite;

    [SerializeField] private ParticleSystem activationParticles;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip activateSoundClip;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!HasStateAuthority || isActivated) return;

        if (collision.CompareTag("Player"))
        {
            isActivated = true;

            Vector3 newSpawnPosition = customSpawnPoint != null ? customSpawnPoint.position : transform.position;
            GameManager.Instance.UpdateRespawnPos(newSpawnPosition);
            RPC_PlayCheckpointEffects();
        }
    }

    public override void Render()
    {
        if (isActivated && sr != null && activeSprite != null)
        {
            sr.sprite = activeSprite;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayCheckpointEffects()
    {
        if (activationParticles != null)
        {
            activationParticles.Play();
        }

        if (audioSource != null && activateSoundClip != null)
        {
            audioSource.PlayOneShot(activateSoundClip);
        }
    }
}