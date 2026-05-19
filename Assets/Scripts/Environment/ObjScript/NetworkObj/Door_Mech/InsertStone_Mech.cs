using Fusion;
using UnityEngine;

public class InsertStone_Mech : NetworkBehaviour
{
    [Header("Required Pads")]
    public StonePad[] requiredPads; //ช่องใส่หิน

    [Header("Door Visual")]
    public Sprite lockSprite;
    public Sprite openSprite;
    private SpriteRenderer sr;

    [Header("Door Movement")]
    [SerializeField] float openHeight = 3f;
    [SerializeField] float openSpeed = 2f;

    [Networked] public NetworkBool IsOpen { get; set; }

    private Vector3 closedPos;
    private Vector3 openPos;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public override void Spawned()
    {
        closedPos = transform.position;
        openPos = closedPos + Vector3.up * openHeight;
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

        if (allFilled && requiredPads.Length > 0)
        {
            IsOpen = true;
        }
    }

    public override void Render()
    {
        if (sr != null)
        {
            sr.sprite = IsOpen ? openSprite : lockSprite;
        }

        Vector3 targetPos = IsOpen ? openPos : closedPos;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, openSpeed * Time.deltaTime);
    }
}