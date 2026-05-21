using Fusion;
using UnityEngine;

public class LogSpawner : NetworkBehaviour
{
    [Header("ใส่ Prefab")]
    public NetworkObject itemPrefab;
    public float spawnDelay = 3f;
    [Header("จะเสกทีละอันหรือรัวๆ")]
    public bool isSpamSpawn = true;

    // ---ตัวแปร Network เ---
    [Networked] private TickTimer SpawnTimer { get; set; }
    [Networked] private NetworkObject CurrentLog { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            SpawnTimer = TickTimer.CreateFromSeconds(Runner, spawnDelay);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (!isSpamSpawn)
        {
            if (CurrentLog != null && CurrentLog.IsValid)
            {
                SpawnTimer = TickTimer.CreateFromSeconds(Runner, spawnDelay);
                return;
            }
        }
        if (SpawnTimer.Expired(Runner))
        {
            if (itemPrefab != null)
            {
                CurrentLog = Runner.Spawn(itemPrefab, transform.position, transform.rotation);
            }

            SpawnTimer = TickTimer.CreateFromSeconds(Runner, spawnDelay);
        }
    }
}