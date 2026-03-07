using Fusion;
using UnityEngine;

public class Toggle_Shooter_Trap : NetworkBehaviour
{
    public enum ButtonMode
    {
        ToggleOnce,
        HoldToDisable
    }

    [Header("Trap Settings")]
    [SerializeField] private SnowballShooterTrap targetTrap;
    [SerializeField] private ButtonMode mode = ButtonMode.HoldToDisable;

    [Tooltip("Layer Player/Moveable Rock")]
    [SerializeField] private LayerMask triggerLayer;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Sprite unpressedSprite;
    [SerializeField] private Sprite pressedSprite;

    [Networked] public NetworkBool IsPressed { get; set; }
    [Networked] public int ObjectsOnPad { get; set; }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!HasStateAuthority) return;

        if (((1 << collision.gameObject.layer) & triggerLayer) != 0)
        {
            ObjectsOnPad++;

            if (ObjectsOnPad == 1)
            {
                IsPressed = true;

                if (mode == ButtonMode.ToggleOnce)
                {
                    targetTrap.SetTrapActive(!targetTrap.IsActive);
                }
                else if (mode == ButtonMode.HoldToDisable)
                {
                    targetTrap.SetTrapActive(false);
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!HasStateAuthority) return;

        if (((1 << collision.gameObject.layer) & triggerLayer) != 0)
        {
            ObjectsOnPad--;

            if (ObjectsOnPad <= 0)
            {
                ObjectsOnPad = 0;
                IsPressed = false;

                if (mode == ButtonMode.HoldToDisable)
                {
                    targetTrap.SetTrapActive(true);
                }
            }
        }
    }

    public override void Render()
    {
        if (sr != null)
        {
            sr.sprite = IsPressed ? pressedSprite : unpressedSprite;
        }
    }
}