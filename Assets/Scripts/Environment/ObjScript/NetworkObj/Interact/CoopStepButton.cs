using Fusion;
using UnityEngine;

public class CoopStepButton : NetworkBehaviour
{
    [Networked] public NetworkBool IsPressed { get; set; }
    [Networked] public int ObjectsOnPad { get; set; }

    [SerializeField] public bool isSingleUse;

    [SerializeField] public LayerMask triggerLayers;

    [Header("Visuals")]
    public SpriteRenderer sr;
    public Sprite unpressed;
    public Sprite pressed;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!HasStateAuthority) return;

        if (isSingleUse && IsPressed) return;

        if (((1 << collision.gameObject.layer) & triggerLayers) != 0)
        {
            ObjectsOnPad++;
            IsPressed = ObjectsOnPad > 0;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!HasStateAuthority) return;

        if (isSingleUse) return;

        if (((1 << collision.gameObject.layer) & triggerLayers) != 0)
        {
            ObjectsOnPad--;

            if (ObjectsOnPad < 0) ObjectsOnPad = 0;

            IsPressed = ObjectsOnPad > 0;
        }
    }

    public override void Render()
    {
        if (sr != null)
        {
            sr.sprite = IsPressed ? pressed : unpressed;
        }
    }
}