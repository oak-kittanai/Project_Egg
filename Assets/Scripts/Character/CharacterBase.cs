using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterBase : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] InputActionAsset InputActionAsset;

    [SerializeField] InputAction m_moveAction;
    [SerializeField] InputAction m_jumpAction;

    [SerializeField] Vector2 _moveXAmt;

    [Header("Stats")]

    [SerializeField] string s_name;

    [SerializeField] int s_minHealth;
    [SerializeField] int s_maxHealth;

    [SerializeField] float s_weight;

    [SerializeField] bool _isDash;

    [SerializeField] float s_speed;
    [SerializeField] float s_runSpeed;
    [SerializeField] float s_jumpForce;

    [SerializeField] Vector2 direction = Vector2.zero;

    [Header("Referent")]
    [SerializeField] Rigidbody2D rb2D;


    #region Public_value

    public int hp => s_minHealth;
    public float speed => s_speed;
    public float weight => s_weight;
    public float jumpForce => s_jumpForce;

    #endregion

    private void Awake()
    {
        m_moveAction = InputSystem.actions.FindAction("Move");
        m_jumpAction = InputSystem.actions.FindAction("Jump");

        s_minHealth = s_maxHealth;
        if (rb2D == null)
        {
            rb2D = this.GetComponent<Rigidbody2D>();
        }
    }

    #region Input

    public void GetInput()
    {
        
    }

    #endregion

    #region AdjustValue

    public void TakeDamage(int dmg)
    {
        if (_isDash)
        {
            return;
        }

        s_minHealth -= dmg;
    }

    public void HealPlayer(int amount)
    {
        s_minHealth += amount;
        if (s_minHealth > s_maxHealth)
        {
            s_minHealth = s_maxHealth;
        }
    }

    #endregion


    #region Skill
    public void CombineDashAction(float weight)
    {

    }

    #endregion

}

