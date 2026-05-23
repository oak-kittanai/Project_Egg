using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class ClimbingVine : NetworkBehaviour, Interactable
{
    [SerializeField] private float climbUpSpeed = 0.8f;
    [SerializeField] private float fallSpeed = -5f;

    [Header("Physics...")]
    [SerializeField] private float vineGravity = 0f;
    [SerializeField] private float vineAcceleration = 5f;
    [SerializeField] private float vineDeceleration = 5f;
    [SerializeField] private float vineMaxSpeedX = 2f;

    private List<MovementCharacter> playersInTrigger = new List<MovementCharacter>();
    private List<MovementCharacter> climbingPlayers = new List<MovementCharacter>();
    private Dictionary<MovementCharacter, bool> previousEPress = new Dictionary<MovementCharacter, bool>();
    private Collider2D col;
    private Collider2D[] hitResults = new Collider2D[10];

    public override void Spawned()
    {
        col = GetComponent<Collider2D>();
    }

    public void Interact(MovementCharacter player) { }
    public bool CanInteract(MovementCharacter player) { return true; }

    public override void FixedUpdateNetwork()
    {
        if (col != null)
        {
            Vector2 center = col.bounds.center;
            Vector2 size = col.bounds.size;

            int hitCount = Runner.GetPhysicsScene2D().OverlapBox(center, size, 0f, hitResults, LayerMask.GetMask("Player"));
            List<MovementCharacter> currentPlayersInVine = new List<MovementCharacter>();

            for (int i = 0; i < hitCount; i++)
            {
                if (hitResults[i] != null && hitResults[i].TryGetComponent<MovementCharacter>(out var p))
                {
                    if (!p.isDead && p.Object != null && p.Object.IsValid)
                    {
                        currentPlayersInVine.Add(p);
                    }
                }
            }

            foreach (var p in currentPlayersInVine)
            {
                if (!playersInTrigger.Contains(p)) playersInTrigger.Add(p);
            }

            for (int i = playersInTrigger.Count - 1; i >= 0; i--)
            {
                var p = playersInTrigger[i];
                if (!currentPlayersInVine.Contains(p))
                {
                    playersInTrigger.RemoveAt(i);
                    if (climbingPlayers.Contains(p)) StopClimbing(p);
                    previousEPress.Remove(p);
                }
            }
        }

        for (int i = playersInTrigger.Count - 1; i >= 0; i--)
        {
            var player = playersInTrigger[i];

            if (player == null || !player.Object.IsValid || player.isDead) continue;

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
            if (isClimbing)
            {
                StopClimbing(player);
                return;
            }
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
                //player.rb2D.linearVelocity = 0f;
            }
        }
    }

    private void StartClimbing(MovementCharacter player)
    {
        if (!climbingPlayers.Contains(player))
        {
            climbingPlayers.Add(player);
        }

        if (player is Bird_Moveset bird)
        {
            bird.ForceCancelFlight();
        }

        player.isOptional = true;
        player.isSpeedoptional = true;

        player.FallingBusy = false;
        player.IsFalling = false;

        player.optionalGravity = vineGravity;
        player.rb2D.gravityScale = player.optionalGravity;

        //player.rb2D.linearVelocity = 0f;
    }

    private void StopClimbing(MovementCharacter player)
    {
        if (climbingPlayers.Contains(player))
        {
            climbingPlayers.Remove(player);
        }

        player.isOptional = false;
        player.isSpeedoptional = false;
        player.rb2D.gravityScale = player.normalGravity;
    }
}