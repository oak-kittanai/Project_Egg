using Unity.VisualScripting;
using UnityEngine;

public class ToggleButton_CloseTrap : MonoBehaviour
{
    [SerializeField] TrapPressure targetTrap;
    [SerializeField] bool isDisable = false;
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
        SetTrap();
    }

    public void SetTrap()
    {
        if (playerNearby && Input.GetKeyDown(KeyCode.E))
        {
            isDisable = !isDisable;
            targetTrap.SetTrapActive(!isDisable);
        }
    }
}
