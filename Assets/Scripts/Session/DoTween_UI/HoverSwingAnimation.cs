using DG.Tweening;
using UnityEngine;

public class HoverSwingAnimation : MonoBehaviour
{
    [SerializeField] Sequence hoverSeq;

    public void OnPointerEnter()
    {
        hoverSeq?.Kill();
        hoverSeq = DOTween.Sequence();

        hoverSeq.Append(transform.DOLocalRotate(new Vector3(0, 0, 5), 0.15f).SetEase(Ease.InOutQuart));

        hoverSeq.Append(transform.DOLocalRotate(new Vector3(0, 0, -5), 1f)
                .SetEase(Ease.InOutQuad)
                .SetLoops(-1, LoopType.Yoyo));
    }

    public void OnPointerExit()
    {
        hoverSeq?.Kill();
        transform.DOLocalRotate(Vector3.zero, 0.2f);
    }
}