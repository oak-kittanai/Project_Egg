using Fusion;
using System.Collections.Generic;
using UnityEngine;

// ท่าทางของตัวละคร — แต่ละเครื่องเลือกท่าเองใน Render() จากสถานะการจำลองที่ sync อยู่แล้ว
// (ตาย, บิน, ดำน้ำ, แบก, ติดพื้น ฯลฯ) + ความเร็วของภาพในเครื่องตัวเอง
// network sync แค่ทิศหัน (FlipX — gameplay ใช้เป็นทิศโยน/วาง) และท่าครั้งเดียว (ActionAnim + AnimationTimer)
public class CharacterAnimation : NetworkBehaviour
{
    public enum ActionAnim : byte { None, Interact, Smash, Death }

    [Header("Referent")]
    Animator animator;
    SpriteRenderer spriteRenderer;
    MovementCharacter movement;

    [Networked] public NetworkBool FlipX { get; set; }
    [Networked] public TickTimer AnimationTimer { get; set; }
    [Networked] public ActionAnim CurrentAction { get; set; }

    private string _lastLocalState = "";
    private int _lastActionTick = -1;

    [SerializeField] private float animBlendSpeed = 12f;
    private float _smoothAnimX;
    private float _smoothAnimY;

    // ความเร็วของภาพ (ประมาณจากตำแหน่งที่วาดจริง) — ใช้ได้ทั้งตัวเอง ตัวเพื่อน และบน host โดยไม่ต้อง sync
    private const float TeleportDistance = 2f;
    private const float MoveThreshold = 0.3f;
    private const float FallThreshold = -0.5f;
    private Vector3 _lastVisualPos;
    private bool _hasLastVisualPos;
    private Vector2 _visualVelocity;

    [SerializeField] public bool Carrying => movement != null && movement.isCarrying;
    [SerializeField] public bool BeingCarried => movement != null && movement.IsBeingCarried;
    [SerializeField] public bool isBird => movement != null && movement.isBird;

    [Header("Controller Setting")]
    [SerializeField] public RuntimeAnimatorController DuckController;
    [SerializeField] public RuntimeAnimatorController BirdController;

    [SerializeField] public characterType currentSkin;

    private HashSet<int> parameterHashes = new HashSet<int>();

    private void Awake()
    {
        Setup();
    }

    public override void Spawned()
    {
        if (animator == null) animator = GetComponent<Animator>();

        CacheAnimatorParameters();
    }

    public void Setup()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void InitializeMovement(MovementCharacter mc)
    {
        movement = mc;
    }

    public override void Render()
    {
        if (spriteRenderer != null) spriteRenderer.flipX = FlipX;

        UpdateVisualVelocity();

        if (animator == null || movement == null) return;

        float targetX = Mathf.Abs(_visualVelocity.x) > MoveThreshold ? Mathf.Sign(_visualVelocity.x) : 0f;
        _smoothAnimX = Mathf.MoveTowards(_smoothAnimX, targetX, animBlendSpeed * Time.deltaTime);
        _smoothAnimY = Mathf.MoveTowards(_smoothAnimY, _visualVelocity.y, animBlendSpeed * Time.deltaTime);

        if (HasParameter("X")) animator.SetFloat("X", _smoothAnimX);
        if (HasParameter("Y")) animator.SetFloat("Y", _smoothAnimY);

        string state = ResolveState(targetX != 0f);
        VerifyLogCarryState(state);
        if (string.IsNullOrEmpty(state)) return;

        // ท่าครั้งเดียวที่สั่งซ้ำ (เช่นกด E ติดกัน) ต้องเล่นใหม่ตั้งแต่ต้น
        int actionTick = IsActionActive ? (AnimationTimer.TargetTick ?? -1) : -1;
        bool restartAction = actionTick != -1 && actionTick != _lastActionTick;
        _lastActionTick = actionTick;

        if (state != _lastLocalState || restartAction)
        {
            PlayAnimationSafeLocal(state, restartAction);
            _lastLocalState = state;
        }
    }

    // TEMP — branch test/all2 only. DO NOT MERGE.
    // FB-8: log ท่าที่เลือกของเป็ดทุกครั้งที่ (ท่า, แบกอยู่, แตะพื้น) เปลี่ยน ระหว่างแบก/เพิ่งวาง
    private string _verifyCarryKey;
    private bool _verifyWasCarrying;

    private void VerifyLogCarryState(string state)
    {
        if (currentSkin != characterType.Duck || movement == null) return;

        bool carrying = Carrying;
        if (!carrying && !_verifyWasCarrying) return;
        _verifyWasCarrying = carrying;

        string key = $"{state}|{carrying}|{movement.IsGrounded}";
        if (key == _verifyCarryKey) return;
        _verifyCarryKey = key;

        VerifyLog.Log($"FB8 duck anim t={Time.time:F2} state={state} carrying={carrying} grounded={movement.IsGrounded} vy={_visualVelocity.y:F1} inputAuth={HasInputAuthority}");
    }

    private void UpdateVisualVelocity()
    {
        Vector3 pos = transform.position;
        float dt = Time.deltaTime;

        if (_hasLastVisualPos && dt > 0f)
        {
            Vector3 delta = pos - _lastVisualPos;
            if (delta.sqrMagnitude > TeleportDistance * TeleportDistance)
            {
                _visualVelocity = Vector2.zero; // teleport / เกิดใหม่ ไม่นับเป็นการเคลื่อนที่
            }
            else
            {
                Vector2 v = new Vector2(delta.x, delta.y) / dt;
                _visualVelocity = Vector2.Lerp(_visualVelocity, v, 0.5f);
            }
        }

        _lastVisualPos = pos;
        _hasLastVisualPos = true;
    }

