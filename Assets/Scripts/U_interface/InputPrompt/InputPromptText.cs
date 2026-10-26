using System;
using System.Text;
using TMPro;
using UnityEngine;

// ข้อความที่มีชื่อปุ่ม เปลี่ยนตามอุปกรณ์ (IP-1)
// เขียน template เช่น "{Confirm}: press   {Back}: close" — {ชื่อใน InputPromptAction} ถูกแทนด้วยชื่อปุ่มของอุปกรณ์ปัจจุบัน
// (ช่อง label ใน InputGlyphSet) — template ว่าง = ใช้ข้อความที่พิมพ์ไว้ใน TMP ตอนเริ่ม
[RequireComponent(typeof(TMP_Text))]
public class InputPromptText : MonoBehaviour
{
    [TextArea]
    [SerializeField] private string template = "";
    [Tooltip("ซ่อนข้อความตอนใช้คีย์บอร์ด/เมาส์ (เช่นวิธีกดที่ใช้ได้เฉพาะจอย)")]
    [SerializeField] private bool hideWhenKeyboard;

    private TMP_Text text;
    private readonly StringBuilder builder = new StringBuilder();

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
        if (string.IsNullOrEmpty(template)) template = text.text;
    }

    private void OnEnable()
    {
        InputDeviceWatcher.Changed += OnSchemeChanged;
        Refresh();
    }

    private void OnDisable()
    {
        InputDeviceWatcher.Changed -= OnSchemeChanged;
    }

    private void OnSchemeChanged(InputScheme scheme) => Refresh();

    public void SetTemplate(string value)
    {
        template = value;
        Refresh();
    }

    public void Refresh()
    {
        if (text == null) return;

        InputScheme scheme = InputDeviceWatcher.Current;
        bool show = !(hideWhenKeyboard && scheme == InputScheme.KeyboardMouse);
        text.enabled = show;
        if (show) text.text = Format(scheme);
    }

    private string Format(InputScheme scheme)
    {
        InputGlyphSet set = InputGlyphSet.Instance;
        builder.Clear();
        int i = 0;
        while (i < template.Length)
        {
            int open = template.IndexOf('{', i);
            int close = open >= 0 ? template.IndexOf('}', open + 1) : -1;
            if (open < 0 || close < 0)
            {
                builder.Append(template, i, template.Length - i);
                break;
            }

            builder.Append(template, i, open - i);
            string name = template.Substring(open + 1, close - open - 1);
            if (Enum.TryParse(name, out InputPromptAction action))
                builder.Append(set != null ? set.Label(action, scheme) : name);
            else
                builder.Append(template, open, close - open + 1);   // ไม่ใช่ชื่อ action — คงไว้ตามเดิม
            i = close + 1;
        }
        return builder.ToString();
    }
}
