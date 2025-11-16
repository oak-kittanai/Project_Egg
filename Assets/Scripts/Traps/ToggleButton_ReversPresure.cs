using UnityEngine;

public class ToggleButton_ReversPresure : MonoBehaviour
{
    [SerializeField] TrapPressure targetTrap;
    [SerializeField] bool isRevers = false;
    [SerializeField] bool playerNearby = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerNearby = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerNearby = false;
        }
    }

    private void Update()
    {
        SetRevers();
    }

    public void SetRevers()
    {
        if (playerNearby && Input.GetKeyDown(KeyCode.E))
        {
            isRevers = !isRevers;
            targetTrap.ChangeDirection(!isRevers);
        }
    }
}
