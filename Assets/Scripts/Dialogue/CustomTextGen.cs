using UnityEngine;
using TMPro;
using DG.Tweening;

public class CustomTextGen : MonoBehaviour
{
    [SerializeField] private TMP_Text textLabel;
    private Tween currentTween;

    public void StartEffect(string message, TextEffectType effect)
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }

        textLabel.text = "";

        switch (effect)
        {
            case TextEffectType.Typewriter:
                currentTween = textLabel.DOText(message, 1.5f).SetEase(Ease.Linear);
                break;

            case TextEffectType.Shake:
                textLabel.text = message;
                currentTween = transform.DOShakePosition(0.5f, 5f, 10, 90, false, true);
                break;

            default:
                textLabel.text = message;
                break;
        }
    }

    public void SkipEffect()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Complete();
        }
    }
}