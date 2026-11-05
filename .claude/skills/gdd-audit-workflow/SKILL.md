---
name: gdd-audit-workflow
description: How to work with GDD_AUDIT.md (378 KB source of truth for Easter Bond / Project_Egg) and the task workflow around it — reading the owner-decision table, finding items by code (C-1, H-4, NR-12, SO-9 …), planning, branching from test/all2, commit message format, and recording "แก้แล้ว: <branch> <commit>". Use at the start and end of every task, and when asked about GDD, bug codes, status, branches or commits.
---

# GDD_AUDIT workflow

## 1. แหล่งความจริง
| ไฟล์ | คืออะไร |
|---|---|
| `GDD_AUDIT.md` | audit + บันทึกงานทุกรอบ ไม่มี GDD ต้นฉบับใน repo **เกมเป็นหลัก GDD ตามเกม** |
| `GDD_Solo.md` | GDD โหมด Solo (`Level1_solo`) ผังด่าน 8 โซนตามแกน x, checklist ข้อ 15 |
| `CLAUDE.md` | กฎการทำงาน (ชนะทุกอย่างในนี้ถ้าขัดกัน) |

ไฟล์ใหญ่ — **ห้ามอ่านทั้งไฟล์** ใช้ `grep -n '^## \|^### ' GDD_AUDIT.md` ดูสารบัญ แล้วอ่านเฉพาะช่วงด้วย offset/limit

## 2. อ่านก่อนเริ่มทุกงาน
1. ตาราง **"การตัดสินใจจากเจ้าของโปรเจกต์"** (บรรทัดต้น ๆ) — ข้อในตารางนี้ "ปิดแล้ว" **ห้ามแก้พฤติกรรมเกมกลับไปตาม GDD เดิม** และไม่นับเป็นบั๊ก เช่น ปุ่มดำน้ำ F, ตายแล้วเกิดใหม่อัตโนมัติ, จบ S3 กลับ lobby, ตัดวิ่ง/Fall damage/บอส, หน้าสอนเล่นไม่หยุดโลก (TU-1), กติกา Solo
2. หาหัวข้อของรหัสที่เกี่ยว: `grep -n 'SO-9\|C-1' GDD_AUDIT.md`
3. หัวข้อ 4 (Bug / ความเสี่ยง) และ 5 (งานที่ต้องทำ) สำหรับลำดับความสำคัญ

รหัสที่ใช้: C/H/M/L = บั๊กตามความรุนแรง, D = ความต่าง GDD, NJ/NR = network, MV = การเคลื่อนที่, SO = Solo, TU = tutorial, IP/UI/HL/BK = UI/input, AU/SFX = เสียง, PF = performance, ตั้งรหัสใหม่ต่อจากเลขล่าสุดของกลุ่มนั้น

## 3. ลำดับการทำงาน (CLAUDE.md)
1. **เสนอแผนสั้น ๆ ก่อนแก้:** ไฟล์ + method ที่จะแตะ + เหตุผล แล้ว **รอ OK** (คำถามที่ผู้ใช้ตอบแบบ "เลือกแล้วเริ่มแก้ทันที" ถือเป็น OK)
2. **Branch:** `fix/<ชื่องาน>` (หรือ `feature/…`) แตกจาก `test/all2` ระหว่างที่งานยังไม่รวมเข้า dev แล้วรวมกลับเข้า `test/all2` — **ห้าม commit ตรงเข้า `main` / `dev`** ลำดับ: fix/feature/test → dev (เช็ค) → main (build)
3. ก่อนเริ่มเช็ค `git status` — working tree ของเจ้าของมักมีงานค้าง (ฉาก, prefab, ProjectSettings, font asset) **ห้าม commit ให้** ใช้ `git add <path>` เฉพาะไฟล์ของงาน
4. แก้ได้เต็มที่เท่าที่ตรงเป้าหมาย refactor ได้ แต่ไม่แตะส่วนที่ไม่เกี่ยว; เปลี่ยนชื่อ serialized field ต้องใส่ `[FormerlySerializedAs("ชื่อเดิม")]`
5. `.unity` / `.prefab` / `.controller` ห้ามแก้ YAML ตรง — ดู skill `unity-mcp-editor-ops`
6. compile 0 error → checklist เทส (skill `mppm-2player-test`) → อัปเดต GDD_AUDIT

## 4. Commit message
ภาษาอังกฤษ ขึ้นต้นด้วยรหัส แยก code กับ docs:
```
fix(SO-7): stop the follower losing its position between ticks
feat(SO-9): rescue the sinking bird with the duck and fly from a jump
docs(SO-9): record the bird rescue and the jump flight rules
chore(SO-4): add Level1_solo to the scene list
```

## 5. รูปแบบหัวข้อใหม่ใน GDD_AUDIT
เพิ่มหัวข้อเลขถัดไปท้ายไฟล์:
```markdown
## N. <เรื่อง> (YYYY-MM-DD)

**ที่มา:** <ใครสั่ง/feedback อะไร>
**Branch:** `fix/<ชื่อ>` (แตกจาก `test/all2`) → รวมเข้า `test/all2`

### N.1 สิ่งที่แก้
| รหัส | เรื่อง | การทำ (ไฟล์ : method) | แก้แล้ว |
|---|---|---|---|
| XX-1 | ... | `File.cs` `Method` ... | แก้แล้ว: `fix/<ชื่อ>` `<commit 8 ตัว>` |

### N.2 ผลเทส / ยังไม่ได้เทส
### N.3 สิ่งที่ค้นพบ / บทเรียน (ถ้ามี)
### N.4 Checklist เทส — `test/all2` (MPPM ...)
```
- ถ้าแก้ข้อที่มีอยู่แล้วในหัวข้อเก่า ให้เพิ่มบรรทัด/คอลัมน์ "แก้แล้ว: <branch> <commit>" ที่ข้อนั้นด้วย
- ทุกข้อที่บอกว่า "เสร็จ" ต้องมีหลักฐาน path + class/method
- ถ้าโค้ดต่างจาก GDD ให้บันทึกเป็น "ความต่าง" ให้เจ้าของตัดสิน ไม่ตัดสินเอง
- ตรวจจากโค้ดอย่างเดียว → "✅ โค้ด (ไม่ได้รัน)"

## 6. สื่อสาร
ตอบภาษาไทย ศัพท์เทคนิคใช้อังกฤษได้ เน้นโค้ด/mechanic มากกว่า art
