using Fusion;
using UnityEngine;
using System.Collections;

public class NetObjDestroy : NetworkBehaviour
{
    public string targetTag = "Log";
    public float delayTime = 0.2f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!HasStateAuthority) return;

        if (collision.gameObject.CompareTag(targetTag))
        {
            DespawnSequence(collision.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!HasStateAuthority) return;

        if (collision.CompareTag(targetTag))
        {
            DespawnSequence(collision.gameObject);
        }
    }

    private void DespawnSequence(GameObject obj)
    {
        StartCoroutine(DelayDespawn(obj));
    }

    private IEnumerator DelayDespawn(GameObject obj)
    {
        yield return new WaitForSeconds(delayTime);

        if (obj != null)
        {
            NetworkObject netObj = obj.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsValid)
            {
                Runner.Despawn(netObj);
            }
        }
    }
}