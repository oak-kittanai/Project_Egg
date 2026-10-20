# กฎการทำงานโปรเจกต์ Easter Bond (ใช้ทุก task จนกว่าจะสั่งเปลี่ยน)
- อ้างอิง GDD_AUDIT.md เป็นแหล่งความจริง โดยเฉพาะตาราง "การตัดสินใจจากเจ้าของโปรเจกต์" — ห้ามแก้พฤติกรรมเกมให้กลับไปตรง GDD เดิมในข้อที่ตัดสินใจแล้ว
- ทำงานบน branch แยกจาก dev ชื่อ fix/<ชื่องาน> (ระหว่างที่งานล่าสุดยังไม่รวมเข้า dev ให้แตกจาก feature/owner-movement) ห้าม commit ตรงเข้า main / dev — ลำดับรวมงาน: fix / feature / test → dev (เช็ค) → main (build)
- ก่อนแก้: บอกแผนสั้น ๆ (ไฟล์ + method ที่จะแตะ + เหตุผล) แล้วรอผมตอบ OK
- แก้ได้เต็มที่เท่าที่ควรแก้ เน้นให้ตรงเป้าหมายของงานมากที่สุด — ปรับโครงสร้าง/refactor ได้ถ้าช่วยให้ถึงเป้าหมาย แต่ไม่แตะส่วนที่ไม่เกี่ยวกับเป้าหมาย
- ถ้าเปลี่ยนชื่อ field ที่ serialize อยู่ใน prefab/scene ต้องใส่ `[FormerlySerializedAs("ชื่อเดิม")]` เพื่อไม่ให้ค่าใน Inspector หาย
- ห้ามแก้ .unity / .prefab / .controller ด้วยการ edit YAML ตรง ๆ — ใช้ Unity MCP ถ้าต่อได้ ถ้าไม่ได้ให้เขียนขั้นตอนให้ผมทำใน Editor
- หลักการ Fusion 2: logic เกมทำบน State Authority; ผลที่ client ทำนายเองต้องไม่ส่ง RPC ซ้ำกับที่ host ทำ; เสียง/GUI ครั้งเดียวต้อง gate ด้วย HasInputAuthority && Runner.IsForward; ห้ามเข้าถึง [Networked] ก่อน Spawned()
- หลังแก้: compile ต้อง 0 error, เขียนวิธีเทส 2 ผู้เล่นด้วย Multiplayer Play Mode (Host + Client) เป็น checklist, อัปเดตสถานะใน GDD_AUDIT.md (เพิ่มคอลัมน์/บรรทัด "แก้แล้ว: <branch> <commit>")
- commit message ภาษาอังกฤษ ขึ้นต้นด้วยรหัสในรายงาน เช่น "fix(C-1): ..."