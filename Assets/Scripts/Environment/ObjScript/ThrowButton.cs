using Fusion;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class ThrowButton : NetworkBehaviour
{
    [Header("Door Target")]
    public SlidingNetworkDoor targetDoor;

    [Header("Button Setting")]
    [SerializeField] public bool isSingleUse = true;

    [SerializeField] private float resetTime = 3f;

    [Header("Visuals")]
    public SpriteRenderer sr;
    public Sprite unpressed;
    public Sprite pressed;

    // --- ตัวแปร Network ---
    [Networked] public NetworkBool IsPressed { get; set; }
    [Networked] private TickTimer ResetTimer { get; set; }

    private void Awake()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            IsPressed = false;
            ResetTimer = TickTimer.None;

            if (targetDoor != null)
            {
                targetDoor.SetDoorState(false);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (!isSingleUse && IsPressed && ResetTimer.Expired(Runner))
        {
            IsPressed = false;

            if (targetDoor != null) targetDoor.SetDoorState(false);

            ResetTimer = TickTimer.None;
        }
    }

    #region Collision2D
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!HasStateAuthority) return;
        if (isSingleUse && IsPressed) return;

        if (collision.gameObject.CompareTag("Rock"))
        {
            float impactSpeed = collision.relativeVelocity.magnitude;

            if (impactSpeed > 2f) 
            {
                ActivateButton();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!HasStateAuthority) return;
        if (isSingleUse && IsPressed) return;

        if (collision.gameObject.CompareTag("Rock"))
        {
            Rigidbody2D rockRb = collision.GetComponent<Rigidbody2D>();

            if (rockRb != null)
            {
                float speed = rockRb.linearVelocity.magnitude;

                if (speed > 2f)
                {
                    ActivateButton();
                }
            }
        }
    }
    #endregion

    private void ActivateButton()
    {
        IsPressed = true;

        if (targetDoor != null)
        {
            targetDoor.SetDoorState(true);
        }

        if (!isSingleUse)
        {
            ResetTimer = TickTimer.CreateFromSeconds(Runner, resetTime);
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