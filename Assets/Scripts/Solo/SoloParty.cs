using Fusion;
using UnityEngine;

// หัวใจของโหมด Solo: คนเดียวคุมสองตัว สลับตัว ตัวตามหลัง ยกนก และนกพาบิน
// อยู่บน runner ไม่ใช่ NetworkObject เพราะ GameMode.Single ไม่มีเครื่องอื่นและไม่ย้อน tick — ค่าทั้งหมดเก็บเป็นตัวแปรธรรมดาได้
// ตัวตามหลัง (MovementCharacter.IsSoloFollower) ถูกวาดด้วยเส้นทางเดียวกับ "ตัวของอีกเครื่อง" ส่วนนี้เขียน Rep* ให้ทุก tick
[DefaultExecutionOrder(100)] // หลังตัวละคร ตัวตามหลังจะได้ใช้ตำแหน่งล่าสุดของตัวหลัก
public class SoloParty : SimulationBehaviour
{
    private enum FollowMode { Follow, WaitAtShore, RidingDuck, HangingFromBird }

    private const float GroundProbeStart = 1.2f;
    private const float GroundProbeLength = 5f;
    private const float MinSideGap = 0.6f;
    private const float CatchUpPerUnit = 6f;

    public static SoloParty Instance { get; private set; }
    public static bool IsActive => Instance != null;

    public Duck_Moveset Duck { get; private set; }
    public Bird_Moveset Bird { get; private set; }
    public MovementCharacter Active { get; private set; }
    public float AssistCooldownLeft => Runner != null ? _assistCooldown.RemainingTime(Runner) ?? 0f : 0f;

    private MovementCharacter Follower => Active == Duck ? Bird : Duck;

    private SoloSettings _settings;
    private NetworkButtons _previousButtons;
    private FollowMode _mode = FollowMode.Follow;
    private TickTimer _switchTimer;
    private TickTimer _assistCooldown;
    private TickTimer _assistFlight;
    private Vector2 _shorePoint;
    private bool _hasShorePoint;
    private bool _lifting;
    private bool _assistAirborne;
    private bool _needsPlacement = true;
    private float _groundOffset = 0.5f;
    private int _groundMask;
    private int _waterMask;
    private bool _followerFacingRight;

    public static void Begin(NetworkRunner runner)
    {
        var party = runner.gameObject.AddComponent<SoloParty>();
        runner.AddGlobal(party);
        Instance = party;
        party._settings = SoloSettings.Instance;
        party._groundMask = LayerMask.GetMask("Ground", "Platform");
        party._waterMask = LayerMask.GetMask("Water");
        SoloHud.Create(runner.transform);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public override void FixedUpdateNetwork()
    {
        if (!FindCharacters()) return;

        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Object == null || !gm.Object.IsValid || !gm.IsGameReady) return;

        ReadButtons();

        if (Active.isDead || Follower.isDead) return;

        if (_needsPlacement)
        {
            _needsPlacement = false;
            PlaceBehindLeader();
        }

        RememberGround();
        UpdateFollower();
    }

    private bool FindCharacters()
    {
        if (Duck != null && Bird != null) return true;

        foreach (var character in FindObjectsByType<MovementCharacter>(FindObjectsSortMode.None))
        {
            if (character.Object == null || !character.Object.IsValid) continue;
            if (character is Duck_Moveset duck) Duck = duck;
            else if (character is Bird_Moveset bird) Bird = bird;
        }

        if (Duck == null || Bird == null) return false;

        Active = Duck;
        Bird.SoloBecomeFollower();
        return true;
    }

    #region Input
    private void ReadButtons()
    {
        if (!Runner.TryGetInputForPlayer<NetworkInputData>(Runner.LocalPlayer, out var input)) return;

        bool switchPressed = input.Buttons.WasPressed(_previousButtons, InputButton.Switch);
        bool liftPressed = input.Buttons.WasPressed(_previousButtons, InputButton.Lift);
        _previousButtons = input.Buttons;

        if (GameplayPause.Frozen || Active.isDead) return;

        if (switchPressed) OnSwitchPressed();
        if (liftPressed) OnLiftPressed();
    }

    private void OnSwitchPressed()
    {
        // บนผิวน้ำปุ่มนี้คือสกิลเพื่อน สลับเป็นนกตรงนั้นนกจะจมทันที
        if (Active == Duck && Duck.isWaterSurface && !Duck.onDiving)
        {
            TryAssist();
            return;
        }

        if (CanSwitch()) Switch();
    }

