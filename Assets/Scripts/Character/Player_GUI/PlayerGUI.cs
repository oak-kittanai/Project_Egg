using UnityEngine;
using UnityEngine.UI;

public class PlayerGUI : MonoBehaviour
{
    [Header("Ref")]
    CharacterStats stats;

    [SerializeField] Slider staminaBar;

    private void Awake()
    {
        stats = GetComponentInParent<CharacterStats>();
        if (stats == null)
        {
            Debug.Log("stats in GUI not found");
        }
        else
        {
            staminaBar.minValue = 0;
            staminaBar.maxValue = stats.MaxStamina;
        }
    }

    private void Update()
    {
        StaminaUpdate();

        if (stats.MinStamina >= stats.MaxStamina)
        {
            staminaBar.gameObject.SetActive(false);
        }
        else
        {
            staminaBar.gameObject.SetActive(true);
        }
    }

    public void StaminaUpdate()
    {
        staminaBar.value = stats.MinStamina;
    }
}
