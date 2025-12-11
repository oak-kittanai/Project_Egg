using UnityEngine;

public class BreakableRock : MonoBehaviour, Interactable
{
    [SerializeField] GameObject itemToDrop;
    [SerializeField] int dropAmount = 1;
    [SerializeField] float dropForce = 3f;

    public void Interact()
    {
        BreakAndDrop();
    }

    void BreakAndDrop()
    {
        if (itemToDrop != null)
        {
            for (int i = 0; i < dropAmount; i++)
            {
                SpawnItem();
            }
        }

        Destroy(gameObject);
    }

    void SpawnItem()
    {
        GameObject item = Instantiate(itemToDrop, transform.position, Quaternion.identity);
        Rigidbody2D rb = item.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 randomDir = new Vector2(Random.Range(-1f, 1f), Random.Range(0.5f, 1f)).normalized;
            rb.AddForce(randomDir * dropForce, ForceMode2D.Impulse);
        }
    }
}