    private void OnLiftPressed()
    {
        if (Active != Duck || Duck.onDiving || !Duck.IsGrounded) return;

        SetLifting(!_lifting);
        if (!_lifting) PlaceBehindLeader();
    }
    #endregion

    #region Switch
    private bool CanSwitch()
    {
        if (!_switchTimer.ExpiredOrNotRunning(Runner)) return false;
        if (Active.isStunned || Active.isClimbing) return false;

        if (Active == Duck) return !Duck.onDiving && !Duck.emergencySwimBool && !Duck.IsHeadUnderwater;

        if (Bird.IsFlying || Bird.AlreadyFloating || Bird.IsAlreadyFly) return false;
        if (Bird._prepareToThrow || Bird.IsReleasingThrow) return false;
        if (Bird.stilldrowning && !Duck.isDiveUnlocked) return false;
        return true;
    }

    private void Switch()
    {
        MovementCharacter from = Active;
        MovementCharacter to = Follower;

        bool rescue = from == Bird && Bird.stilldrowning;
        float air = rescue ? Bird.SoloRemainingAir : 0f;

        SetLifting(false);
        to.SoloBecomeLeader(from);
        Active = to;

        if (rescue) Duck.SoloContinueDive(air);

        CameraCharacter rig = CameraCharacter.LocalCamera != null ? CameraCharacter.LocalCamera.GetComponentInParent<CameraCharacter>() : null;
        if (rig != null) rig.SetFollowTarget(to.visualTransform);

        _switchTimer = TickTimer.CreateFromSeconds(Runner, _settings.switchCooldown);
        _mode = FollowMode.Follow;
        PlaceBehindLeader();
    }
    #endregion

    #region Lift & Assist
    private void SetLifting(bool on)
    {
        if (_lifting == on) return;

        _lifting = on;
        _assistAirborne = false;
        _assistFlight = TickTimer.None;
        Duck.SoloSetCarrying(Bird, on);
        Duck.SoloHideCarryPose = false;
        Bird.SoloSetHidden(false);
    }

    private void TryAssist()
    {
        if (!_lifting || !Bird.isFlyUnlocked || !_assistCooldown.ExpiredOrNotRunning(Runner)) return;

        _assistFlight = TickTimer.CreateFromSeconds(Runner, Bird.CarryFlyTime);
        _assistCooldown = TickTimer.CreateFromSeconds(Runner, _settings.assistCooldown);
        _assistAirborne = true;
    }
    #endregion

    #region Follower
    private static bool InWater(MovementCharacter character)
    {
        return character.isWaterSurface || character.stilldrowning || character.IsHeadUnderwater
            || (character is Duck_Moveset duck && duck.onDiving);
    }

    private FollowMode ResolveMode()
    {
        if (Active == Bird)
        {
            if (Bird.IsFlying || Bird.AlreadyFloating || (Bird.IsAlreadyFly && !Bird.IsGrounded)) return FollowMode.HangingFromBird;
            return InWater(Bird) ? FollowMode.WaitAtShore : FollowMode.Follow;
        }

        if (_lifting) return FollowMode.RidingDuck;
        return InWater(Duck) ? FollowMode.WaitAtShore : FollowMode.Follow;
    }

    private void UpdateFollower()
    {
        FollowMode mode = ResolveMode();
        if (mode != _mode)
        {
            OnModeChanged(_mode, mode);
            _mode = mode;
        }

        switch (mode)
        {
            case FollowMode.Follow: FollowLeader(); break;
            case FollowMode.WaitAtShore: WaitAtShore(); break;
            case FollowMode.RidingDuck: RideDuck(); break;
            case FollowMode.HangingFromBird: HangFromBird(); break;
        }
    }

    private void OnModeChanged(FollowMode from, FollowMode to)
    {
        if (from == FollowMode.HangingFromBird) Duck.SoloSetCarryPose(false);
        if (to == FollowMode.HangingFromBird) Duck.SoloSetCarryPose(true);

        if (to != FollowMode.Follow) return;

        float distance = Vector2.Distance(Follower.rb2D.position, Active.rb2D.position);
        if (from != FollowMode.WaitAtShore || distance > _settings.shoreWarpDistance) PlaceBehindLeader();
    }

