using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

public class CharacterStats : NetworkBehaviour, IDamageable
{
    [Header("Referent")]
    InputControl controller;
    CharacterAction characterAction;
    MovementCharacter movement;
    CharacterAnimation cAnimation;

    Rigidbody2D rb2D;
    [Networked] NetworkRigidbody2D netRB2D {  get; set; }

    [Header("Stats & Network")]
    [SerializeField] public int s_minHealth;
    [SerializeField] public int s_maxHealth = 5;

    [SerializeField] public float s_speed;
    [SerializeField] public float s_walkSpeed;
    [SerializeField] public float s_jumpForce;
    [SerializeField] public float s_flySpeed;

    [SerializeField] public float s_minStamina;
    [SerializeField] public float s_maxStamina = 30;

    [SerializeField] public float acceleration = 5f;
    [SerializeField] public float deceleration = 5f;
    [SerializeField] public float maxSpeed = 20f;

    bool StaminaBusy => movement._staminaBusy;

    [Header("CharacterSet")]
    [Networked] public characterType skinType { get; set; }
    [Networked] public int skinIndex { get; set; }

    private void Awake()
    {
        Setup();
    }

    public override void Spawned()
    {
        if (skinType == characterType.Bird)
        {
            s_jumpForce += 1.5f;
        }
    }

    public void Setup()
    {
        controller = GetComponent<InputControl>();
        characterAction = GetComponent<CharacterAction>();
        movement = GetComponent<MovementCharacter>();
        cAnimation = GetComponent<CharacterAnimation>();

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

    #region AdjustValue
    public void TakeDamage(int dmg, float knockbackForce, Vector2 vec)
    {
        if (movement.isDash)
        {
            Debug.Log("Dodge");
            return;
        }

        Vector2 direction = (vec - (Vector2)transform.position).normalized;
        Vector2 knockbackDir = -direction * knockbackForce;

        rb2D.AddForce(knockbackDir, ForceMode2D.Impulse);

        TakeDamage(dmg);
    }
    #endregion
}
