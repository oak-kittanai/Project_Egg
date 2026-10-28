# รูปปุ่มที่เปลี่ยนตามอุปกรณ์ (Input Glyphs) — คู่มือทีม UI

เกมเปลี่ยนรูปปุ่มบนจอให้ตรงกับอุปกรณ์ที่ผู้เล่นกด **ล่าสุด** ทันที:
- คีย์บอร์ด/เมาส์
- จอย Xbox (รวมจอยอื่นที่ไม่รู้จัก)
- จอย PlayStation (PS4/PS5)

## ไฟล์จริงอยู่ที่ไหน (2026-10-28)
| ชุด | โฟลเดอร์ |
|---|---|
| Xbox | `Assets/Resources/UI_Assets/xbox/` |
| PlayStation | `Assets/Resources/UI_Assets/playstation/` |
| คีย์บอร์ด | รูปเดิมของเกม (`Assets/Resources/Assets/Ingame/SkillMenu/...`) + placeholder ใน `Keyboard/` ของโฟลเดอร์นี้ |
| รูปสอนเล่น (How to play) | `Assets/Resources/UI_Assets/How to play/` |
| ปุ่มตัวเลขใส่เลขห้อง | `Assets/Resources/UI_Assets/number room join/` |

- แก้ art ให้วาดทับไฟล์ชื่อเดิมในโฟลเดอร์เหล่านี้ → ขึ้นในเกมทุกจุดเอง
- โฟลเดอร์นี้ (`Art-Source/UI/InputGlyphs`) เหลือแค่ placeholder ที่ยังไม่มีรูปจริง: ○ ของ PS (`PlayStation/ps_circle`) และปุ่มคีย์บอร์ดบางปุ่ม — ไฟล์ Xbox/PS อื่นในนี้ไม่ได้ใช้แล้ว
- คำในชื่อไฟล์: `pass` = ตอนกด, `hold` = ตอนชี้/จอยเลือก, `dis` = กดไม่ได้

## ขนาด
| ชนิด | ขนาด canvas | หมายเหตุ |
|---|---|---|
| ปุ่มเดี่ยว | 300 × 300 px | ขอบรอบปุ่ม ~44 px แบบปุ่ม E/F/Q เดิม |
| ปุ่มยาว (R1, LB, OPTION) | สูง 300 กว้างตามปุ่ม | เกมให้ช่องสูงเท่าเดิมแล้วกว้างตามรูปเอง |
| ปุ่ม ×2 | สูง 300 กว้างตามรูป | ใช้ทั้งภาพ เหมือน spacebar ×2 |
| ป้าย TO CLOSE | สูง 118 กว้างตามรูป | ใช้ทั้งภาพ |

- รูปตอนกด/ตอนชี้ ใช้ canvas ขนาดเดียวกับรูปปกติ และวางปุ่มตำแหน่งเดียวกัน
- Import ตั้งไว้แล้ว: ปุ่มเดี่ยวตัดขอบเป็น sprite เดียว (รูปตอนกดใช้กรอบเดียวกัน), PPU 300

## รายการปุ่ม
| การกระทำ | คีย์บอร์ด | Xbox (`xbox/`) | PlayStation (`playstation/`) |
|---|---|---|---|
| เดิน (Move) | `Keyboard/key_wasd` | `L Stick xbox` (+ pass) | `L Stick` (+ pass) |
| กระโดด (Jump) / ยืนยัน (Confirm) | `key_space` / `key_enter` | `join Xbox jump` | `join jump` |
| บิน — นก (Fly) | รูปเดิม `spacebar x 2` | `join Xbox Fly` (+ Pass) | `join Fly` (+ pass) |
| ใช้ของ/ทุบ (Interact) / ลบ (Delete) | รูปเดิม `E` / `key_backspace` | `join Xbox interact` (+ pass) | `jpin interact` (+ pass) |
| ดำน้ำ — เป็ด (Dive) / ปาหิน — นก (Throw) | รูปเดิม `F` / `q` | `RB` (+ pass) | `R1` (+ pass) |
| ทิ้งของ (Drop) | `key_g` | `LB` (+ pass) | `L1` (+ `l1 pass`) |
| Pause | `key_esc` | `join Xbox option` | `option` |
| ปิด UI (CloseUI) / ย้อน (Back) | `key_tab` / `key_esc` | `join xbox close` | **ยังไม่มี** — ใช้ placeholder `PlayStation/ps_circle` |
| ป้าย "กด … เพื่อปิด" (CloseHint) | รูปเดิม `Note_Interface/Tab close` | `howwto xbox close_` | `join close` |
| แถบบอกปุ่ม: ไม่มีปุ่มย้อน (HintConfirm) | — (แถบขึ้นเฉพาะตอนใช้จอย) | `confirm xbox` | `confirm playstation` |
| แถบบอกปุ่ม: ย้อนกลับ (HintConfirmBack) | — | `confirm back xbox` | `confirm back playstation` |
| แถบบอกปุ่ม: กดค้างออกจากห้อง (HintConfirmLeave) | — | `confirm Leave xbox` | `confirm Leave playstation` |

