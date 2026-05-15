using UnityEngine;
using DG.Tweening;

public class ButtonPopupAnimation : MonoBehaviour
{
    private Vector3 originalScale;
    private Sequence hoverSeq;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        hoverSeq?.Kill();
        hoverSeq = DOTween.Sequence();

        hoverSeq.Append(transform.DOScale(originalScale, 0.5f)
                .From(Vector3.zero)
                .SetEase(Ease.OutBack));
    }

    private void OnDisable()
    {
        hoverSeq?.Kill();
        transform.localScale = Vector3.zero;
    }
}