    // อยู่ข้างหลังตามทิศที่ตัวหลักหัน บนพื้นจริง แบบสัตว์เลี้ยง — ไม่มี collider จึงเดินซ้อนกันได้ตอนตัวหลักหันกลับ
    private void FollowLeader()
    {
        MovementCharacter leader = Active;
        Vector2 lead = leader.rb2D.position;
        Vector2 current = Follower.rb2D.position;

        if (leader.IsGrounded && Vector2.Distance(current, lead) > _settings.shoreWarpDistance)
        {
            PlaceBehindLeader();
            return;
        }

        float targetX = BehindX(lead);
        float gap = Mathf.Abs(targetX - current.x);
        // ยิ่งห่างจากจุดข้างหลังยิ่งเดินเร็ว ตอนตัวหลักเดินจึงไม่ทิ้งระยะ
        float speed = Mathf.Min(_settings.walkSpeed + gap * CatchUpPerUnit, _settings.catchUpSpeed);
        float nextX = Mathf.MoveTowards(current.x, targetX, speed * Runner.DeltaTime);

        bool onGround = TryFindGround(nextX, Mathf.Max(current.y, lead.y), out float groundY);

        // ข้างหน้าเป็นหลุม: รอที่ขอบจนตัวหลักลงพื้น แล้วค่อยกระโดดตามไป
        if (!onGround && !leader.IsGrounded)
        {
            nextX = current.x;
            onGround = TryFindGround(nextX, current.y, out groundY);
        }

        float targetY = onGround ? groundY : lead.y;
        float nextY = Mathf.MoveTowards(current.y, targetY, _settings.hopSpeed * Runner.DeltaTime);
        Vector2 next = new Vector2(nextX, nextY);
        Vector2 velocity = (next - current) / Runner.DeltaTime;

        MovementCharacter.MoveFlags flags = onGround && Mathf.Abs(nextY - targetY) < 0.05f
            ? MovementCharacter.MoveFlags.Grounded
            : MovementCharacter.MoveFlags.InAir | (velocity.y > 0.1f ? MovementCharacter.MoveFlags.Jumping : MovementCharacter.MoveFlags.None);

        SetFollower(next, velocity, flags);
    }

    private void WaitAtShore()
    {
        Vector2 current = Follower.rb2D.position;
        Vector2 target = _hasShorePoint ? _shorePoint : current;
        Vector2 next = Vector2.MoveTowards(current, target, _settings.catchUpSpeed * Runner.DeltaTime);
        SetFollower(next, (next - current) / Runner.DeltaTime, MovementCharacter.MoveFlags.Grounded);
    }

    private void RideDuck()
    {
        bool flying = _assistFlight.IsRunning && !_assistFlight.Expired(Runner);
        if (_assistFlight.IsRunning && !flying) _assistFlight = TickTimer.None;

        if (_assistAirborne && !flying && (Duck.IsGrounded || Duck.isWaterSurface)) _assistAirborne = false;

        // กระโดดเอง: ไม่มีท่าแบกกลางอากาศ เล่นแค่ท่าเป็ดกระโดด / นกพาบิน: เห็นทั้งคู่จนลง
        bool hideBird = Duck.IsInAir && !_assistAirborne;
        Duck.SoloHideCarryPose = hideBird;
        Bird.SoloSetHidden(hideBird);

        MovementCharacter.MoveFlags flags = Duck.IsGrounded ? MovementCharacter.MoveFlags.Grounded : MovementCharacter.MoveFlags.InAir;
        if (flying) flags |= MovementCharacter.MoveFlags.Flying;

        Vector2 position = Duck.rb2D.position + Vector2.up * Bird.betweenCarryPosition;
        SetFollower(position, Duck.CurrentVelocity, flags);
    }

    private void HangFromBird()
    {
        Vector2 position = Bird.rb2D.position + Vector2.down * Duck.betweenCarryPosition;
        SetFollower(position, Bird.CurrentVelocity, MovementCharacter.MoveFlags.InAir);
    }

    private void SetFollower(Vector2 position, Vector2 velocity, MovementCharacter.MoveFlags flags)
    {
        if (Mathf.Abs(velocity.x) > 0.05f) _followerFacingRight = velocity.x > 0f;
        if (_followerFacingRight) flags |= MovementCharacter.MoveFlags.FacingRight;
        Follower.SoloSetFollowerState(position, velocity, flags);
    }
    #endregion

