using Fusion;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PressurePlateHinge : NetworkBehaviour
{
    [Header("Setting")]
    [SerializeField] private HingeDoor[] targetHinges;

    [Networked] public NetworkBool IsPressed { get; set; }

    [Header("Visuals")]
    public SpriteRenderer sr;
    public Sprite unpressed;
    public Sprite pressed;

    private BoxCollider2D col;

    private void Awake()
    {
        col = GetComponent<BoxCollider2D>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        Collider2D[] hitColliders = Physics2D.OverlapBoxAll(col.bounds.center, col.bounds.size, 0f);

        int playerCount = 0;

        foreach (Collider2D hit in hitColliders)
        {
            if (hit.CompareTag("Player"))
            {
                playerCount++;
            }
        }

        bool shouldBePressed = playerCount > 0;

        if (IsPressed != shouldBePressed)
        {
            IsPressed = shouldBePressed;
            UpdateHinges(IsPressed);
        }
    }

    private void UpdateHinges(bool isOpen)
    {
        if (targetHinges == null) return;

        foreach (HingeDoor hinge in targetHinges)
        {
            if (hinge != null)
            {
                hinge.SetDoorState(isOpen);
            }
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