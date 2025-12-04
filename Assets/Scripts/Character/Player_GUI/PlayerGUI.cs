using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class PlayerGUI : NetworkBehaviour
{
    [Header("Ref")]
    CharacterStats stats;

    [SerializeField] Slider staminaBar;
    [Networked] public float minValue {  get; set; }
    [Networked] public float maxValue {  get; set; }

    private void Awake()
    {
        stats = GetComponentInParent<CharacterStats>();
    }

    public override void Spawned()
    {
        if (stats == null)
        {
            Debug.Log("stats in GUI not found");
        }
        else
        {
            if (HasStateAuthority)
            {
                minValue = 0;
                maxValue = stats.s_maxStamina;
            }

            staminaBar.minValue = minValue;
            staminaBar.maxValue = maxValue;
        }
    }

    public override void FixedUpdateNetwork()
    {
        StaminaUpdate();

        if (stats.s_minStamina >= stats.s_maxStamina)
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
        if (HasStateAuthority)
        {
            minValue = stats.s_minStamina;
        }
        staminaBar.value = minValue;
    }
}
