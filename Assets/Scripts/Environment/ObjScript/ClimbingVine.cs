using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class ClimbingVine : NetworkBehaviour, Interactable
{
    [SerializeField] private float climbUpSpeed = 0.8f;
    [SerializeField] private float fallSpeed = -5f;
    [SerializeField] private float autoSlideDownSpeed = -1.5f;

    [Header("Physics")]
    [SerializeField] private float vineGravity = 0f;
    [SerializeField] private float vineAcceleration = 5f;
    [SerializeField] private float vineDeceleration = 5f;
    [SerializeField] private float vineMaxSpeedX = 2f;

    private List<MovementCharacter> playersInTrigger = new List<MovementCharacter>();
    private List<MovementCharacter> climbingPlayers = new List<MovementCharacter>();
    private Dictionary<MovementCharacter, bool> previousEPress = new Dictionary<MovementCharacter, bool>();

    private Collider2D col;
    private ContactFilter2D playerFilter;
    private List<Collider2D> overlappedColliders = new List<Collider2D>();

    public override void Spawned()
    {
        col = GetComponent<Collider2D>();

        playerFilter.SetLayerMask(LayerMask.GetMask("Player"));
        playerFilter.useLayerMask = true;
    }

    private void RemovePlayer(MovementCharacter player)
    {
        if (playersInTrigger.Contains(player)) playersInTrigger.Remove(player);
        if (climbingPlayers.Contains(player)) StopClimbing(player);
        if (previousEPress.ContainsKey(player)) previousEPress.Remove(player);
    }

    public void Interact(MovementCharacter player) { }
    public bool CanInteract(MovementCharacter player) { return true; }

    public override void FixedUpdateNetwork()
    {
        if (col == null) return;

        int count = col.OverlapCollider(playerFilter, overlappedColliders);

        List<MovementCharacter> currentPlayers = new List<MovementCharacter>();
        for (int i = 0; i < count; i++)
        {
            if (overlappedColliders[i].TryGetComponent<MovementCharacter>(out var p))
            {
                if (p.Object != null && p.Object.IsValid && !p.isDead && p.gameObject.activeInHierarchy)
                {
                    currentPlayers.Add(p);
                }
            }
        }

        for (int i = playersInTrigger.Count - 1; i >= 0; i--)
        {
            var p = playersInTrigger[i];
            if (!currentPlayers.Contains(p)) RemovePlayer(p);
        }

        foreach (var p in currentPlayers)
        {
            if (!playersInTrigger.Contains(p)) playersInTrigger.Add(p);
        }

        foreach (var player in playersInTrigger)
        {
            if (player.HasStateAuthority || player.HasInputAuthority)
            {
                if (player.GetInput(out NetworkInputData input))
                {
                    HandleClimbing(player, input);
                }
            }
        }
    }

    private void HandleClimbing(MovementCharacter player, NetworkInputData input)
    {
        bool isClimbing = climbingPlayers.Contains(player);
        bool wasPressed = previousEPress.ContainsKey(player) && previousEPress[player];
        bool pressedE = input.KeybindInteract && !wasPressed;
        previousEPress[player] = input.KeybindInteract;

        if (pressedE)
        {
            if (isClimbing) StopClimbing(player);
            else
            {
                StartClimbing(player);
                isClimbing = true;
            }
        }

        if (isClimbing)
        {
            if (input.KeybindJump)
            {
                StopClimbing(player);
                return;
            }

            bool pressingUp = input.vertical > 0.1f;
            bool pressingDown = input.vertical < -0.1f;

            if (pressingUp)
            {
                float climbVel = player.stats.s_walkSpeed * climbUpSpeed;
                player.rb2D.linearVelocity = new Vector2(player.rb2D.linearVelocity.x, climbVel);
            }
            else if (pressingDown)
            {
                player.rb2D.linearVelocity = new Vector2(player.rb2D.linearVelocity.x, fallSpeed);
            }
            else
            {
                player.rb2D.linearVelocity = new Vector2(player.rb2D.linearVelocity.x, autoSlideDownSpeed);
            }
        }
    }

    private void StartClimbing(MovementCharacter player)
    {
        if (!climbingPlayers.Contains(player)) climbingPlayers.Add(player);

        if (player is Bird_Moveset bird) bird.ForceCancelFlight();

        player.isOptional = true;
        player.isSpeedoptional = true;
        player.FallingBusy = false;
        player.IsFalling = false;
        player.optionalGravity = vineGravity;
        player.rb2D.gravityScale = player.optionalGravity;
    }

    private void StopClimbing(MovementCharacter player)
    {
        if (climbingPlayers.Contains(player)) climbingPlayers.Remove(player);

        player.isOptional = false;
        player.isSpeedoptional = false;
        player.rb2D.gravityScale = player.normalGravity;
    }
}