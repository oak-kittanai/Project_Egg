using Fusion;
using UnityEngine;

public class LogSpawner : NetworkBehaviour
{
    [Header("Spawn Setting")]
    public NetworkObject logPrefab;
    public float spawnInterval = 3f;

    [Networked] private TickTimer SpawnTimer { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            SpawnTimer = TickTimer.CreateFromSeconds(Runner, spawnInterval);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (SpawnTimer.Expired(Runner))
        {
            if (logPrefab != null)
            {
                Runner.Spawn(logPrefab, transform.position, transform.rotation);
            }
            SpawnTimer = TickTimer.CreateFromSeconds(Runner, spawnInterval);
        }
    }
}