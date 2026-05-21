using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using DG.Tweening;

public class SkillGUI : MonoBehaviour
{
    [Header("Skill Setting")]
    [SerializeField] bool isSkillDiscover;

    [SerializeField] Image skillProfile;
    [SerializeField] Sprite skillProfile_Pic;
    [SerializeField] Sprite skillProfile_Pic_Cooldown;

    [SerializeField] TMP_Text skillCooldownText;

    [SerializeField] bool isSkillKeyPressShort;

    [SerializeField] Image longSkillKey;
    [SerializeField] Sprite longSkill;
    [SerializeField] Sprite longSkillPressed;

    [SerializeField] Image shortSkillKey;
    [SerializeField] Sprite shortSkill;
    [SerializeField] Sprite shortSkillPressed;

    private void Start()
    {
        if (!isSkillDiscover)
        {
            transform.localScale = Vector3.zero;
        }
    }

    private void OnEnable()
    {
        longSkillKey.enabled = !isSkillKeyPressShort;
        shortSkillKey.enabled = isSkillKeyPressShort;
    }

    public void SetPressed(bool isKeyPress)
    {
        if (isSkillKeyPressShort)
            shortSkillKey.sprite = isKeyPress ? shortSkillPressed : shortSkill;
        else
            longSkillKey.sprite = isKeyPress ? longSkillPressed : longSkill;
    }

    public void UpdateCooldown(TickTimer timer, NetworkRunner runner)
    {
        if (timer.IsRunning && !timer.Expired(runner))
        {
            float? remaining = timer.RemainingTime(runner);
            if (remaining.HasValue)
            {
                skillProfile.sprite = skillProfile_Pic_Cooldown;
                skillCooldownText.enabled = true;
                skillCooldownText.text = remaining.Value.ToString("F1") + "s";
                return;
            }
        }

        skillProfile.sprite = skillProfile_Pic;
        skillCooldownText.enabled = false;
    }

    public void SetUsable(bool isUsable)
    {
        Color targetColor = isUsable ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.8f);

        if (skillProfile != null) skillProfile.color = targetColor;
        if (longSkillKey != null) longSkillKey.color = targetColor;
        if (shortSkillKey != null) shortSkillKey.color = targetColor;
    }

    public void UnlockSkill()
    {
        if (isSkillDiscover) return;

        isSkillDiscover = true;
        PlayDoTween();
    }

    public void UnlockSkillImmediate()
    {
        if (isSkillDiscover) return;
        isSkillDiscover = true;
        transform.localScale = Vector3.one;
    }

    public void PlayDoTween()
    {
        transform.DOKill();

        transform.DOScale(Vector3.one, 1f)
                 .From(Vector3.zero)
                 .SetDelay(0.5f)
                 .SetEase(Ease.InOutElastic);
    }
}