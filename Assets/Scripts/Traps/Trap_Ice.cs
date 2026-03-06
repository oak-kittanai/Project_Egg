using Fusion;
using System.Collections;
using UnityEngine;

public class Trap_Ice : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float cooldownTime = 1.5f;
    [SerializeField] private GameObject hitColl;

    private bool isReady = true;

    private void Awake()
    {
        if (hitColl != null) hitColl.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {

        if (isReady && other.CompareTag("Player"))
        {
            StartCoroutine(ActivateTrap());
        }
    }

    private IEnumerator ActivateTrap()
    {
        isReady = false;

        if (hitColl != null)
        {
            hitColl.SetActive(true);

            yield return new WaitForFixedUpdate();
            hitColl.SetActive(false);
        }

        yield return new WaitForSeconds(cooldownTime);
        isReady = true;
    }
}