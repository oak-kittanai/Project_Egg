using Fusion;
using UnityEngine;

public class TrapPressure : NetworkBehaviour
{
    [Header("Stat")]
    [SerializeField] float pushForce = 3.3f;

    [Networked] public NetworkBool _isActive { get; set; }
    [Networked] public NetworkBool _isRevers { get; set; }

    [SerializeField] Vector2 defaultDirection;

    [Header("Visuals")]
    [SerializeField] Animator anim;

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

    public void OnTriggerStay2D(Collider2D collision)
    {
        if (!_isActive) return;

        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<MovementCharacter>(out var player))
            {
                if (player.HasStateAuthority || player.HasInputAuthority)
                {
                    if (player.rb2D != null)
                    {
                        Vector2 currentDirection = _isRevers ? -defaultDirection : defaultDirection;

                        player.rb2D.AddForce(currentDirection.normalized * pushForce, ForceMode2D.Force);
                    }
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