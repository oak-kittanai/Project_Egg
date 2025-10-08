using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [Header("Referent")]
    InputControl controller;
    CharacterAction characterAction;

    [Header("Network")]
    [SerializeField] int s_minHealth;
    [SerializeField] int s_maxHealth = 5;

    [SerializeField] float s_speed;
    [SerializeField] float s_walkSpeed;
    [SerializeField] float s_runSpeed;
    [SerializeField] float s_jumpForce;

    [SerializeField] float s_minStamina;
    [SerializeField] float s_maxStamina = 30;

    #region Public_value

    public int hp => s_minHealth;
    public int maxHp => s_maxHealth;
    public float speed => s_speed;
    public float jumpForce => s_jumpForce;

    #endregion

    public void Setup()
    {
        controller = GetComponent<InputControl>();
        characterAction = GetComponent<CharacterAction>();

        s_minHealth = s_maxHealth;
    }

    public float WalkSpeed => s_walkSpeed;
    public float RunSpeed => s_runSpeed;
    public float MinStamina => s_minStamina;
    public float MaxStamina => s_maxStamina;

    public void RechargeStamina()
    {
        bool recharging = true;
        if (s_minStamina < s_maxStamina && recharging)
        {
            s_minStamina += 1 * Time.deltaTime;
            recharging = false;
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
