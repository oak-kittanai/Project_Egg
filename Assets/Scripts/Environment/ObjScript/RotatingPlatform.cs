using UnityEngine;
using Fusion;

[RequireComponent(typeof(Rigidbody2D))]
public class RotatingPlatform : NetworkBehaviour
{
    [Header("Rotate Setting")]
    [SerializeField] private float rotateDuration = 1.5f; 
    [SerializeField] private float pauseDuration = 2.0f;
    [SerializeField] private bool rotateClockwise = true; //หมุนตามเข็ม

    private Rigidbody2D rb;

    // === ตัวแปร Network ===
    [Networked] private float StartAngle { get; set; }
    [Networked] private float TargetAngle { get; set; }
    [Networked] private float RotationProgress { get; set; }
    [Networked] private NetworkBool IsPausing { get; set; }
    [Networked] private TickTimer PauseTimer { get; set; }
    [Networked] private float CurrentAngle { get; set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.isKinematic = true;
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            StartAngle = transform.eulerAngles.z;
            TargetAngle = StartAngle + (rotateClockwise ? -90f : 90f);
            RotationProgress = 0f;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (IsPausing)
        {
            if (PauseTimer.Expired(Runner))
            {
                IsPausing = false;
                StartAngle = TargetAngle;
                TargetAngle = StartAngle + (rotateClockwise ? -90f : 90f);
                RotationProgress = 0f;
                PauseTimer = TickTimer.None;
            }
        }
        else
        {
            RotationProgress += Runner.DeltaTime / rotateDuration;

            if (RotationProgress >= 1f)
            {
                RotationProgress = 1f;
                IsPausing = true;
                PauseTimer = TickTimer.CreateFromSeconds(Runner, pauseDuration);
            }

            float easedProgress = Mathf.SmoothStep(0f, 1f, RotationProgress);

            CurrentAngle = Mathf.Lerp(StartAngle, TargetAngle, easedProgress);

            rb.MoveRotation(CurrentAngle);
        }
    }

    public override void Render()
    {
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, CurrentAngle), Runner.DeltaTime * 15f);
    }
}