    // ท่าว่าย (เป็ดดำน้ำ) วาดหันคนละทางกับท่าอื่น (ดู UpdateSwimFlip) และตัวนอนราบ — UI ที่วางตามหัวต้องรู้ (FB2-8)
    // ตอน stun ท่าค้างตาม _lastLocalState จึงตรงกับภาพที่เห็นอยู่
    public bool IsSwimPose => _lastLocalState == "Swim";

    private bool IsActionActive =>
        Runner != null && CurrentAction != ActionAnim.None && !AnimationTimer.ExpiredOrNotRunning(Runner);

    // ลำดับเงื่อนไขถอดมาจากจุดที่โค้ดเดิมเคยสั่งท่า (MovementCharacter / Bird_Moveset / Duck_Moveset)
    private string ResolveState(bool isMoving)
    {
        // stun: ค้างท่าเดิมไว้ (เดิมใช้ isFrozen กันไม่ให้เปลี่ยนท่า)
        if (movement.isStunned) return _lastLocalState;

        if (IsActionActive)
        {
            string action = ActionStateName(CurrentAction);
            if (action != null && HasState(action)) return action;
        }

        // ตายแบบเพื่อนตาย (ไม่มีท่า Death) — ค้างท่าเดิมเหมือนเดิม
        if (movement.isDead) return _lastLocalState;

        bool carrying = Carrying;

        if (movement is Bird_Moveset bird)
        {
            if (bird.IsReleasingThrow) return "Throwing";
            if (bird._prepareToThrow) return "Aim";
            if (bird.IsFlying) return "Fly_Up";
            if (bird.AlreadyFloating) return "Fly";
        }
        else if (movement is Duck_Moveset duck)
        {
            if (duck.onDiving) return "Swim";
            if (movement.isWaterSurface)
            {
                if (carrying) return isMoving ? "Floating_carry_walk" : "Floating_carry_Idle";
                return isMoving ? "Floating_walk" : "Floating_Idle";
            }
        }

        bool airborne = !movement.IsGrounded && !movement.isWaterSurface && !movement.isClimbing && !BeingCarried;

        // เป็ดแบกเพื่อนกลางอากาศใช้ท่าแบกต่อ (controller ไม่มีท่ากระโดด/ตกขณะแบก — FB-8)
        if (airborne && !carrying)
        {
            if (_visualVelocity.y < FallThreshold && !movement.FallingBusy) return "Falling";
            if (movement.isJumping) return "Jump";
        }

        return carrying ? "CarryMovementTree" : "NormalMovementTree";
    }

    private string ActionStateName(ActionAnim action)
    {
        switch (action)
        {
            case ActionAnim.Interact: return "interact"; // Duck_Controller ตั้งชื่อตัวเล็ก (Bird ไม่มีท่านี้)
            case ActionAnim.Smash: return "Smash";
            case ActionAnim.Death: return "Death";
            default: return null;
        }
    }

    public void UpdateSkin(characterType skin)
    {
        currentSkin = skin;
        animator.runtimeAnimatorController = (skin == characterType.Duck) ? DuckController : BirdController;
        CacheAnimatorParameters();
        _lastLocalState = "";
    }

    private void CacheAnimatorParameters()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        parameterHashes.Clear();
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            parameterHashes.Add(param.nameHash);
        }
    }

    private bool HasParameter(string paramName) => parameterHashes.Contains(Animator.StringToHash(paramName));
    private bool HasState(string stateName) => animator.HasState(0, Animator.StringToHash(stateName));

    // ทิศหันตามแกนเดิน (FlipX sync เพราะ gameplay ใช้)
    public void UpdateAnimationController(Vector2 direction)
    {
        if (HasStateAuthority || HasInputAuthority)
        {
            if (direction.x < -0.01f) FlipX = false;
            if (direction.x > 0.01f) FlipX = true;
        }
    }

    public void FaceTo(float o)
    {
        if (o < -0.01f)
        {
            FlipX = false;
        }
        else if (o > 0.01f)
        {
            FlipX = true;
        }
    }

    // ท่าครั้งเดียว: ตั้งบน host / input authority ใน FUN แล้วทุกเครื่องเล่นตามจาก state ที่ sync
    public void PlayAction(ActionAnim action, float lockDuration)
    {
        if (Runner == null || !(HasStateAuthority || HasInputAuthority)) return;

        CurrentAction = action;
        AnimationTimer = TickTimer.CreateFromSeconds(Runner, lockDuration);
    }

    public void ClearAnimationLock()
    {
        if (Runner == null) return;

        AnimationTimer = TickTimer.None;
        CurrentAction = ActionAnim.None;
    }

    private void PlayAnimationSafeLocal(string stateName, bool restart)
    {
        if (!HasState(stateName)) return;

        if (restart || stateName == "BlendAnimation" || stateName == "NormalMovementTree" || stateName == "CarryMovementTree")
        {
            animator.Play(stateName, 0, 0f);
        }
        else
        {
            animator.Play(stateName, 0);
        }
    }

    // Overall
    public void InteractAnimation() => PlayAction(ActionAnim.Interact, 0.5f);
    public void DeathAnimation() => PlayAction(ActionAnim.Death, 10f);

    // Duck
    public void UpdateSwimFlip(Vector2 direction)
    {
        if (HasStateAuthority || HasInputAuthority)
        {
            if (direction.x < -0.01f) FlipX = true;
            if (direction.x > 0.01f) FlipX = false;
        }
    }

    public void SetFlipToFalse()
    {
        if (HasStateAuthority || HasInputAuthority) FlipX = false;
    }

    public void SmashAnimation() => PlayAction(ActionAnim.Smash, 1.27f);
}