- ยังไม่มีการกระทำไหนใช้: Y (`join xbox other`), △ (`other`)
- ยังไม่มีรูปตอนกด: A, Menu, B ของ Xbox; ✕, OPTION ของ PS (ไม่มี = ใช้รูปปกติ)
- ตารางนี้ตรงกับปุ่มที่เกมรับจริง ถ้าโปรแกรมเมอร์เปลี่ยนปุ่ม ต้องแก้ทั้งสองที่
- ไฟล์ที่ผูกทั้งหมดอยู่ที่ `Assets/Resources/InputGlyphSet.asset` (เปิดใน Inspector ได้)
  - ช่อง `label` คือคำที่ใช้ในข้อความ เช่น "X", "Square" — ใช้ตัวอังกฤษเท่านั้น (ฟอนต์ในเกมไม่มี ✕○□△)
  - ช่อง PlayStation ว่าง = ใช้รูป Xbox

## ใส่ปุ่มใน UI ใหม่
- **รูปบน UI (Image / RawImage):** ลาก `Template/InputPrompt_UI.prefab` มาใช้ หรือใส่ component `InputGlyph` บน Image/RawImage แล้วเลือก `Action`
  - `Keep Authored Keyboard Sprite` เปิด = ตอนใช้คีย์บอร์ดโชว์รูปที่ใส่ไว้ใน Image (รักษาสไตล์ art เดิม)
  - ปิด = ใช้รูปช่องคีย์บอร์ดใน `InputGlyphSet`
- **รูปในฉาก (SpriteRenderer):** `Template/InputPrompt_World.prefab` หรือใส่ `InputGlyph` บน SpriteRenderer — ขนาดบนจอคงเท่ารูปเดิมเอง
- **ข้อความ (TextMeshPro):** ใส่ component `InputPromptText` แล้วเขียน Template ตามตัวอย่างนี้
  - ใส่ชื่อการกระทำในวงเล็บปีกกา เช่น `Press {Interact} to read`
  - ผลบนจอ: คีย์บอร์ด = "Press E to read", Xbox = "Press X to read", PS = "Press Square to read"
  - `Hide When Keyboard` = ซ่อนข้อความตอนใช้คีย์บอร์ด
- **ปุ่มเมนู:** ใส่รูป hover ในช่อง Highlighted ของ Button (SpriteSwap) → จอยเลือกแล้วใช้รูปนี้แทนกรอบขาวเอง

## รูปหน้าสอนเล่น
- รูปปัจจุบัน (`UI_Assets/How to play/`) มีปุ่มคีย์บอร์ด + PS + Xbox ในรูปเดียว ใช้กับทุกอุปกรณ์
- รูป 1920 × 1080 เนื้อรูปอยู่ในกรอบเดียวกับรูปเดิม (ในเกมโชว์ในกรอบ 1293 × 726)
- ถ้าอยากแยกรูปตามอุปกรณ์: ฉากด่าน → object ที่มี `LevelData` → รายการ tutorial → ช่อง `Tutorial Sprite Controller` (ว่าง = ใช้รูปหลักทุกอุปกรณ์)

## จุดที่ต่อไว้แล้ว
- ป้ายกดเหนือของ — `Canvas.prefab` → `InteractPrompt/Square`
- ไอคอนสกิล 4 ตัว (`Resources/Prefabs/Skill_UI/`) — ช่อง `Key Action` ใน `SkillGUI`
- ป้าย "PRESS Tab TO CLOSE" ในหน้าโน้ตและหน้าสอนเล่น — `Canvas.prefab` → `NoteObj/PressTabToClose`, `TutorialPanel/PressTabToClose`
- ปุ่มลูกศร back (`UI_Assets/back*`): ปุ่มกลับหน้า Join, Prev/Next บทสนทนาและหน้าสอนเล่น; ปุ่ม ? (`UI_Assets/how to play*`) ในเมนู pause
- popup ใส่เลขห้องของจอย (`SessionScene` → `RoomCodeKeypad`): รูปจาก `number room join/`; ข้อความวิธีกดอยู่ใต้กรอบ
- รูป hover: ลูกศรเปลี่ยนตัวละคร (`change proflie L/R`), ช่องใส่เลขห้อง (`number room join/code  number hold`)
- แถบบอกปุ่มมุมขวาล่างของเมนู (ตอนใช้จอย) — `Resources/Prefabs/Interface/ControllerHintBar.prefab`
  - เป็นรูปป้ายเดียวทั้งแถบ (`Bar/Banner`) สูง 72 กว้างตามสัดส่วนรูป — ป้ายใหม่ต้องสูงเท่ากัน (118 px) และวางปุ่มตำแหน่งเดิม
  - เลือกป้ายเอง: หน้าไม่มีปุ่มย้อน = `confirm`, มีปุ่มย้อน (component `UIBackButton`) = `confirm back`, ปุ่มย้อนแบบกดค้าง (Hold Seconds > 0) = `confirm Leave`
  - วงสีส้มรอบปุ่ม B ตอนกดค้าง (`Banner/HoldRing`) วางตามตำแหน่งปุ่ม B ในรูป `confirm Leave` (457, 55 จากรูป 906 × 118)
- กรอบเลือกปุ่มสีขาว: ขึ้นเฉพาะตอนใช้จอย กับหลอดเสียงและปุ่มที่ยังไม่มีรูป hover

## หมายเหตุ
- ถ้าเล่นจอย PS ผ่าน Steam Input หรือ DS4Windows ระบบจะเห็นเป็นจอย Xbox → โชว์ปุ่ม Xbox
- เพิ่มการกระทำใหม่ (โปรแกรมเมอร์): เพิ่มใน `InputPromptAction.cs` **ต่อท้ายเท่านั้น** แล้วเพิ่มแถวใน `InputGlyphSet`