    #region Placement
    // จำจุดยืนล่าสุด (รอบนฝั่ง) และความสูงจากพื้นถึงตัว (วางตัวตามหลังให้เท้าแตะพื้น)
    private void RememberGround()
    {
        MovementCharacter leader = Active;
        if (!leader.IsGrounded || InWater(leader)) return;

        Vector2 position = leader.rb2D.position;
        _shorePoint = position;
        _hasShorePoint = true;

        RaycastHit2D hit = Physics2D.Raycast(position, Vector2.down, GroundProbeLength, _groundMask);
        if (hit.collider != null) _groundOffset = hit.distance;
    }

    private float BehindX(Vector2 lead)
    {
        float direction = Active.GetMoveFlag(MovementCharacter.MoveFlags.FacingRight) ? -1f : 1f;

        float room = FreeDistance(lead, direction);
        if (room < MinSideGap)
        {
            direction = -direction;
            room = FreeDistance(lead, direction);
        }
        return lead.x + direction * room;
    }

    // ระยะว่างด้านนั้นจนชนกำแพงหรือสุดขอบพื้น ไม่เกิน followDistance
    private float FreeDistance(Vector2 lead, float direction)
    {
        float distance = _settings.followDistance;

        RaycastHit2D wall = Physics2D.Raycast(lead, Vector2.right * direction, distance, _groundMask);
        if (wall.collider != null) distance = Mathf.Max(wall.distance - 0.1f, 0f);

        if (!Active.IsGrounded) return distance;

        // ยืนอยู่ปลายขอบ: ไม่ลอยออกไปนอกพื้น
        for (float check = distance; check > 0.05f; check -= 0.2f)
        {
            if (TryFindGround(lead.x + direction * check, lead.y, out _)) return check;
        }
        return 0f;
    }

    private bool TryFindGround(float x, float nearY, out float y)
    {
        Vector2 origin = new Vector2(x, nearY + GroundProbeStart);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, GroundProbeLength, _groundMask);

        y = nearY;
        if (hit.collider == null || hit.distance <= 0f) return false;

        y = hit.point.y + _groundOffset;
        return Physics2D.OverlapPoint(new Vector2(x, y), _waterMask) == null;
    }

    // วางตัวตามหลังข้างหลังตัวหลักทันที
    private void PlaceBehindLeader()
    {
        Vector2 lead = Active.rb2D.position;
        float x = BehindX(lead);
        bool grounded = TryFindGround(x, lead.y, out float y);

        Vector2 position = new Vector2(x, grounded ? y : lead.y);
        MovementCharacter.MoveFlags flags = grounded ? MovementCharacter.MoveFlags.Grounded : MovementCharacter.MoveFlags.InAir;
        if (x < lead.x) _followerFacingRight = true;
        else if (x > lead.x) _followerFacingRight = false;
        SetFollower(position, Vector2.zero, flags);
    }
    #endregion

    #region Events
    public void OnCharacterRespawned(MovementCharacter character)
    {
        if (character != Active) character.SoloMakeFollowerBody();

        _lifting = false;
        _assistAirborne = false;
        _assistFlight = TickTimer.None;
        _mode = FollowMode.Follow;
        _hasShorePoint = false;
        _needsPlacement = true;

        if (Duck != null) Duck.SoloHideCarryPose = false;
        if (Bird != null) Bird.SoloSetHidden(false);
    }

    public void UnlockSkill(MovementCharacter.SkillType skill)
    {
        switch (skill)
        {
            case MovementCharacter.SkillType.Duck_Dive:
                Duck.isDiveUnlocked = true;
                PlayerInterface._duckDiveUnlocked = true;
                break;
            case MovementCharacter.SkillType.Duck_Smash:
                Duck.isSmashUnlocked = true;
                PlayerInterface._duckSmashUnlocked = true;
                break;
            case MovementCharacter.SkillType.Bird_Fly:
                Bird.isFlyUnlocked = true;
                PlayerInterface._birdFlyUnlocked = true;
                break;
            case MovementCharacter.SkillType.Bird_Throw:
                Bird.isThrowUnlocked = true;
                PlayerInterface._birdThrowUnlocked = true;
                break;
        }
    }
    #endregion
}
