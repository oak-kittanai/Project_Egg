using Fusion;
using UnityEngine;

public class CheckpointTrigger : NetworkBehaviour
{
    [Header("Checkpoint Settings")]
    [SerializeField] private Transform customSpawnPoint;

    [Networked] private NetworkBool isActivated { get; set; }

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Sprite activeSprite;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!HasStateAuthority || isActivated) return;

        if (collision.CompareTag("Player"))
        {
            isActivated = true;

            Vector3 newSpawnPosition = customSpawnPoint != null ? customSpawnPoint.position : transform.position;

            GameManager.Instance.UpdateRespawnPos(newSpawnPosition);
        }
    }

    public override void Render()
    {
        if (isActivated && sr != null && activeSprite != null)
        {
            sr.sprite = activeSprite;
        }
    }
}