using Fusion;
using UnityEngine;

public class DoDamgeClear : NetworkBehaviour
{
    [Header("Trap Setting")]
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float knockbackForce = 5f;

    [Header("Lifetime Setting")]
    [SerializeField] private float delayBeforeDes = 0.25f;

    [SerializeField] private float maxLifetime = 6.0f;

    // --- ตัวแปร Network ---
    [Networked] private TickTimer ClearTimer { get; set; }
    [Networked] private TickTimer AutoDestroyTimer { get; set; }
    [Networked] private NetworkBool IsTriggered { get; set; }

    private Rigidbody2D rb2D;

    // freeze ตอน pause: เก็บค่าความเร็ว/แรงโน้มถ่วงไว้ แล้วหยุดนิ่ง คืนค่าเมื่อเล่นต่อ (แบบ RockObject)
    private bool _frozen;
    private Vector2 _frozenVel;
    private float _frozenAng;
    private float _frozenGravity;

    public override void Spawned()
    {
        rb2D = GetComponent<Rigidbody2D>();

        if (HasStateAuthority)
        {
            IsTriggered = false;
            ClearTimer = TickTimer.None;

            AutoDestroyTimer = TickTimer.CreateFromSeconds(Runner, maxLifetime);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        // freeze ตอน pause/dialogue (TP-3) — เดิมแค่ return: ขอนยังกลิ้ง/ตกด้วย physics และอายุหมดระหว่าง pause
        if (GameplayPause.Frozen)
        {
            ClearTimer = GameplayPause.Hold(ClearTimer, Runner);
            AutoDestroyTimer = GameplayPause.Hold(AutoDestroyTimer, Runner);

            if (!_frozen && rb2D != null)
            {
                _frozenVel = rb2D.linearVelocity;
                _frozenAng = rb2D.angularVelocity;
                _frozenGravity = rb2D.gravityScale;
                rb2D.linearVelocity = Vector2.zero;
                rb2D.angularVelocity = 0f;
                rb2D.gravityScale = 0f;
                _frozen = true;
            }
            return;
        }
        if (_frozen && rb2D != null)
        {
            rb2D.linearVelocity = _frozenVel;
            rb2D.angularVelocity = _frozenAng;
            rb2D.gravityScale = _frozenGravity;
            _frozen = false;
        }

        if (IsTriggered && ClearTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        if (!IsTriggered && AutoDestroyTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!HasStateAuthority) return;
        if (IsTriggered) return;
        if (GameplayPause.Frozen) return;

        if (other.CompareTag("Player"))
        {
            MovementCharacter[] allCharacterMovement = other.GetComponents<MovementCharacter>();

            foreach (var character in allCharacterMovement)
            {
                if (character != null && character.enabled)
                {
                    float pushDirectionX = Mathf.Sign(other.transform.position.x - transform.position.x);
                    Vector2 knockbackDirection = new Vector2(pushDirectionX, 1f).normalized;

                    character.TakeDamage(damageAmount, knockbackForce, knockbackDirection);
                }
            }

            IsTriggered = true;
            ClearTimer = TickTimer.CreateFromSeconds(Runner, delayBeforeDes);
        }
    }
}