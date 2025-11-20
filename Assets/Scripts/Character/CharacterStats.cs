using Fusion;
using UnityEngine;

public class CharacterStats : NetworkBehaviour
{
    [Header("Referent")]
    InputControl controller;
    CharacterAction characterAction;

    // Test Stamina
    [SerializeField] RectTransform staminaBar;

    [Header("Stats & Network")]
    [SerializeField] public int s_minHealth;
    [SerializeField] public int s_maxHealth = 5;

    [SerializeField] public float s_speed;
    [SerializeField] public float s_walkSpeed;
    [SerializeField] public float s_jumpForce;
    [SerializeField] public float s_flySpeed;

    [SerializeField] public float s_minStamina;
    [SerializeField] public float s_maxStamina = 30;

    [SerializeField] public float _acceleration = 5f;
    [SerializeField] public float _deceleration = 5f;
    [SerializeField] public float _maxSpeed = 20f;

    [Header("CharacterSet")]
    [Networked] public bool isDuck {  get; set; }
    [Networked] public bool isBird { get; set; }


    #region Public_value_Networked

    #endregion

    public void Setup()
    {
        controller = GetComponent<InputControl>();
        characterAction = GetComponent<CharacterAction>();

        s_minHealth = s_maxHealth;
        s_minStamina = s_maxStamina;
    }

    public void RechargeStamina(bool recharging)
    {
        if (s_minStamina < s_maxStamina && recharging)
        {
            recharging = StaminaRecharge(0);
        }
        else
        {
            Debug.Log("Already on max stamina");
            recharging = false;
        }
    }

    #region AdjustValue
    public void TakeDamage(int damage)
    {
        s_minHealth -= damage;
    }

    public void StaminaReduce(float i)
    {
        if (s_minStamina > 0)
        {
            s_minStamina -= (1f + i) * Time.deltaTime;
        }
    }

    public bool StaminaRecharge(float i)
    {
        if (s_minStamina == s_maxStamina)
        {
            return false;
        }
        else
        {
            s_minStamina += (1f + i) * Time.deltaTime;
            return true;
        }
    }

    public void HealPlayer(int amount)
    {
        if (s_minHealth == s_maxHealth)
        {
            Debug.Log("Health is already full");
            return;
        }
        else if (s_minHealth < s_maxHealth)
        {
            s_minHealth += amount;
            if (s_minHealth > s_maxHealth)
            {
                s_minHealth = s_maxHealth;
            }
        }
    }
    #endregion
}
