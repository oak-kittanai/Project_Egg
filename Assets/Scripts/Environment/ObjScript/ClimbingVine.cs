using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class ClimbingVine : NetworkBehaviour, Interactable
{
    [Header("Settings")]
    [SerializeField] private float climbUpSpeed = 0.8f;
    [SerializeField] private float fallSpeed = -5f;

    [Header("Vine Physics")]
    [SerializeField] private float vineGravity = 0f;
    [SerializeField] private float vineAcceleration = 5f;
    [SerializeField] private float vineDeceleration = 5f;
    [SerializeField] private float vineMaxSpeedX = 2f;

    //เก็บ List ผู้เล่น
    private List<MovementCharacter> playersInTrigger = new List<MovementCharacter>();

    //เก็บสถานะการเกาะเถาวัลย์
    private List<MovementCharacter> climbingPlayers = new List<MovementCharacter>();

    //กัน Spame ปุ่ม
    private Dictionary<MovementCharacter, bool> previousEPress = new Dictionary<MovementCharacter, bool>();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<MovementCharacter>(out var player))
        {
            if (!playersInTrigger.Contains(player))
            {
                playersInTrigger.Add(player);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent<MovementCharacter>(out var player))
        {
            playersInTrigger.Remove(player);

            if (climbingPlayers.Contains(player))
            {
                StopClimbing(player);
            }
        }
    }
    public void Interact(MovementCharacter player)
    {

    }

    public override void FixedUpdateNetwork()
    {
        for (int i = playersInTrigger.Count - 1; i >= 0; i--)
        {
            var player = playersInTrigger[i];

            if (player == null || !player.Object.IsValid || player.isDead)
            {
                playersInTrigger.RemoveAt(i);
                climbingPlayers.Remove(player);
                previousEPress.Remove(player);
                continue;
            }

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
        bool pressedE = input.Keyboard_E && !wasPressed;
        previousEPress[player] = input.Keyboard_E;

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
            if (input.jump)
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
                player.rb2D.linearVelocity = new Vector2(player.rb2D.linearVelocity.x, 0f);
            }
        }
    }

    private void StartClimbing(MovementCharacter player)
    {
        if (!climbingPlayers.Contains(player))
        {
            climbingPlayers.Add(player);
        }

        player.isOptional = true;
        player.isSpeedoptional = true;

        player.FallingBusy = false;
        player.IsFalling = false;

        player.optionalGravity = vineGravity;
        player.rb2D.gravityScale = player.optionalGravity;

        player.accelerationSpeedOptional = vineAcceleration;
        player.decelerationSpeedOptional = vineDeceleration;
        player.optionalMaxSpeed = vineMaxSpeedX;

        player.rb2D.linearVelocity = new Vector2(player.rb2D.linearVelocity.x, 0f);
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