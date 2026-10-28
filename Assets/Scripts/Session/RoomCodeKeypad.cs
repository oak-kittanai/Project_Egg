using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// ปุ่มตัวเลขสำหรับใส่เลขห้องด้วย Controller (KP-1)
// Controller กด A ที่ช่องใส่เลข -> UIKeyboardNavigator เรียก TryOpenFor -> เปิด popup นี้แทนการเข้าโหมดพิมพ์
// เลขห้องเป็นตัวเลข 6 หลัก (SessionManager สร้างจาก 0-9) — คีย์บอร์ด/เมาส์ยังพิมพ์ในช่องได้ตามเดิม
// วางไว้บน object ที่ active ตลอด (ตัว popup คือ panel ที่เปิด/ปิด)
public class RoomCodeKeypad : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private TMP_InputField targetField;
    [SerializeField] private Button joinButton;

    [Header("Popup")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text display;
    [Tooltip("ปุ่มเลข 0-9 ตามลำดับ index")]
    [SerializeField] private Button[] digitButtons = new Button[10];
    [SerializeField] private Button backspaceButton;
    [SerializeField] private Button okButton;
    [SerializeField] private int maxLength = 6;
    [Tooltip("ข้อความในช่องตอนยังไม่มีเลข (ตามแบบทีม UI)")]
    [SerializeField] private string emptyText = "ENTER CODE";
    [SerializeField] private Color emptyTextColor = new Color(0.6f, 0.63f, 0.75f, 1f);

    private static readonly List<RoomCodeKeypad> openable = new List<RoomCodeKeypad>();
    private readonly StringBuilder displayText = new StringBuilder();
    private Color displayColor;

    public bool IsOpen => panel != null && panel.activeSelf;

    public static bool TryOpenFor(TMP_InputField field)
    {
        foreach (var keypad in openable)
        {
            if (keypad.targetField != field) continue;
            keypad.Open();
            return true;
        }
        return false;
    }

    private void Awake()
    {
        if (display != null) displayColor = display.color;

        for (int i = 0; i < digitButtons.Length; i++)
        {
            int digit = i;
            if (digitButtons[i] != null) digitButtons[i].onClick.AddListener(() => AddDigit(digit));
        }
        if (backspaceButton != null) backspaceButton.onClick.AddListener(Backspace);
        if (okButton != null) okButton.onClick.AddListener(Confirm);

        SetupGridNavigation();
        if (panel != null) panel.SetActive(false);

        // เลื่อนมาที่ช่องด้วย Controller/WASD แล้วไม่เข้าโหมดพิมพ์ทันที (เดิมเข้าแล้วติดอยู่ในช่อง เลื่อนออกไม่ได้)
        // เมาส์คลิกยังเข้าโหมดพิมพ์ (OnPointerClick), คีย์บอร์ดกด Enter ที่ช่องเพื่อเริ่มพิมพ์
        if (targetField != null) targetField.shouldActivateOnSelect = false;
    }

    private void OnEnable() => openable.Add(this);
    private void OnDisable() => openable.Remove(this);

    private void Update()
    {
        if (!IsOpen) return;

        // ออกจากหน้า Join ไปแล้ว (เช่นเข้าห้องสำเร็จ) — ปิดตาม
        if (targetField == null || !targetField.gameObject.activeInHierarchy)
        {
            Close(false);
            return;
        }

        var pad = Gamepad.current;
        if (pad != null)
        {
            // B ปิด popup อย่างเดียว ไม่ให้หน้า Join ย้อนกลับต่อในกดเดียวกัน (BK-1)
            if (pad.buttonEast.wasPressedThisFrame) { UIBackButton.ConsumeBack(); Close(true); return; }
            if (pad.buttonWest.wasPressedThisFrame) Backspace();
            if (pad.startButton.wasPressedThisFrame) { Confirm(); return; }
        }

        // ตัวเลือกต้องอยู่ใน popup ตลอดที่เปิด (เช่นเมาส์คลิกที่อื่น)
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected == null || !selected.transform.IsChildOf(panel.transform)) SelectDefault();
    }

    public void Open()
    {
        if (panel == null || targetField == null) return;
        panel.SetActive(true);
        RefreshDisplay();
        SelectDefault();
    }

    private void Close(bool reselectField)
    {
        if (panel != null) panel.SetActive(false);
        if (reselectField && targetField != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(targetField.gameObject);
    }

    private void AddDigit(int digit)
    {
        if (targetField.text.Length >= maxLength) return;
        targetField.text += digit.ToString();
        RefreshDisplay();
    }

    private void Backspace()
    {
        string text = targetField.text;
        if (text.Length == 0) return;
        targetField.text = text.Substring(0, text.Length - 1);
        RefreshDisplay();
    }

    // OK = ปิดแล้วกด Join ให้ — SessionHub.JoinRoom ตรวจ 6 หลัก/ขึ้นข้อความผิดเหมือนเดิม
    private void Confirm()
    {
        Close(false);
        if (joinButton == null || !joinButton.interactable) return;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(joinButton.gameObject);
        joinButton.onClick.Invoke();
    }

    // ว่าง = "ENTER CODE" สีจาง, มีเลขแล้ว = "4 8 2 _ _ _"
    private void RefreshDisplay()
    {
        if (display == null) return;
        string text = targetField.text;
        if (text.Length == 0)
        {
            display.text = emptyText;
            display.color = emptyTextColor;
            return;
        }

        display.color = displayColor;
        displayText.Clear();
        for (int i = 0; i < maxLength; i++)
        {
            if (i > 0) displayText.Append(' ');
            displayText.Append(i < text.Length ? text[i] : '_');
        }
        display.text = displayText.ToString();
    }

    private void SelectDefault()
    {
        Button first = digitButtons.Length > 1 && digitButtons[1] != null ? digitButtons[1] : okButton;
        if (first != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
    }

    // ตาราง 1 2 3 / 4 5 6 / 7 8 9 / ⌫ 0 OK — เดินวนในตาราง ไม่หลุดไปปุ่ม Join/Leave ด้านหลัง
    private void SetupGridNavigation()
    {
        if (digitButtons.Length < 10) return;
        var grid = new Selectable[,]
        {
            { digitButtons[1], digitButtons[2], digitButtons[3] },
            { digitButtons[4], digitButtons[5], digitButtons[6] },
            { digitButtons[7], digitButtons[8], digitButtons[9] },
            { backspaceButton, digitButtons[0], okButton },
        };
        int rows = grid.GetLength(0), cols = grid.GetLength(1);
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (grid[r, c] == null) continue;
                grid[r, c].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = grid[(r + rows - 1) % rows, c],
                    selectOnDown = grid[(r + 1) % rows, c],
                    selectOnLeft = grid[r, (c + cols - 1) % cols],
                    selectOnRight = grid[r, (c + 1) % cols],
                };
            }
        }
    }
}
