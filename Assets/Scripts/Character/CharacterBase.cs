using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterBase : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] InputActionAsset InputActionAsset;

    [SerializeField] InputAction m_moveAction;
    [SerializeField] InputAction m_jumpAction;
    [SerializeField] InputAction m_interactAction;
    [SerializeField] InputAction m_skillActiveAction;

    [SerializeField] Vector2 _moveXAmt;
    [SerializeField] bool _isInteractAble;
    [SerializeField] bool _isAbleSkill;

    [SerializeField] bool _busy;

    [Header("Stats")]

    [SerializeField] string s_name;

    [SerializeField] int s_minHealth;
    [SerializeField] int s_maxHealth;

    [SerializeField] float s_weight;

    [SerializeField] bool _isDash;

    [SerializeField] float s_speed;
    [SerializeField] float s_walkSpeed;
    [SerializeField] float s_runSpeed;
    [SerializeField] float s_jumpForce;
    [SerializeField] bool _isJump;

    [SerializeField] Vector2 direction = Vector2.zero;

    [Header("CharacterSet")]
    [SerializeField] bool _isDuck;
    [SerializeField] bool _isEagle;

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
        s_minHealth = s_maxHealth;

        GetInput();
    }

    private void Update()
    {
        UpdateInput();
    }

    #region Input

    public void GetInput()
    {
        m_moveAction = InputSystem.actions.FindAction("Move");
        m_jumpAction = InputSystem.actions.FindAction("Jump");
        m_interactAction = InputSystem.actions.FindAction("Interact");
        m_skillActiveAction = InputSystem.actions.FindAction("SkillActive");

        if (rb2D == null)
        {
            rb2D = this.GetComponent<Rigidbody2D>();
        }
    }

    public void UpdateInput()
    {
        _moveXAmt = m_moveAction.ReadValue<Vector2>();

        if (_isJump)
        {
            if (m_jumpAction.WasPressedThisFrame())
            {
                _isJump = false;
                // do jump


                _isJump = JumpAction();
            }
        }

        if (_isInteractAble)
        {
            if (_busy) return;

            if (m_interactAction.WasPressedThisFrame())
            {
                // do interact

                // return _busy = false; return _busy to false
            }
        }
        
        if (_isAbleSkill)
        {
            if (_busy) return;

            if (m_skillActiveAction.WasPressedThisFrame())
            {
                _busy = true;
                // do skill

                // return _busy = false; return _busy to false
            }
        }
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

    #region CharacterAction

    private bool JumpAction()
    {
        // Do jump
        int i = 1;
        if (i == 1)
        {
            // able to return to ground and return true
            return true;
        }
        else
        {
            // can't find ground
            return false;
        }

    }

    private void CheckCharacterAbilty()
    {
        if (_isDuck)
        {

        }

        if (_isEagle)
        {

        }
    }

    #endregion

}

[Serializable]
public class SkillSet
{
    [Header("Eagle")]
    public float _flyDuration;
    public float _flySpeed;


    [Header("Duck")]
    public bool isFloating;
    public float floatingSpeed;
    // floating animation

    public bool isDive;
    public float diveSpeed;
    // dive animation


    // Duck
    public void WaterFloating()
    {
        // Set speed
        int i = 1;
        if (i == 1) // if on top of water
        {
            float speed;
            float walkSpeed = floatingSpeed;
            // floating animation

            speed = walkSpeed;

            if (i == 2) // if press some button to dive
            {
                speed = diveSpeed;
                // dive animation
            }
        }
    }

}