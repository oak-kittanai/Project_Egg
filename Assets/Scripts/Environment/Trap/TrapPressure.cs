using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class TrapPressure : NetworkBehaviour
{
    [Header("Stat&Mode")]
    [SerializeField] float pushForce = 50f;
    [Networked] public NetworkBool _isActive { get; set; }
    [Header("Wind Fluctuation")]
    [SerializeField] float fluctuationSpeed = 5f; // ลมกระเพื่อม
    [SerializeField] float fluctuationAmount = 0.2f; // ขนาดการกระเพื่อม
    [Header("Wind Falloff")]
    [SerializeField] float maxWindDistance = 5f; // 2.162273
    [Networked, OnChangedRender(nameof(OnDirectionChanged))]
    public NetworkBool _isRevers { get; set; }
    [SerializeField] Vector2 defaultDirection;
    [Header("Visuals")]
    [SerializeField] Animator anim;

    private Collider2D[] hitResults = new Collider2D[10];

    private List<MovementCharacter> playersInTrap = new List<MovementCharacter>();

    public void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
        //defaultDirection = transform.up; เอาทิศ transform.up ของ Obj
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            _isActive = true;
        }

        OnDirectionChanged();
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
        if (Object == null || !Object.IsValid) return;

        if (collision.CompareTag("Player") && collision.TryGetComponent<MovementCharacter>(out var player))
        {
            if (playersInTrap.Contains(player))
            {
                playersInTrap.Remove(player);

                if (player.rb2D != null)
                {
                    Vector2 currentDirection = _isRevers ? -defaultDirection : defaultDirection;

                    float velocityInDirection = Vector2.Dot(player.rb2D.linearVelocity, currentDirection.normalized);

                    if (velocityInDirection > 0)
                    {
                        player.rb2D.linearVelocity -= currentDirection.normalized * velocityInDirection * 0.8f;
                    }
                }
            }
        }
    }
    #region FixedUpdateNetwork 
    public override void FixedUpdateNetwork()
    {
        if (!_isActive) return;

        int hitCount = Runner.GetPhysicsScene2D().OverlapBox(
            transform.position,
            transform.localScale,
            0,
            hitResults,
            LayerMask.GetMask("Player")
        );

        List<MovementCharacter> processedPlayers = new List<MovementCharacter>();

        for (int i = 0; i < hitCount; i++)
        {
            var hit = hitResults[i];
            if (hit != null && hit.TryGetComponent<MovementCharacter>(out var player))
            {
                if (processedPlayers.Contains(player)) continue;
                processedPlayers.Add(player);

                if (HasStateAuthority || player.HasInputAuthority)
                {
                    ApplyTrapForce(player);
                }
            }
        }
    }

    private void ApplyTrapForce(MovementCharacter player)
    {
        if (player.rb2D == null) return;
        if (player.rb2D.IsSleeping()) player.rb2D.WakeUp();

        Vector2 currentDirection = _isRevers ? -defaultDirection : defaultDirection;
        float currentSpeedInDir = Vector2.Dot(player.rb2D.linearVelocity, currentDirection.normalized);
        Vector2 offset = (Vector2)player.transform.position - (Vector2)transform.position;
        float distanceInWindDir = Vector2.Dot(offset, currentDirection.normalized);
        float distanceMultiplier = 1f - Mathf.Clamp01(distanceInWindDir / maxWindDistance);
        float baseTargetSpeed = 15f;
        float sineWave = Mathf.Sin(Runner.SimulationTime * fluctuationSpeed);
        float fluctuatedTargetSpeed = baseTargetSpeed * (1f + (sineWave * fluctuationAmount));

        if (currentSpeedInDir < fluctuatedTargetSpeed)
        {
            float speedDifference = fluctuatedTargetSpeed - currentSpeedInDir;
            float appliedForce = pushForce * speedDifference * distanceMultiplier;

            player.rb2D.AddForce(currentDirection.normalized * appliedForce, ForceMode2D.Force);
        }
    }
    #endregion

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

    public void OnDirectionChanged()
    {
        if (anim != null)
        {
            anim.SetBool("isPulling", _isRevers);
            anim.SetBool("isReversed", _isRevers);
        }
    }
}