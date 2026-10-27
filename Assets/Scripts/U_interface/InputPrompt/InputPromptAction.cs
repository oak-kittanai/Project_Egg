// การกระทำที่มีป้ายปุ่มบน UI (IP-1) — ใช้เลือกรูป/ข้อความจาก InputGlyphSet
// เพิ่มของใหม่ต่อท้ายเท่านั้น: prefab/asset เก็บค่าเป็นตัวเลข แทรกกลางแล้วของเดิมจะเลื่อนผิดตัว
public enum InputPromptAction
{
    Move,
    Jump,
    Fly,
    Interact,
    Dive,
    Throw,
    Drop,
    Pause,
    CloseUI,
    Confirm,
    Back,
    Delete,
    CloseHint,  // ป้าย "กด … เพื่อปิด" ในหน้าโน้ต/หน้าสอนเล่น (รูปทั้งป้าย ไม่ใช่ปุ่มเดี่ยว) (IP-5)
}
