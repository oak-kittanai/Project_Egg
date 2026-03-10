using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class TrapPressure : NetworkBehaviour
{
    [Header("Stat&Mode")]
    [SerializeField] float pushForce = 50f;
    [SerializeField] ForceMode2D forceMode = ForceMode2D.Force;

    [Networked] public NetworkBool _isActive { get; set; }
    [Networked] public NetworkBool _isRevers { get; set; }

    [SerializeField] Vector2 defaultDirection;

    [Header("Visuals")]
    [SerializeField] Animator anim;

    private List<MovementCharacter> playersInTrap = new List<MovementCharacter>();

    public void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
        defaultDirection = transform.up;
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            _isActive = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && collision.TryGetComponent<MovementCharacter>(out var player))
        {
            if (!playersInTrap.Contains(player))
            {
                playersInTrap.Add(player);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        //if (collision.CompareTag("Player") && collision.TryGetComponent<MovementCharacter>(out var player))
        //{
        //    if (playersInTrap.Contains(player))
        //    {
        //        playersInTrap.Remove(player);
        //    }
        //}

        if (collision.CompareTag("Player") && collision.TryGetComponent<MovementCharacter>(out var player))
        {
            if (playersInTrap.Contains(player))
            {
                playersInTrap.Remove(player);

                if (player.rb2D != null)
                {
                    Vector2 currentDirection = _isRevers ? -defaultDirection : defaultDirection;

                    float velocityInDirection = Vector2.Dot(player.rb2D.velocity, currentDirection.normalized);

                    if (velocityInDirection > 0)
                    {
                        player.rb2D.velocity -= currentDirection.normalized * velocityInDirection * 0.8f;
                    }
                }
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        playersInTrap.RemoveAll(p => p == null || !p.Object.IsValid);

        if (!_isActive) return;

        foreach (var player in playersInTrap)
        {
            if (HasStateAuthority)
            {
                if (player.rb2D != null)
                {
                    if (player.rb2D.IsSleeping()) player.rb2D.WakeUp();

                    Vector2 currentDirection = _isRevers ? -defaultDirection : defaultDirection;

                    player.rb2D.AddForce(currentDirection.normalized * pushForce, forceMode);
                }
            }
        }
    }

    public void SetTrapActive(bool active)
    {
        if (HasStateAuthority) _isActive = active;
        else RPC_SetTrapActive(active);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SetTrapActive(NetworkBool active) => _isActive = active;

    public void ChangeDirection(bool isRevers)
    {
        if (HasStateAuthority) _isRevers = isRevers;
        else RPC_ChangeDirection(isRevers);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ChangeDirection(NetworkBool isRevers) => _isRevers = isRevers;

    public override void Render()
    {
        if (anim != null)
        {
            anim.SetBool("isPulling", _isRevers);
            anim.SetBool("isReversed", _isRevers);
        }
    }
}