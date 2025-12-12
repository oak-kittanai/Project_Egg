using Fusion;
using UnityEngine;

public class GameManager : SingletonNetwork<GameManager>
{
    [SerializeField] NetworkRunner NetworkRunner;

    public override void Spawned()
    {
        base.Spawned();
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
    }

    public void GetNetworkRunner(NetworkRunner networkRunner)
    {
        NetworkRunner = networkRunner;
    }

    public void SelfDeSpawn(NetworkObject objToDespawn, bool NeedToRespawn, Vector2 oldPos)
    {
        NetworkRunner.Despawn(objToDespawn);
        if (NeedToRespawn)
        {
            switch (objToDespawn.name)
            {
                default:
                    NetworkRunner.Spawn(objToDespawn);
                    break;
            }

        }
    }

    public void ProjectileDespawn(NetworkObject objToDespawn)
    {
        NetworkRunner.Despawn(objToDespawn);
    }

    public void ProjectileSpawn(NetworkObject objToSpawn, Vector2 posToSpawn, Vector2 direction, Quaternion rota)
    {
        Vector3 spawnPos = new Vector3(posToSpawn.x, posToSpawn.y, 0f);
        NetworkRunner.Spawn(objToSpawn, spawnPos, rota);

        Rigidbody2D rb = objToSpawn.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = direction * 10f;
        }
    }

    public void SpawnDropItem(NetworkObject objToSpawn, Vector2 posToSpawn)
    {
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
