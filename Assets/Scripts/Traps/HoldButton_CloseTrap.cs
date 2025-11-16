using UnityEngine;

public class HoldButton_CloseTrap : MonoBehaviour
{
    [SerializeField] TrapPressure targetTrap;
    [SerializeField] GameObject corePresure;


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (targetTrap != null && collision.CompareTag("Player"))
        {
            Debug.Log("PushOFF");
            targetTrap.SetTrapActive(false);
            corePresure.SetActive(false);
        }
    }

    //=======================================================================

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (targetTrap != null && collision.CompareTag("Player"))
        {
            Debug.Log("PushON");
            targetTrap.SetTrapActive(true);
            corePresure.SetActive(true);
        }
    }
}
