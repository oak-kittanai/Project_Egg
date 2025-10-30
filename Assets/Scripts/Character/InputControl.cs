using UnityEngine;
using UnityEngine.InputSystem;

public class InputControl : MonoBehaviour
{
    [Header("ActionAssets")]
    [SerializeField] public InputActionAsset actionAsset;
    private InputAction m_moveAction;
    private InputAction m_jumpAction;
    //private InputAction m_ShiftAction;
    private InputAction m_interactAction;
    private InputAction m_skillActiveAction;

    [SerializeField] Vector2 _moveXAmt;

    public void Setup()
    {
        GetInput();
    }

    public void GetInput()
    {
        m_moveAction = InputSystem.actions.FindAction("Move");
        //m_ShiftAction = InputSystem.actions.FindAction("Sprint");
        m_jumpAction = InputSystem.actions.FindAction("Jump");
        m_interactAction = InputSystem.actions.FindAction("Interact");
        m_skillActiveAction = InputSystem.actions.FindAction("SkillActive");
    }

    public Vector2 UpdateMoveInput()
    {
        _moveXAmt = m_moveAction.ReadValue<Vector2>();
        return _moveXAmt;
    }

    public InputAction JumpAction => m_jumpAction;
    //public InputAction ShiftAction => m_ShiftAction;
    public InputAction InteractAction => m_interactAction;
    public InputAction SkillPressAction => m_skillActiveAction;
}
