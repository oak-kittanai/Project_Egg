using Fusion;
using UnityEngine;

public class InsertStone_Mech : NetworkBehaviour
{
    [Header("Required Pads")]
    public StonePad[] requiredPads; // ช่องใส่หิน

    [Header("Components")]
    private Animator anim;
    private Collider2D col;

    [Networked, OnChangedRender(nameof(OnDoorStateChanged))]
    public NetworkBool IsOpen { get; set; }

    private void Awake()
    {
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            IsOpen = false;
        }

        UpdateVisuals();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (IsOpen) return;

        bool allFilled = true;
        foreach (var pad in requiredPads)
        {
            if (pad == null || !pad.IsFilled)
            {
                allFilled = false;
                break;
            }
        }

        // หินครบ
        if (allFilled && requiredPads.Length > 0)
        {
            IsOpen = true;
        }
    }

    public void OnDoorStateChanged()
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (col != null)
        {
            col.enabled = IsOpen;
        }

        // Anim
        if (anim != null)
        {
            if (IsOpen)
            {
                anim.Play("Dimensions_Door_Open");
            }
            else
            {
                anim.Play("Dimensions_Door_Close");
            }
        }
    }
}