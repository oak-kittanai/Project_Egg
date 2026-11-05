---
name: mppm-2player-test
description: How to test Easter Bond (Project_Egg) with two players using Unity Multiplayer Play Mode (Host + Client), and how to write the test checklist that CLAUDE.md requires after every change. Use after any code change, when asked "วิธีเทส", "checklist เทส", "เทส 2 คน", "MPPM", "Host/Client", or when reading Player 2 logs.
---

# เทส 2 ผู้เล่นด้วย Multiplayer Play Mode

CLAUDE.md บังคับ: หลังแก้ทุกครั้ง compile 0 error + เขียนวิธีเทส Host + Client เป็น checklist + อัปเดต GDD_AUDIT (ดู skill `gdd-audit-workflow`)

## 1. การตั้งค่าที่มีอยู่แล้ว
- Package `com.unity.multiplayer.playmode` 1.6.3
- Scenario: `Assets/Settings/PlayMode/DuoGamePlay_Test.asset` (Main Editor + Player 2, log ของ Player 2 เข้า Console หลัก)
- เปิด: dropdown ข้างปุ่ม Play → **DuoGamePlay_Test** → Play (ถ้าไม่เห็น: Window > Multiplayer > Play Mode Scenarios)
- Stop/Play ทำ domain reload → ค่า static reset ทั้งหมด ถ้าข้อเทสต้องต่อเนื่อง ให้บอกผู้เทสว่า **ห้ามกด Stop**
- Log Player 2: `Library/VP/mppm293b93cf/Logs/Editor.log`; Host: `%LOCALAPPDATA%\Unity\Editor\Editor.log`

## 2. "เริ่มเกม" มาตรฐาน (ใช้อ้างในทุก checklist)
1. **Main Editor (Host):** Play → DUO → Create Room → เลือก **Mira (นก)** → Ready
2. **Player 2 (Client):** Join Room → ใส่เลขห้อง → เลือก **Kael (เป็ด)** → Ready
3. Host กด Start → ทั้งคู่ Skip cutscene → ปิด dialogue ที่จุดเกิด

Client เป็น Kael เพราะการยกเพื่อนทำได้เฉพาะเป็ด และบั๊กส่วนใหญ่โผล่เมื่อ client เป็นคนกด ถ้างานเกี่ยวกับนกฝั่ง client ให้เพิ่มรอบสลับ (Host = Kael, Client = Mira)

Solo ไม่ใช้ MPPM: เมนูหลัก → Start → SOLO (PROTOTYPE) → ใช้ checklist ใน `GDD_Solo.md` ข้อ 15 แต่ถ้าแก้โค้ดที่ Solo กับ Duo ใช้ร่วมกัน ต้องมีข้อเทส Duo ด้วยเสมอ

## 3. รูปแบบ checklist ที่ต้องเขียน
ตารางเดียว เรียงตามลำดับเล่นจริง (ผู้เทสมีเวลาน้อย ให้เล่นรอบเดียวจบ ~10–15 นาที):

```markdown
### N.x Checklist เทส — branch `<branch>` (MPPM `DuoGamePlay_Test`, Host = Mira, Client = Kael) ~10 นาที
| ขั้น | ใคร | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|---|
| 1 | ทั้งคู่ | เริ่มเกม (ขั้นตอนมาตรฐาน) | เข้า S1 ทั้งคู่ | |
| 2 | Client | ... | ทั้ง **จอ Host และจอ Client** เห็น ... | |
```

กฎของ checklist:
- ทุกข้อระบุ **ใครกด** (Host / Client) และ **ดูที่จอไหน** — บั๊ก network มักเห็นแค่จอเดียว
- มีข้อที่ **client เป็นคนกด** เสมอสำหรับ interaction ใหม่ (เช็ค RPC ซ้ำ)
- มีข้อเทียบ (control) ที่ไม่ควรเกิดผล วางตำแหน่งเดียวกัน ไม่งั้นผ่านหลอกได้
- ถ้ามี log ให้ดู ใส่ข้อความที่ค้นใน Console ได้ เช่น `[VERIFY] …` (log ทดสอบใช้ `VerifyLog.Log` ใน `Assets/Scripts/_Verify/` — **ห้ามให้เข้า main**)
- เงื่อนไข lag: Fusion Network Stats / `NetworkStatsHotkey` ถ้างานเกี่ยวกับการสั่น/ดีด
- ปิดท้ายด้วย "ยังไม่ได้เทส:" บอกสิ่งที่ probe/อัตโนมัติไม่ครอบคลุม

## 4. เทสอัตโนมัติก่อนส่งให้คนเทส (ถ้า Unity MCP ต่อได้)
ใช้ skill `unity-mcp-editor-ops` เล่น Host คนเดียวผ่าน probe — ได้แค่ฝั่ง Host ยังต้องมี checklist 2 คนเสมอ บันทึกผลเป็น "probe n/n" และสิ่งที่ probe ไม่ได้เทส
ถ้าตรวจได้จากโค้ดอย่างเดียว บันทึกเป็น "✅ โค้ด (ไม่ได้รัน)" — เจ้าของโปรเจกต์มีเวลาเทสน้อย

## 5. ผลข้างเคียงหลังเล่นใน Editor ที่ต้องคืนด้วย git (ห้าม commit)
- `LoadingScreenManager.prefab` ถูกเขียน `m_IsActive: 0`
- `LiberationSans SDF - Fallback.asset` / font asset ได้ glyph เพิ่ม
- ฉากที่เปิดค้างแบบ dirty (SessionScene, Stage1-S1) — ห้ามเซฟแทนเจ้าของโปรเจกต์
