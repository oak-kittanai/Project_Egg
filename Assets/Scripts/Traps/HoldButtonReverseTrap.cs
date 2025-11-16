using UnityEngine;

public class HoldButtonReverseTrap : MonoBehaviour
{
    [SerializeField] TrapPressure targetTrap;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (targetTrap != null && collision.CompareTag("Player"))
        {
            Debug.Log("Revers");
            targetTrap.ChangeDirection(true);
        }
    }

    //============================================================

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (targetTrap != null && collision.CompareTag("Player"))
        {
            Debug.Log("Default");
            targetTrap.ChangeDirection(false);
        }
    }
}
