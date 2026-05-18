using UnityEngine;
using Fusion;

public class HingeDoor : NetworkBehaviour
{
    [Header("Hinge Setting")]
    [SerializeField] private float closedAngle = 0f;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float swingSpeed = 150f;

    [Networked] public NetworkBool IsOpen { get; set; }
    [Networked] private float CurrentAngle { get; set; }

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            CurrentAngle = closedAngle;
            transform.rotation = Quaternion.Euler(0, 0, CurrentAngle);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        float targetAngle = IsOpen ? openAngle : closedAngle;

        if (!Mathf.Approximately(CurrentAngle, targetAngle))
        {

            CurrentAngle = Mathf.MoveTowards(CurrentAngle, targetAngle, swingSpeed * Runner.DeltaTime);

            rb.MoveRotation(CurrentAngle);
        }
    }

    public override void Render()
    {
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, CurrentAngle), Runner.DeltaTime * 15f);
    }

    public void SetDoorState(bool open)
    {
        if (HasStateAuthority)
        {
            IsOpen = open;
        }
    }
}