---
name: fusion2-netcode
description: Photon Fusion 2 rules for Easter Bond (Project_Egg) — use before writing or reviewing any NetworkBehaviour, [Networked] property, RPC, input (NetworkInputData / OnInput), FixedUpdateNetwork, Render, or anything that must look the same on Host and Client (Duo) or must work in Solo (GameMode.Single). Triggers: "network", "Fusion", "RPC", "sync", "Host/Client", "ฝั่ง client", "สั่น/ดีด/วาร์ป", "เสียงดังซ้ำ", "ยกเพื่อน", "Solo".
---

# Fusion 2 netcode — Easter Bond

โมเดลของเกมนี้ไม่ใช่ตัวอย่างมาตรฐานของ Fusion อ่านให้ครบก่อนแตะโค้ดที่มี network

## 1. โหมดที่ใช้ (`Assets/Scripts/Session/SessionManager.cs`)
| โหมด | ใช้กับ | หมายเหตุ |
|---|---|---|
| `GameMode.Host` / `GameMode.Client` | Duo (2 คน) | Host = State Authority ของทุก object |
| `GameMode.Single` | Solo (`Level1_solo`) | ไม่มีเครื่องอื่น ไม่ย้อน tick; **เปลี่ยน input authority หลังมอบแล้วไม่ได้** (`RemoveInputAuthority`, `AssignInputAuthority(None)` ไม่มีผล) |

Client ตั้ง `ClientPhysicsSimulation = SimulateForward` ก่อน `StartGame`

## 2. ใครคุมอะไร (NR-12, GDD_AUDIT หัวข้อ 17)
| เครื่อง / ตัว | ทำอะไร |
|---|---|
| เจ้าของตัว (input authority) | จำลองการเคลื่อนที่ตัวเอง (Rigidbody2D dynamic) **เฉพาะ tick ใหม่** ไม่ถูก host แก้/ย้อน; ส่ง `OwnerPosition/OwnerVelocity/OwnerFlags/OwnerAckSeq` ใน input ทุก tick |
| Host กับตัวของ client | kinematic วางตามตำแหน่งที่ client ส่งมา (`useFullKinematicContacts` ให้ trigger ยังทำงาน) |
| Client กับตัวของ host | kinematic, interpolate จาก `RepPosition` |
| Host (ทุกตัว) | ตัดสินโลก: E/ไอเท็ม, ดาเมจ, มึน, ตาย/เกิดใหม่, ยก/ปล่อย (`HostSetCarried`/`HostReleaseCarried`), ปาหิน, oxygen |

- ผลจาก host ที่ต้องกระทบตัวเจ้าของ (กระเด็น, วาร์ป, เกิดใหม่, ถูกปล่อย/โยน) ส่งเป็น **event** (`EventSeq` + ตำแหน่ง/แรง) แล้วเจ้าของตอบรับผ่าน `OwnerAckSeq` — ห้ามเขียนตำแหน่งตัวของ client ตรง ๆ จาก host
- สถานะการเคลื่อนที่ (`IsGrounded`, น้ำ, บิน, ทิศหัน) เป็นของเจ้าของ เครื่องอื่นอ่าน `RepFlags`
- `MovementCharacter.IsOwner => IsSpawnedObject && HasInputAuthority`

