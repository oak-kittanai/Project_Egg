using Fusion;
using System.Collections;
using UnityEngine;

public class DamageFlash : NetworkBehaviour
{
    [Header("Ref")]
    [SerializeField] SpriteRenderer[] _spriteRenderers;
    [SerializeField] Material[] _materials;

    [Header("Setting")]
    [ColorUsage(true, true)]
    [SerializeField] private Color _flashColor = Color.white;
    [SerializeField] private AnimationCurve _flashCurve;

    [Networked] public TickTimer _damageFlashTimer { get; set; }
    public float flashDuration = 0.25f;

    private void Awake()
    {
        _spriteRenderers = GetComponents<SpriteRenderer>();

        Init();
    }

    private void Init()
    {
        _materials = new Material[_spriteRenderers.Length];

        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            _materials[i] = _spriteRenderers[i].material;

            _materials[i].SetColor("_FlashColor", _flashColor);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void CallDamageFlash_RPC()
    {
        _damageFlashTimer = TickTimer.CreateFromSeconds(Runner, flashDuration);
    }

    public override void Render()
    {
        if (_damageFlashTimer.IsRunning)
        {
            float timeRemaining = _damageFlashTimer.RemainingTime(Runner) ?? 0f;

            float percentComplete = 1f - (timeRemaining / flashDuration);

            float currentFlashAmount = _flashCurve.Evaluate(percentComplete);

            SetFlashAmount(currentFlashAmount);
        }
        else if (_damageFlashTimer.Expired(Runner))
        {
            SetFlashAmount(0f);

            _damageFlashTimer = TickTimer.None;
        }
    }

    private void SetFlashAmount(float amount)
    {
        for (int i = 0; i < _materials.Length; ++i)
        {
            _materials[i].SetFloat("_FlashAmount", amount);
        }
    }
}
