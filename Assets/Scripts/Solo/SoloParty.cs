using System.Collections.Generic;
using Fusion;
using UnityEngine;

// หัวใจของโหมด Solo: คนเดียวคุมสองตัว สลับตัว ตัวตามหลัง ยกนก และนกพาบิน
// อยู่บน runner ไม่ใช่ NetworkObject เพราะ GameMode.Single ไม่มีเครื่องอื่นและไม่ย้อน tick — ค่าทั้งหมดเก็บเป็นตัวแปรธรรมดาได้
// ตัวตามหลังไม่มี input authority จึงถูกวาดด้วยเส้นทางเดียวกับ "ตัวของอีกเครื่อง" ส่วนนี้เขียน Rep* ให้ทุก tick
[DefaultExecutionOrder(100)] // หลังตัวละคร ตัวตามหลังจะได้ใช้ตำแหน่งล่าสุดของตัวหลัก
public class SoloParty : SimulationBehaviour
{
    private enum FollowMode { Trail, WaitAtShore, RidingDuck, HangingFromBird }

    private struct TrailPoint
    {
        public Vector2 Position;
        public bool Grounded;
    }

    private const int MaxTrailPoints = 300;
    private const float MinTrailStep = 0.02f;

    public static SoloParty Instance { get; private set; }
    public static bool IsActive => Instance != null;

    public Duck_Moveset Duck { get; private set; }
    public Bird_Moveset Bird { get; private set; }
    public MovementCharacter Active { get; private set; }
    public float AssistCooldownLeft => Runner != null ? _assistCooldown.RemainingTime(Runner) ?? 0f : 0f;

    private MovementCharacter Follower => Active == Duck ? Bird : Duck;

    private readonly List<TrailPoint> _trail = new List<TrailPoint>();
    private SoloSettings _settings;
    private NetworkButtons _previousButtons;
    private FollowMode _mode = FollowMode.Trail;
    private TickTimer _switchTimer;
    private TickTimer _assistCooldown;
    private TickTimer _assistFlight;
    private Vector2 _shorePoint;
    private bool _hasShorePoint;
    private bool _lifting;
    private bool _assistAirborne;
    private bool _needsTrailReset = true;
    private bool _followerFacingRight;

    public static void Begin(NetworkRunner runner)
    {
        var party = runner.gameObject.AddComponent<SoloParty>();
        runner.AddGlobal(party);
        Instance = party;
        party._settings = SoloSettings.Instance;
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

        if (_needsTrailReset)
        {
            _needsTrailReset = false;
            ResetTrail();
        }

        RecordTrail();
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
        Bird.SoloMakeFollowerBody();
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
        if (!_lifting) ResetTrail();
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
        _mode = FollowMode.Trail;
        ResetTrail();
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
            return InWater(Bird) ? FollowMode.WaitAtShore : FollowMode.Trail;
        }

        if (_lifting) return FollowMode.RidingDuck;
        return InWater(Duck) ? FollowMode.WaitAtShore : FollowMode.Trail;
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
            case FollowMode.Trail: FollowTrail(); break;
            case FollowMode.WaitAtShore: WaitAtShore(); break;
            case FollowMode.RidingDuck: RideDuck(); break;
            case FollowMode.HangingFromBird: HangFromBird(); break;
        }
    }

    private void OnModeChanged(FollowMode from, FollowMode to)
    {
        if (from == FollowMode.HangingFromBird) Duck.SoloSetCarryPose(false);
        if (to == FollowMode.HangingFromBird) Duck.SoloSetCarryPose(true);

        if (to != FollowMode.Trail) return;

        float distance = Vector2.Distance(Follower.rb2D.position, Active.rb2D.position);
        if (from != FollowMode.WaitAtShore || distance > _settings.shoreWarpDistance) ResetTrail();
    }

    private void FollowTrail()
    {
        TrailPoint target = SampleTrail(_settings.followDistance);
        Vector2 current = Follower.rb2D.position;
        float speed = Mathf.Max(_settings.catchUpSpeed, Active.CurrentVelocity.magnitude + 1f);
        Vector2 next = Vector2.MoveTowards(current, target.Position, speed * Runner.DeltaTime);
        Vector2 velocity = (next - current) / Runner.DeltaTime;

        MovementCharacter.MoveFlags flags = target.Grounded
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

    #region Trail
    private void RecordTrail()
    {
        MovementCharacter leader = Active;
        Vector2 position = leader.rb2D.position;

        if (leader.IsGrounded && !InWater(leader))
        {
            _shorePoint = position;
            _hasShorePoint = true;
        }

        if (_trail.Count > 0 && (position - _trail[_trail.Count - 1].Position).sqrMagnitude < MinTrailStep * MinTrailStep) return;

        _trail.Add(new TrailPoint { Position = position, Grounded = leader.IsGrounded });
        if (_trail.Count > MaxTrailPoints) _trail.RemoveRange(0, _trail.Count - MaxTrailPoints);
    }

    // จุดบนเส้นทางของตัวหลัก ย้อนกลับไปเท่า distance
    private TrailPoint SampleTrail(float distance)
    {
        if (_trail.Count == 0) return new TrailPoint { Position = Follower.rb2D.position, Grounded = true };

        float remaining = distance;
        for (int i = _trail.Count - 1; i > 0; i--)
        {
            TrailPoint near = _trail[i];
            TrailPoint far = _trail[i - 1];
            float step = Vector2.Distance(near.Position, far.Position);

            if (step >= remaining)
            {
                return new TrailPoint
                {
                    Position = Vector2.Lerp(near.Position, far.Position, step > 0f ? remaining / step : 0f),
                    Grounded = far.Grounded,
                };
            }
            remaining -= step;
        }
        return _trail[0];
    }

    // เริ่มเส้นทางใหม่จากข้างหลังตัวหลัก (ไม่ทะลุกำแพง) แล้ววางตัวตามหลังตรงนั้น
    private void ResetTrail()
    {
        Vector2 lead = Active.rb2D.position;
        Vector2 back = Active.GetMoveFlag(MovementCharacter.MoveFlags.FacingRight) ? Vector2.left : Vector2.right;

        float distance = _settings.followDistance;
        RaycastHit2D wall = Physics2D.Raycast(lead, back, distance, LayerMask.GetMask("Ground", "Platform"));
        if (wall.collider != null) distance = Mathf.Max(wall.distance - 0.1f, 0f);

        Vector2 start = lead + back * distance;

        _trail.Clear();
        _trail.Add(new TrailPoint { Position = start, Grounded = Active.IsGrounded });
        _trail.Add(new TrailPoint { Position = lead, Grounded = Active.IsGrounded });

        Follower.SoloSetFollowerState(start, Vector2.zero, Active.IsGrounded ? MovementCharacter.MoveFlags.Grounded : MovementCharacter.MoveFlags.InAir);
    }
    #endregion

    #region Events
    public void OnCharacterRespawned(MovementCharacter character)
    {
        if (character != Active) character.SoloMakeFollowerBody();

        _lifting = false;
        _assistAirborne = false;
        _assistFlight = TickTimer.None;
        _mode = FollowMode.Trail;
        _hasShorePoint = false;
        _needsTrailReset = true;

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