## 3. กฎที่ต้องทำตามทุกครั้ง (จาก CLAUDE.md)
1. **Logic เกมทำบน State Authority** (`HasStateAuthority` / `Object.HasStateAuthority`) — client ขอผ่าน RPC `[Rpc(RpcSources.All, RpcTargets.StateAuthority)]`
2. **ผลที่ client ทำนายเองแล้ว ห้ามส่ง RPC ซ้ำกับที่ host ทำ** (บั๊ก H-4: Interact/Splash ยิงสองครั้ง) — ถ้า host จะทำอยู่แล้วผ่าน input ก็ไม่ต้อง RPC
3. **เสียง / GUI / effect ครั้งเดียว** ต้อง gate ด้วย `HasInputAuthority && Runner.IsForward` (ไม่งั้นดังซ้ำตอน resimulation) หรือย้ายไปอยู่ใน `Render()` / `OnChangedRender`
4. **ห้ามอ่าน/เขียน `[Networked]` ก่อน `Spawned()`** — จาก Render หรือ static helper ให้เช็ค `Object != null && Object.IsValid` ก่อน (ดู `Utility/GameplayPause.Frozen`)
5. Game logic อยู่ใน `FixedUpdateNetwork()` ไม่ใช่ `Update()`; เวลาใช้ `TickTimer` ไม่ใช่ `Time.deltaTime` สะสม
6. ปุ่มกดใช้ `NetworkButtons` เทียบกับ `[Networked] PreviousButtons` (`WasPressed`) — ไม่เพิ่ม bool แยกต่อปุ่ม
7. ภาพ (ท่าทาง, ซ่อน/แสดง, กระพริบ) **คำนวณในแต่ละเครื่องจากค่าที่ sync อยู่แล้ว** ใน `Render()` — ไม่ sync string ชื่อท่า ไม่ RPC เพื่อเปลี่ยนภาพ (NR-1, NR-8, NR-9)
8. Object ที่ต้องเคลื่อนตรงกันสองเครื่อง (แพ, แมงกะพรุน) คำนวณจาก tick (`Runner.Tick`) + ค่าเริ่มที่ networked; ถ้าต้องจำลองฝั่ง client ใช้ `Runner.SetIsSimulated(Object, true)`
9. Scene transition ผ่าน `G_SceneManager` เท่านั้น; singleton network สืบ `SingletonNetwork<T>` (DontDestroyOnLoad, ตัวซ้ำถูก Destroy)
10. ใช้ Input System ใหม่ (`InputSystem_Actions`) ไม่ใช้ `Input.GetKey`

## 4. กับดักที่เคยเจอ
- **Solo:** ห้ามเช็ค `Object.InputAuthority == Runner.LocalPlayer` เพราะทั้งสองตัวมี input authority เดียวกัน ใช้ `MovementCharacter.HasInputAuthority` (ซ่อนด้วย `new` ให้รวม `IsSoloFollower`) และ `IsOwner`
- **Render เขียน root transform** ของตัวที่อ่าน `rb2D.position` ไม่ได้ — Physics2D sync ค่ากลับเข้า body แม้ `simulated` ปิด (SO-7) ให้วาดลง child/visual แทน (`RenderOwnerVisual`)
- `Camera.main` เป็น null ในเกม ใช้ `CameraCharacter.LocalCamera`
- Pause/บทพูดหยุดโลกผ่าน `GameManager.IsGameplayFrozen` + `timer = GameplayPause.Hold(timer, Runner)` ทุก tick ที่หยุด; **หน้าสอนเล่นไม่หยุดโลก** (TU-1) ห้ามเอากลับเข้า `IsGameplayFrozen`
- `GameManager.SpawnDropItem(prefab, pos, breaker)` สั่ง `IgnoreCollision` กับ breaker

## 5. Checklist ก่อนส่งงาน network
- [ ] ทุกการเปลี่ยน state อยู่ฝั่ง State Authority หรือเป็นของเจ้าของตามตารางข้อ 2
- [ ] ไม่มี RPC ซ้ำกับผลที่ host ทำจาก input อยู่แล้ว
- [ ] เสียง/GUI ครั้งเดียว gate แล้ว หรืออยู่ใน Render/OnChangedRender
- [ ] ไม่มีการแตะ `[Networked]` ก่อน `Spawned()`
- [ ] ใช้ได้ทั้ง Duo (Host + Client) และ Solo (`GameMode.Single`) หรือระบุชัดว่าใช้แค่โหมดไหน
- [ ] เขียน checklist เทส 2 คน ตาม skill `mppm-2player-test`
