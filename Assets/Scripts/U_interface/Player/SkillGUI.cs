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

    [Header("Input Prompt (IP-2)")]
    [Tooltip("ปุ่มของสกิลนี้ — ตอนใช้จอยรูปปุ่มมาจาก InputGlyphSet; คีย์บอร์ดใช้รูปด้านบนเหมือนเดิม")]
    [SerializeField] InputPromptAction keyAction;

    // คู่รูปปุ่มของอุปกรณ์ปัจจุบัน — เลือกใหม่เฉพาะตอนอุปกรณ์เปลี่ยน; SetPressed (เรียกทุกเฟรม) แค่สลับคู่นี้
    private Sprite keyNormal;
    private Sprite keyPressed;
    private bool keyIsPressed;

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

        InputDeviceWatcher.Changed += OnInputSchemeChanged;
        RefreshKeySprites();
    }

    private void OnDisable()
    {
        InputDeviceWatcher.Changed -= OnInputSchemeChanged;
    }

    private void OnInputSchemeChanged(InputScheme scheme) => RefreshKeySprites();

    private void RefreshKeySprites()
    {
        keyNormal = isSkillKeyPressShort ? shortSkill : longSkill;
        keyPressed = isSkillKeyPressShort ? shortSkillPressed : longSkillPressed;

        InputGlyphSet set = InputGlyphSet.Instance;
        if (InputDeviceWatcher.UsingGamepad && set != null)
        {
            InputGlyphSet.Glyph glyph = set.Get(keyAction, InputDeviceWatcher.Current);
            if (glyph != null && glyph.sprite != null)
            {
                keyNormal = glyph.sprite;
                keyPressed = glyph.pressed != null ? glyph.pressed : glyph.sprite;
            }
        }
        ApplyKeySprite();
    }

    public void SetPressed(bool isKeyPress)
    {
        keyIsPressed = isKeyPress;
        if (keyNormal == null) RefreshKeySprites();
        else ApplyKeySprite();
    }

    private void ApplyKeySprite()
    {
        Image key = isSkillKeyPressShort ? shortSkillKey : longSkillKey;
        if (key != null) key.sprite = keyIsPressed ? keyPressed : keyNormal;
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