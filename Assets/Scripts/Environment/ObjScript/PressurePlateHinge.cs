using Fusion;
using UnityEngine;

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

    // ใช้ buffer เดิมทุก tick แทน OverlapBoxAll ที่จอง array ใหม่ทุกครั้ง (PF-1) — ผลเหมือนเดิม
    private readonly Collider2D[] _hits = new Collider2D[32];
    private ContactFilter2D _filter;

    private void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        _filter = new ContactFilter2D { useLayerMask = true, layerMask = Physics2D.DefaultRaycastLayers, useTriggers = Physics2D.queriesHitTriggers };
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        int hitCount = Physics2D.OverlapBox(col.bounds.center, col.bounds.size, 0f, _filter, _hits);

        int playerCount = 0;

        for (int i = 0; i < hitCount; i++)
        {
            if (_hits[i].CompareTag("Player"))
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