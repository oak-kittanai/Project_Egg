# รูปปุ่มที่เปลี่ยนตามอุปกรณ์ (Input Glyphs) — คู่มือทีม UI

เกมเปลี่ยนรูปปุ่มบนจอให้ตรงกับอุปกรณ์ที่ผู้เล่นกด **ล่าสุด** ทันที:
- คีย์บอร์ด/เมาส์
- จอย Xbox (รวมจอยอื่นที่ไม่รู้จัก)
- จอย PlayStation (PS4/PS5)

รูปในโฟลเดอร์นี้ตอนนี้เป็น **placeholder** — วาดของจริงทับได้เลย

## เปลี่ยน art: วาดทับไฟล์เดิม ชื่อเดิม
- เปิด PNG ในโฟลเดอร์นี้ วาดใหม่แล้ว **save ทับชื่อเดิม** → Unity ใช้รูปใหม่ทุกที่ทันที (ไม่ต้องลากต่อใหม่)
- อย่าเปลี่ยนชื่อ/ย้ายไฟล์ — ถ้าจำเป็น ให้ย้ายใน Unity (ไฟล์ `.meta` ต้องไปด้วย)

| ชนิด | ขนาด canvas | หมายเหตุ |
|---|---|---|
| ปุ่มเดี่ยว | 300 × 300 px | ช่องในเกมเป็นสี่เหลี่ยมจัตุรัส |
| ปุ่ม ×2 (`*_x2`) | 1133 × 300 px | สัดส่วนเดียวกับรูป spacebar ×2 เดิม (ช่องในเกมยืดตามสัดส่วนนี้) |

- ทุกปุ่มมีคู่ `_pressed` = รูปตอนกดค้าง (เช่นตอนนกบิน/ตอนเล็งปาหิน)
- Import ตั้งไว้แล้ว: Sprite (Single), PPU 300, ไม่มี mipmap, Max Size 256 (ปุ่มเดี่ยว) / 1024 (×2)

## รายการไฟล์
| การกระทำ | คีย์บอร์ด | Xbox | PlayStation |
|---|---|---|---|
| เดิน (Move) | `Keyboard/key_wasd` | `Xbox/xbox_ls` | `PlayStation/ps_ls` |
| กระโดด (Jump) | `Keyboard/key_space` | `xbox_a` | `ps_cross` |
| บิน — นก (Fly) | รูปเดิม `SkillMenu/Bird/spacebar x 2` | `xbox_a_x2` | `ps_cross_x2` |
| ใช้ของ/ทุบ (Interact) | รูปเดิม `SkillMenu/Duck/E` | `xbox_x` | `ps_square` |
| ดำน้ำ — เป็ด (Dive) | รูปเดิม `SkillMenu/Duck/F` | `xbox_rb` | `ps_r1` |
| ปาหิน — นก (Throw) | รูปเดิม `SkillMenu/Bird/q` | `xbox_rb` | `ps_r1` |
| ทิ้งของ (Drop) | `Keyboard/key_g` | `xbox_lb` | `ps_l1` |
| Pause | `Keyboard/key_esc` | `xbox_start` | `ps_options` |
| ปิด UI (CloseUI) | `Keyboard/key_tab` | `xbox_b` | `ps_circle` |
| ยืนยัน (Confirm) | `Keyboard/key_enter` | `xbox_a` | `ps_cross` |
| ย้อน (Back) | `Keyboard/key_esc` | `xbox_b` | `ps_circle` |
| ลบ (Delete) | `Keyboard/key_backspace` | `xbox_x` | `ps_square` |

- ตารางนี้ตรงกับปุ่มที่เกมรับจริง ถ้าโปรแกรมเมอร์เปลี่ยนปุ่ม ต้องแก้ทั้งสองที่
- ไฟล์ที่ผูกทั้งหมดอยู่ที่ `Assets/Resources/InputGlyphSet.asset` (เปิดใน Inspector ได้)
  - ช่อง `label` คือคำที่ใช้ในข้อความ เช่น "X", "Square"
  - ใช้ตัวอังกฤษเท่านั้น — ฟอนต์ในเกมไม่มี ✕○□△
  - ช่อง PlayStation ว่าง = ใช้รูป Xbox

## ใส่ปุ่มใน UI ใหม่
- **รูปบน UI (Image):** ลาก `Template/InputPrompt_UI.prefab` มาใช้ หรือใส่ component `InputGlyph` บน Image แล้วเลือก `Action`
  - `Keep Authored Keyboard Sprite` เปิด = ตอนใช้คีย์บอร์ดโชว์รูปที่ใส่ไว้ใน Image (รักษาสไตล์ art เดิม)
  - ปิด = ใช้รูปช่องคีย์บอร์ดใน `InputGlyphSet`
- **รูปในฉาก (SpriteRenderer):** `Template/InputPrompt_World.prefab` หรือใส่ `InputGlyph` บน SpriteRenderer — ขนาดบนจอคงเท่ารูปเดิมเอง
- **ข้อความ (TextMeshPro):** ใส่ component `InputPromptText` แล้วเขียน Template ตามตัวอย่างนี้
  - ใส่ชื่อการกระทำในวงเล็บปีกกา เช่น `Press {Interact} to read`
  - ผลบนจอ: คีย์บอร์ด = "Press E to read", Xbox = "Press X to read", PS = "Press Square to read"
  - `Hide When Keyboard` = ซ่อนข้อความตอนใช้คีย์บอร์ด

## รูปหน้าสอนเล่นเวอร์ชันจอย
รูปสอนเล่นเดิม (`Resources/Assets/Ingame/Tutorial/`) วาดปุ่มคีย์บอร์ดไว้ในรูป วิธีใส่เวอร์ชันจอย:
1. ทำรูปใหม่ **ขนาดเท่ารูปเดิม** (กรอบในเกม 1293 × 726)
2. เปิดฉากด่าน → object ที่มี `LevelData` → รายการ tutorial → ใส่รูปใหม่ในช่อง `Tutorial Sprite Controller`

- ช่องว่าง = ใช้รูปเดิมทุกอุปกรณ์
- ระหว่างเปิดหน้าสอนเล่น ถ้าผู้เล่นสลับอุปกรณ์ รูปจะเปลี่ยนตามทันที

## จุดที่ต่อไว้แล้ว
- ป้ายกด E เหนือของ — `Canvas.prefab` → `InteractPrompt/Square`
- ไอคอนสกิล 4 ตัว (`Resources/Prefabs/Skill_UI/`) — ช่อง `Key Action` ใน `SkillGUI`
  - ใช้คีย์บอร์ด: รูปเดิมของ prefab
  - ใช้จอย: รูปจาก `InputGlyphSet`
- ข้อความวิธีกดใน popup ใส่เลขห้อง (`SessionScene`)
- หน้าสอนเล่น (รอรูปเวอร์ชันจอย)
- กรอบเลือกปุ่มสีขาวในเมนู: ขึ้นเฉพาะตอนกำลังใช้จอย

## หมายเหตุ
- ถ้าเล่นจอย PS ผ่าน Steam Input หรือ DS4Windows ระบบจะเห็นเป็นจอย Xbox → โชว์ปุ่ม Xbox
- เพิ่มการกระทำใหม่ (โปรแกรมเมอร์): เพิ่มใน `InputPromptAction.cs` **ต่อท้ายเท่านั้น** แล้วเพิ่มแถวใน `InputGlyphSet`
