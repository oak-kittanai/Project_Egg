using Fusion;
using UnityEngine;

public class GameManager : SingletonNetwork<GameManager>
{
    [SerializeField] NetworkRunner NetworkRunner;

    [Networked] public int TeamKeys { get; set; }

    public override void Spawned()
    {
        base.Spawned();
    }

    public void GetNetworkRunner(NetworkRunner networkRunner)
    {
        NetworkRunner = networkRunner;
    }

    // Key
    public void AddKey()
    {
        if (HasStateAuthority) TeamKeys++;
        else RPC_RequestAddKey();
    }

    public void UseKey()
    {
        if (HasStateAuthority) TeamKeys--;
        else RPC_RequestUseKey();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestAddKey() => TeamKeys++;

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestUseKey() => TeamKeys--;


    public void RequestDespawn(NetworkObject objToDespawn)
    {
        if (HasStateAuthority)
        {
            NetworkRunner.Despawn(objToDespawn);
        }
        else
        {
            RPC_Despawn(objToDespawn.Id);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_Despawn(NetworkId objId)
    {
        if (NetworkRunner.TryFindObject(objId, out var obj))
        {
            NetworkRunner.Despawn(obj);
        }
    }

    public void ProjectileSpawn(NetworkObject objToSpawn, Vector2 posToSpawn, Vector2 direction, Quaternion rota)
    {
        if (!HasStateAuthority) return;

        Vector3 spawnPos = new Vector3(posToSpawn.x, posToSpawn.y, 0f);
        NetworkRunner.Spawn(objToSpawn, spawnPos, rota);

        Rigidbody2D rb = objToSpawn.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * 10f;
        }
    }

    public void SpawnDropItem(NetworkObject objToSpawn, Vector2 posToSpawn)
    {
        if (!HasStateAuthority) return;

        Vector3 spawnPos = new Vector3(posToSpawn.x, posToSpawn.y, 0f);
        NetworkObject objIte = NetworkRunner.Spawn(objToSpawn, posToSpawn);

        float dropForce = Random.Range(0.5f, 1.5f);

        Rigidbody2D rb = objIte.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 randomDir = new Vector2(Random.Range(-1f, 1f), Random.Range(0.5f, 1f)).normalized;
            rb.AddForce(randomDir * dropForce, ForceMode2D.Impulse);
        }
    }
}