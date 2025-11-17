using Fusion;
using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [Header("Referent")]
    InputControl controller;
    CharacterAction characterAction;

    // Test Stamina
    [SerializeField] RectTransform staminaBar;

    [Header("Stats & Network")]
    [SerializeField] int s_minHealth;
    [SerializeField] int s_maxHealth = 5;

    [SerializeField] float s_speed;
    [SerializeField] float s_walkSpeed;
    [SerializeField] float s_jumpForce;
    [SerializeField] float s_flySpeed;

    [SerializeField] float s_minStamina;
    [SerializeField] float s_maxStamina = 30;

    [SerializeField] float _acceleration = 5f;
    [SerializeField] float _deceleration = 5f;
    [SerializeField] float _maxSpeed = 20f;

    [Header("CharacterSet")]
    [Networked] public bool isDuck {  get; set; }
    [Networked] public bool isBird { get; set; }


    #region Public_value_Networked

    [Networked] public int hp => s_minHealth;
    [Networked] public int maxHp => s_maxHealth;
    [Networked] public float speed => s_speed;
    [Networked] public float jumpForce => s_jumpForce;
    [Networked] public float WalkSpeed => s_walkSpeed;
    [Networked] public float FlySpeed => s_flySpeed;
    [Networked] public float MinStamina => s_minStamina;
    [Networked] public float MaxStamina => s_maxStamina;

    [Networked] public float Acceleration => _acceleration;
    [Networked] public float Deceleration => _deceleration;
    [Networked] public float MaxSpeed => _maxSpeed;

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
