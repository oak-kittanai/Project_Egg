# GDD Audit — Easter Bond (Project_Egg)

- **วันที่ตรวจ:** 2026-10-02
- **Branch ที่ตรวจ:** `branch-UpdateSession` @ `17719e1a` (commit ล่าสุด 2026-09-01)
- **วิธีตรวจ:** Read-only / static — อ่านโค้ดทั้ง `Assets/Scripts` (131 ไฟล์, ~15,400 บรรทัด) + วิเคราะห์ YAML ของ scene / prefab / AnimatorController / ProjectSettings
- **สิ่งที่ไม่ได้ตรวจ:** ไม่ได้รันเกม ไม่ได้ทดสอบ 2 เครื่อง Unity MCP ตอบ "Connection revoked" จึงอ่าน Console ตรง ๆ ไม่ได้ (ใช้ `Editor.log` แทน) ทุกข้อที่เป็นพฤติกรรมตอนรันจริงจึงเป็น **ข้อสรุปจากการอ่านโค้ด** และติดป้าย "ต้องยืนยันตอนรัน" ไว้
- **GDD อ้างอิง:** ใช้ checklist A–L ใน prompt (ใน repo ไม่มีไฟล์ GDD) ตามที่ตกลง เน้น **โค้ด / mechanic** ส่วน Art, Storyboard, จำนวนเฟรม sprite ตรวจแค่ "มี/ไม่มี"
- **สัญลักษณ์:** ✅ เสร็จตาม GDD | ⚠️ มีแต่ไม่ครบ/ต่างจาก GDD | ❌ ไม่มี | ❓ หาไม่เจอ/ยืนยันไม่ได้

---

## การตัดสินใจจากเจ้าของโปรเจกต์ (2026-10-02)

หลักการ: **ยึดเกมตามที่เป็นอยู่ แล้วแก้ GDD ให้ตรงกับเกม** — สถานะ ⚠️/❌ ในตารางหัวข้อ 2 คือผลเทียบกับ GDD ฉบับเดิม ข้อที่อยู่ในตารางนี้ถือว่า "ปิดแล้ว" ไม่ต้องแก้โค้ด

| เรื่อง | การตัดสินใจ | ผลต่องาน |
|---|---|---|
| Target build (D5) | **PC เป็นหลัก** WebGL เป็นการทดลอง | H-5, H-6 ลดเป็นเรื่องของ CI/ขนาดไฟล์ ไม่ block การส่ง |
| ปุ่มดำน้ำ (D1) | **F** ตามโค้ด | แก้ Game Flow ใน GDD |
| Key Extra / ห้องลับ | **เลื่อนไปอนาคต** ไม่ทำรอบนี้ | ต้องจัดการ `Item_Key` / `Item_GoldKey` ที่ค้างในด่าน (ดูงานข้อ 4) |
| ฉากจบ | **คงแบบปัจจุบัน** จบ S3 แล้วกลับ lobby | ไม่ทำฉากจบ แต่ flow กลับ lobby ต้องใช้ได้จริง (C-1) |
| ตาย / Game over (D3, D18) | **เกิดใหม่อัตโนมัติเหมือนเดิม** ที่ checkpoint ล่าสุด | ไม่ทำหน้า "YOU ARE FAILED" |
| เป็ด oxygen หมด (D2) | **คงเดิม**: ว่ายขึ้นอัตโนมัติ 2 วิ แล้วเสีย 1 HP/วิ | แก้ GDD |
| Mira ทั้งหมด (D8–D11) | **GDD ตามเกม** | แก้ GDD: บินแบบจับเวลา, glide ไม่จำกัด, เป็ดยกนก, ตกน้ำลึกค่อย ๆ เสียเลือด |
| มอนสเตอร์ (D12, D13) | **GDD ตามเกมไปก่อน** | แก้ GDD: Knight Jay พุ่ง 1 ครั้ง, เกิดใหม่ทุก 8 วิ, Puffy ไม่มี alert |
| แพลตฟอร์มล่องหน (D14) | **ตามเกมไปก่อน** | แก้ GDD: จางเมื่อเหยียบ |
| วิ่ง, Fall damage, บอส, preview ด่าน (D4, D6, D7) | **ตัดออกจาก scope** | ลบออกจาก GDD |
| เครื่องยิงหิมะ (D15) | **คงไว้** | ย้ายจากรายการ "ไม่ใช้" ไปรายการใช้จริงใน GDD |

**ยังไม่ได้ตัดสินใจ (ไม่ block):** D16 ตัวเลข "1/2 Players" = จำนวนคนกด Ready, D17 Esc ปิดเมนูไม่ได้ + Resume ต้อง vote 2 คน, D19 ชื่อ product ใน build, Quest UI / ตัวนับกุญแจ (มีระบบแต่ไม่มีใครเรียก — จะเปิดใช้หรือเอาออก)

---

## 0. สภาพโปรเจกต์ทั่วไป

### 0.1 Git
| รายการ | ผล |
|---|---|
| Branch ตอนเริ่ม | `main` (clean) → checkout `branch-UpdateSession` เพื่อ audit (ตามที่ตกลง) |
| Uncommitted changes | ไม่มี (มีเฉพาะ `GDD_AUDIT.md` ไฟล์นี้ที่ untracked) |
| Remote | `origin` = `github.com/oak-kittanai/Project_Egg` ทุก branch local ตรงกับ origin |

| Branch | Commit ล่าสุด | วันที่ | เทียบกับ `main` |
|---|---|---|---|
| `branch-UpdateSession` | `17719e1a` update | 2026-09-01 | **ahead 86 / behind 0** — ยังไม่ merge |
| `LevelDesign-RollBack` | `949c2b3b` Map3 | 2026-06-27 | ahead 78 — ยังไม่ merge (อยู่ใน UpdateSession ครบแล้ว) |
| `dev` | `e9ecbea1` up | 2026-06-12 | ahead 17 / behind 8 — ยังไม่ merge (อยู่ใน UpdateSession ครบแล้ว) |
| `main` | `66bebb90` Merge branch 'dev' | 2026-06-08 | — |
| `branch-TestCharacterMechaics` | `239c71a6` | 2026-03-15 | merge แล้ว |
| `branch-InGameUI` | `11aedb2f` | 2026-03-13 | merge แล้ว |
| `branch-Prototype` | `ca87bb67` | 2026-01-28 | merge แล้ว |
| `branch-mechaics_boss&character` | `901d4d8d` | 2025-12-16 | merge แล้ว |
| `branch-map-LevelDesign` | `b7fcfe3f` | 2025-11-26 | merge แล้ว |
| `branch-DemoGame`, `branch-PreAssets` | `a084ce40` Setup | 2025-09-02 | merge แล้ว |
| `origin/gh-pages` | deploy (WebGL) | — | branch สำหรับ deploy |

**สิ่งที่ `main` ยังไม่มี (อยู่แค่ใน `branch-UpdateSession`):** `GameBootstrapper`, `DialogueVoteManager`, `MiniDialogueManager`, `MonsterRadarRelay`, `UIKeyboardNavigator` + `SelectionHighlightFollower` (เล่นเมนูด้วยคีย์บอร์ด/จอย), `NetworkDebugOverlay`, `GameSessionData` / `TutorialProgressSO`, `IRideablePlatform`, asmdef `MainGame.Core`, ชุด PlayMode tests 7 ไฟล์, รองรับ Gamepad ใน `OnInput`, การ refactor Session/leave-flow, scene `MadScene` / `Showcase`, และการแก้ทุก stage scene
→ CI (`.github/workflows/build.yml`) build จาก `main` เท่านั้น ดังนั้น build ที่ deploy อยู่บน gh-pages เป็นของเก่ากว่างานจริง ~3 เดือน

### 0.2 Git LFS
- Track เฉพาะ `*.unity` และ `*.mp4` (`.gitattributes`) — PNG / เสียง **ไม่ได้อยู่ใน LFS**
- `.unity` ทั้ง 16 ไฟล์ใน working tree เป็น YAML จริง (ขึ้นต้น `%YAML`) ไม่มี pointer เสีย, LFS object ครบในเครื่อง
- `git lfs fsck --pointers` ฟ้อง 1 ไฟล์: `Assets/Resources/UI_Assets/UI/Video/Loading Scene.mp4` (4.4 MB) ถูก commit เป็นไฟล์ดิบ ไม่ใช่ pointer
- `Assets/StreamingAssets/Stage1-S1/Cutscene1.mp4` ขนาด **501 MB** (ดู Bug H-6)
- ประวัติ `dev` มี commit `bd25c72c "Fix: restore Stage1-S1/S3 from RollBack (LFS pointer corruption)"` — เคยมีปัญหา pointer เสียมาก่อน ปัจจุบันไม่พบ

### 0.3 เวอร์ชัน / Packages
| รายการ | ค่าจริง | หมายเหตุ |
|---|---|---|
| Unity | `6000.2.6f2` (`ProjectSettings/ProjectVersion.txt`) | ตรงกับที่แจ้ง |
| Photon Fusion | `2.0.8 Stable 1402` (`Assets/Photon/Fusion/build_info.txt`) + Fusion Physics addon | |
| Fusion config | Tick rate 60, PeerMode Single, PlayerCount 2, weave `MainGame.Core` แล้ว (`NetworkProjectConfig.fusion`) | ตรงกับที่แจ้ง |
| Game mode | `GameMode.Host` / `GameMode.Client` (`SessionManager.cs:236, 285`) | ตรงกับที่แจ้ง |
| Photon region | `FixedRegion: asia` (`PhotonAppSettings.asset`) | |
| URP | 17.2.0 (2D) | |
| Input | Input System 1.14.2, `activeInputHandler: 2` (Both) | โค้ด UI บางจุดยังใช้ `Input.*` แบบเก่า — ใช้ได้เพราะตั้งเป็น Both |
| อื่น ๆ | DOTween Pro, Firebase (ไม่มีโค้ดเรียกใช้), `com.unity.ai.assistant`, Multiplayer Play Mode 1.6.3 | |

**ต่างจากข้อมูลที่แจ้งมา:**
1. **Player ไม่ได้เป็น prefab เดียว** — แยกเป็น `Player(Bird).prefab` และ `Player(Duck).prefab` (`Assets/Resources/Prefabs/Character_Prefabs/`) แต่ละตัวมี moveset เดียว; `CapsuleCollider2D` 2 อัน (normal/carry) มีเฉพาะฝั่ง Duck
2. **ไม่พบ Pixel Perfect Camera** ใน scene หรือ prefab ใดเลย (ค้น GUID ของ `PixelPerfectCamera.cs` ทั้ง `Assets/` ได้ 0) — กล้องใน player prefab มีแค่ `Camera` + `UniversalAdditionalCameraData` + `CameraCharacter`
3. **ไม่พบ `MatchDataSO`** — สิ่งที่ทำหน้าที่นี้คือ `GameSessionData` (`Assets/Scripts/ScriptableObjects/GameSessionData.cs`)

### 0.4 Compile / Console
- Unity เปิดโปรเจกต์นี้อยู่ระหว่าง audit — `Editor.log` (2026-10-02 22:44): **0 compile error**, 6 warning ในโค้ดโปรเจกต์:
  - `InsertItem_Mech.cs:57` CS0219 ตัวแปร `insertedAny` ไม่ถูกใช้
  - `JellyfishTrap.cs:104` CS0162 unreachable code
  - `GameManager.cs:47` CS0414 `currentLoadingUI` ไม่ถูกใช้
  - `Enemy_KnightJay.cs:195` CS0114 `OnDrawGizmos` ซ่อน method ของ base
  - `SessionHub.cs:230` CS0114 `OnDestroy` ซ่อน `SingletonNetwork.OnDestroy` (มีผลจริง ดู M-9)
  - `SessionHub.cs:43` CS0067 event `OnStateChanged` ไม่ถูกใช้
- Exception ใน log มีแต่ของ package `com.unity.ai.assistant` ไม่ใช่ของเกม
- ❓ Runtime error ตอน Play ยังไม่ได้ตรวจ (ไม่มีการรัน Play mode ใน log)

### 0.5 Build Settings
ลำดับ: `SessionScene` → `Stage1-S1` → `Stage1-S2` → `Stage1-S3` (`ProjectSettings/EditorBuildSettings.asset`)
- Scene ที่โค้ด/scene อ้างถึง (`SessionScene`, `Stage1-S1/S2/S3`) อยู่ใน build ครบ ไม่มี scene ที่ใช้จริงแต่หลุด build
- Scene นอก build: `Stage-Sample`, `TestUIScene`, `MadScene`, `Showcase`, `_Recovery/0*.unity` ×4, DOTween examples ×3, `URP2DSceneTemplate`
- Player settings: productName = **`Project_EggGame`** (ไม่ใช่ Easter Bond), company `MadMakeGame`, version 0.01, 1920×1080 fullscreen

### 0.6 ความสะอาดของโค้ด
- `TODO/FIXME/HACK`: ไม่มี มีแค่ `// Bug` (`NetworkInteractableWater.cs:116`), `// Test` (`MovementCharacter.cs:385`, `BearTrapScript.cs:23`), "ใช้ชั่วคราว" (`TriggerDialogue.cs:87`), `// need Animation` (`CharacterAnimation.cs:277`)
- `Debug.Log*`: 45 จุดใน 20 ไฟล์ — ที่มีผลตอนเล่นจริงคือ `CharacterAnimation.cs:138` (ดู M-7)
- โค้ด comment ทิ้ง: น้อย (`JellyfishTrap.cs:78-87`, `SingletonNetwork.cs:16-19`, `TriggerDialogue.cs:150,171`)
- Script / asset ที่ไม่ถูกใช้: ดูหัวข้อ 6

---

## 1. สรุปภาพรวม % ความครบ (โดยประมาณ)

| หมวด | % | สรุปสั้น |
|---|---|---|
| A. ภาพรวม | 85 | ครบ ยกเว้น preview ภาพรวมด่าน |
| B. Lobby / UI Flow | 70 | หน้าจอครบ แต่ flow "กลับ lobby / คนหลุด" น่าจะพัง (C-1) |
| C. In-Game HUD | 60 | ไม่มีหน้า Failed, หน้าจบ, ตัวนับกุญแจ; Quest UI ไม่มีใครเรียก |
| D. Controls | 80 | ไม่มีวิ่ง (Shift); บินไม่ใช่ toggle |
| E. Player Mechanic ร่วม | 70 | ไม่มี fall damage; ไม่มี hit animation |
| F. Mira (เหยี่ยว) | 70 | ครบเชิงระบบ แต่ตัวเลข/พฤติกรรมต่างจาก GDD หลายจุด |
| G. Kael (เป็ด) | 85 | ครบ ต่างที่ผลของ oxygen หมด |
| H. Animation | 75 | sync ครบ; ขาด Hold Friend, Interact ของ Mira, hit ของ Kael |
| I. Enemies | 70 | มี 2 ตัวครบ แต่พฤติกรรมต่าง (ไม่ใช่ 3 ครั้ง, เกิดใหม่ได้, ไม่มี alert) |
| J. Objects / Traps | 80 | ครบเกือบหมด; Key Extra ใช้ไม่ได้; แพลตฟอร์มล่องหนทำงานกลับด้าน |
| K. Levels | 75 | 3 ด่าน + warp ครบ; ไม่มีฉากจบ |
| L. Art / Audio (ตรวจเบา) | 60 | ไม่มี Pixel Perfect Camera; SFX ขาดหลายตัว |
| **รวม (ถ่วงน้ำหนักฝั่งโค้ด)** | **~75** | เนื้อหาเกมมีครบพอเล่นจบ 3 ด่าน งานที่เหลือส่วนใหญ่คือ flow ปิดเกม + ช่องว่างกับ GDD |

---

## 2. ตาราง Checklist

### A. ภาพรวม
| รายการ | สถานะ | หลักฐาน | หมายเหตุ |
|---|---|---|---|
| Online co-op 2 คน ผ่าน Photon Fusion | ✅ | `SessionManager.StartSession()` `:227-262` (Host, PlayerCount 2), `JoinRoom()` `:264-307`; `NetworkRunner.prefab` | Host mode, tick 60, region asia |
| Kael = เป็ด, Mira = เหยี่ยว | ✅ | `SessionHub.cs:98-99` (`characterDuck="Kael"`, `characterBird="Mira"`); `Duck_Moveset.cs`, `Bird_Moveset.cs`; `CenterHost.SpawnPlayer()` `:133-160` | |
| กล้องตามผู้เล่นของแต่ละเครื่อง | ✅ | `CameraCharacter.InitializeAsLocal()` `:28-52`, `LateUpdate()` `:80-115`; เรียกจาก `MovementCharacter.Spawned()` `:278-285` | มีระบบขอบกล้อง (layer `OutofBound`, `littleBitOfBound`) |
| Preview ภาพรวมด่านก่อนเริ่มเล่น | ❌ | ไม่พบโค้ด camera pan/overview (grep `preview` = 0) | ก่อนเริ่มมีแค่ cutscene/loading video |
| แพลตฟอร์ม PC / Keyboard | ✅ | `INetworkStructure.OnInput()` `:53-122` | มี Gamepad เพิ่มมาด้วย; CI build เป็น WebGL (ดูหัวข้อ 3) |

### B. Lobby / UI Flow
| รายการ | สถานะ | หลักฐาน | หมายเหตุ |
|---|---|---|---|
| Home: Play / Setting / Exit + โลโก้ | ✅ | `SessionScene.unity` (GameObject `play`, `setting`, `Exit`, sprite `logogame.png`); `SessionHub.Setup()` `:146-215` | Exit = `Application.Quit()` `:810-813` |
| Section Room: Create / Join / ย้อนกลับ | ✅ | `SessionScene` (`Create_Button`, `Join_Button`, `Leave_Button`); `SessionHub.CreateRoom()` `:342`, `JoinSessionRoom()` `:353` | |
| Room: เลือกตัวละครด้วยลูกศร | ✅ | `RuntimeUpdate.ChangeTypeRequest_RPC()` `:61-103`; `Character_Change_Add/Reduce` | วน 3 ค่า: ??? → Kael → Mira |
| Room: You / Guest | ✅ | sprite `You p1.png`, `friend p1.png` ใน `SessionScene` | ชื่อ text ในโค้ดคือ "Player1"/"Player2" (`SessionHub.cs:679-692`) |
| Room: "1/2 Players" | ⚠️ | `SessionHub.UpdateOverTime()` `:663-670` | ตัวเลขคือ **จำนวนคนที่กด Ready** ไม่ใช่จำนวนคนในห้อง |
| Room: Room Code + copy | ✅ | `GenerateSessionCode()` `SessionManager.cs:309-321` (เลข 6 หลัก); `CopyKeyToClipboard()` `SessionHub.cs:707` | |
| Room: Start กดได้เมื่อครบ 2 คน | ⚠️ | `SessionHub.ReadyToPlay()` `:471-493` | เงื่อนไขจริง: ทั้งคู่กด Ready **และ** เลือกตัวละครไม่ซ้ำกัน (มีปุ่ม Ready เพิ่มจาก GDD) |
| Room: ปุ่มปิด | ✅ | `SessionHub.LeaveRoom()` `:401-422`; sprite `X.png` | |
| Join Room: ช่อง code + Join + ข้อความ | ✅ | `SessionScene` text `Ask your friend for the room code`, `ENTER ROOM CODE`; `SessionHub.JoinRoom()` `:361-377` | ตรวจความยาว 6 ตัว |
| Profile บอกว่าเล่นตัวไหน | ✅ | `SessionHub.ChangeType()` `:592-626`, `UpdateIcon()` `:628-639` | |
| Setting: slider เพลง / เอฟเฟกต์ + บันทึก | ✅ | `GameSettingsSO.LoadSettings/SaveSettings()` (PlayerPrefs `MusicVolume`, `SFXVolume`); `SessionHub.cs:777-804`; ในเกม `MenuController.cs:242-260` | บันทึกข้าม session ได้ |
| เมนู Esc เปิด/ปิด ระหว่างเล่น | ⚠️ | `MovementCharacter.HandleEtcInput()` `:533-560` → `GameManager.RequestOpenMenu()` `:695-705`; `MenuController.cs` | Esc **เปิดได้อย่างเดียว** ปิดต้องกด Resume และ **ต้องกดทั้ง 2 คน** (vote); pause เป็นของทั้งห้อง; มีปุ่ม Reset + Tutorial เพิ่มจาก GDD |
| Esc: Quit กลับ lobby | ⚠️ | `MenuController.RPC_QuitGame()` `:269-279` → `GameManager.BackToSessionScene()` `:295-301` | พาออกทั้ง 2 คน; น่าจะไม่กลับถึง lobby จริง (C-1) |
| Cutscene เปิดเกม + Skip | ⚠️ | `CutsceneManager.cs:71-84, 127-146`; `LevelData.cutsceneFolderName = "Stage1-S1"`; `StreamingAssets/Stage1-S1/Cutscene1.mp4` | เป็นวิดีโอไฟล์เดียว เนื้อหา 18 ฉาก ❓ไม่ได้ตรวจ; Skip ต้องกดทั้ง 2 คน; S2/S3 ไม่มีไฟล์ → อาศัย `OnVideoError` ข้ามให้ |
| คนหลุด / Host ออก / กลับ lobby แล้วเข้าใหม่ | ⚠️ | `INetworkStructure.OnPlayerLeft()` `:154-159`, `OnShutdown()` `:25-37`, `GameManager.ReturnToSession()` `:305-321` | เจตนาคือ "ใครหลุด → ออกทั้งคู่กลับ lobby" แต่เงื่อนไขน่าจะไม่เข้า (C-1); ไม่มี reconnect / ข้อความแจ้ง |

### C. In-Game HUD
| รายการ | สถานะ | หลักฐาน | หมายเหตุ |
|---|---|---|---|
| HP bar แยกแต่ละคน 5/5 | ✅ | `PlayerInterface.UpdateHealthUI()` `:366-378`; `MovementCharacter.OnHealthChanged()` `:1220-1226`; `CharacterStats.s_maxHealth = 5` | แสดงเฉพาะของตัวเอง (`HasInputAuthority`) |
| Oxygen bar | ✅ | `PlayerGUI.StartOxygenTracking()` `:76-89`; เรียกจาก `Duck_Moveset.OnDivingStateChanged()` `:397`, `Bird_Moveset.OnDrownTimerStateChanged()` `:208` | เป็นฟองอากาศเหนือหัวตัวละคร |
| Skill icon (cooldown / unlock) | ✅ | `SkillGUI.cs`; `PlayerInterface.SetupSkills()` `:300-336`; prefab `Item_Skill_Bird_Fly/Throw`, `Item_Skill_Duck_Dive/Smash` | |
| Interact prompt | ✅ | `PlayerInterface.ShowInteract()` `:686-691`; `MovementCharacter.CheckInteractable()` `:1418-1466`; prefab `Button_E.prefab` | เป็นไอคอนปุ่ม E ลอยเหนือวัตถุ ไม่ใช่ข้อความ "Press E to pick up items" |
| ตัวนับกุญแจ | ❌ | `GameManager.TeamBlueKeys/TeamOrangeKeys` `:68-69` มีค่า แต่ไม่มี UI แสดง และไม่มี object ใดเรียก `AddKey` (`KeyScript` ใช้ 0 ที่) | มีแค่ `ItemOverlay` แสดงของที่ถืออยู่ 1 ช่อง |
| Quest text | ⚠️ | UI: `PlayerInterface.UpdateQuestUI()` `:391-430`; ระบบ: `GameManager.StartGlobalQuest()` `:574-586` | **ไม่มีที่ไหนเรียก `StartGlobalQuest`** (ทั้งโค้ดและ UnityEvent) → Quest UI ไม่เคยขึ้น |
| Off-screen indicator | ✅ | `OffScreenIndicator.cs`; `FriendTracker.prefab` ซ้อนใน `Canvas.prefab` | |
| Popup อ่านโน้ต | ✅ | `Note.Interact()` `:29-41` → `PlayerInterface.ShowNote()` `:453-462`; `Item_Note.prefab` (S1 ×3, S2 ×2, S3 ×1) | ปิดด้วย Tab; รองรับไทย/อังกฤษ |
| หน้า "YOU ARE FAILED" + Retry / Home | ❌ | ไม่พบ UI/โค้ด (`Canvas.prefab` ไม่มี object นี้; `GameManager.gameOver` `:67` ไม่ถูกใช้) | ตอนนี้ตายแล้วเกิดใหม่อัตโนมัติใน 2.5 วิ |
| หน้าจบด่าน / จบเกม | ❌ | ไม่พบ | จบ S3 แล้ว warp กลับ lobby เลย (ดู K) |

### D. Controls
| รายการ | สถานะ | หลักฐาน | หมายเหตุ |
|---|---|---|---|
| A/D + Arrow = เดิน | ✅ | `INetworkStructure.cs:69-72` | W/S + ↑/↓ = แกนตั้ง (ปีนเถาวัลย์, ว่ายน้ำ) |
| Shift = วิ่ง | ❌ | ไม่มีปุ่ม Shift ใน `NetworkInputData` `:231-246`; `HandleMovement()` `MovementCharacter.cs:575-586` ใช้ `stats.maxSpeed` ค่าเดียว | `s_walkSpeed` มีใน `CharacterStats` แต่ไม่ถูกใช้ |
| Space = กระโดด (บนพื้น) | ✅ | `HandleJump()` `MovementCharacter.cs:592-615` (`IsGrounded` + cooldown 1 วิ) | **↑ ก็กระโดดด้วย** (`INetworkStructure.cs:74`) |
| E = Interact / เก็บของ / ยกเพื่อน | ✅ | `HandleInteraction()` `:718-759`; `Duck_Moveset.HandleDuckInteraction()` `:200-228` | ยกเพื่อนได้เฉพาะเป็ด |
| ปุ่มดำน้ำ | ✅ | **F** — `Duck_Moveset.HandleWaterLogic()` `:317-330` | Space ในน้ำ = กระโดดขึ้นจากผิวน้ำ (`:151-154`) ตรงกับตาราง Control ไม่ตรงกับ Game Flow |
| Space ×2 = บิน (toggle) | ⚠️ | `Bird_Moveset.HandleFlightLogic()` `:282-348` | กด Space ซ้ำกลางอากาศ = เริ่มบิน **แต่ยกเลิกเองไม่ได้** บินจนหมดเวลาแล้วเข้า glide; กดอีกครั้งตอน glide = สลับ glide/ร่วง |
| Esc = เมนู | ✅ | `INetworkStructure.cs:77`; `HandleEtcInput()` | |
| (เพิ่มจาก GDD) Q = เล็ง/ปาหิน, G = วางของ, Tab = ปิดโน้ต/tutorial/dialogue | — | `INetworkStructure.cs:78-80` | ควรใส่ในตาราง Control ของ GDD |

### E. Player Mechanic ร่วม
| รายการ | สถานะ | หลักฐาน | หมายเหตุ |
|---|---|---|---|
| HP แยก + คนหนึ่งตาย → ทั้งคู่เกิดใหม่ | ✅ | `MovementCharacter.CharacterDie()` `:869-921` (สั่ง partner ตายตาม `:908-920`), `Respawn()` `:923-963` | เกิดที่ `GameManager.respawnPos` = checkpoint **ล่าสุดที่แตะ** หรือจุดเริ่มด่าน |
| Checkpoint (sync 2 คน) | ✅ | `CheckpointTrigger.cs:18-30` → `GameManager.UpdateRespawnPos()` `:339-349` (`[Networked] respawnPos`); `Spawnpoint_WayMark.prefab` ×2 ต่อด่าน | ใครแตะก็ได้ มีผลกับทั้งคู่ |
| Fall damage | ❌ | ไม่พบ — `FallingCheck()` `:1365-1371` ปรับแค่ gravity/ความเร็วตก | |
| น้ำหนักตัวละครต่างกัน | ✅ | `WeighingScale_Mech.CalculateSide()` `:236-237` (Duck 130, Bird 100) | เป็นค่าคงที่ในโค้ด ไม่ได้ผูกกับ `Rigidbody2D.mass` (ทั้งคู่ mass = 1) |
| Hit animation / i-frame | ⚠️ | i-frame: `InvincibleTimer` `:170-171, 767, 790` (1.5 วิ); `DamageFlash.cs` | มี i-frame + กระพริบ แต่ **ไม่มี hit animation** (clip `Ad_hit` ถูกใช้เป็นท่าทุบหิน) |

### F. Mira — เหยี่ยว
| รายการ | สถานะ | หลักฐาน | หมายเหตุ |
|---|---|---|---|
| บินได้ระยะหนึ่ง (stamina 1 / อุ้มลด 3) | ⚠️ | `Bird_Moveset.StartFlying()` `:393-417`; prefab: `normalFlyTime = 0.9`, `carryFlyTime = 0.8` วิ | เป็น **ตัวจับเวลา** ไม่ใช่ stamina; อัตรา เปล่า:อุ้ม ≈ 1 : 0.89 (GDD 1 : 3); `s_maxStamina` ไม่ถูกใช้ |
| หมดแล้วร่อน ~3 วิ | ⚠️ | `StartFloating()` `:370-380` (`floatingGravity`, damping 5) | ร่อนได้ **จนกว่าจะถึงพื้น** ไม่มีจำกัด 3 วิ |
| ปาหิน | ✅ | `HandleThrowLogic()` `:448-506`, `ExecuteThrow()` `:528-555`; `RockObject.cs`; `ThrowButton.cs`; `TreeSapObstacle.cs` | เล็งแบบแกว่งอัตโนมัติ ±30°; หินฆ่ามอนได้และ stun เป็ดได้ |
| อุ้มเป็ดบินข้ามช่องว่าง | ⚠️ | `Duck_Moveset.PickupFriend()` `:230-247`; `Bird_Moveset.cs:337-343` (นกที่ถูกยกบิน → ยกเป็ดขึ้นด้วย) | ทำได้ แต่กลไกคือ **เป็ดยกนกขึ้นหัว แล้วนกบินพาทั้งคู่** (GDD: เหยี่ยวอุ้มเป็ด) |
| isFlyUnlocked / isThrowUnlocked | ✅ | ปลดจาก `TriggerDialogue` ใน **Stage1-S1**: `Dialogue_EmptyFlying` → Bird_Fly, `Dialogue_TutorialRock (1)` → Bird_Throw; เก็บข้ามด่านที่ `GameManager.SkillBird*Unlocked` `:731-734` | ฝั่งเป็ด: `Dialogue_TutorialDive` → Dive, `Dialogue_TutorialRock` → Smash; S2/S3 ไม่มีจุดปลดเพิ่ม — ตรงดีไซน์ไหม ❓ ต้องเทียบ GDD |
| ตกน้ำลึก → กลับ checkpoint | ⚠️ | `Bird_Moveset.HandleDrowning()` `:138-200`; prefab `drowningTime = 6` | ไม่ได้กลับทันที: จม → oxygen 6 วิ → เสีย 1 HP/วิ จนตาย → ทั้งคู่เกิดใหม่ |

### G. Kael — เป็ด
| รายการ | สถานะ | หลักฐาน | หมายเหตุ |
|---|---|---|---|
| ลอยผิวน้ำ + ลอยพร้อมแบกเพื่อน | ✅ | `Duck_Moveset.HandleBuoyancy()` `:516-547`; animation `Floating_carry_walk/Idle` | |
| ดำน้ำ เคลื่อนที่อิสระ | ✅ | `StartDiveLogic()` `:377-395`, `HandleWaterLogic()` `:345-368`; prefab `divingTime = 8` | ดำน้ำไม่ได้ขณะแบกเพื่อน (`:380`) |
| Oxygen หมด | ⚠️ | `EndDivingLogic()` `:416-432` → `EmergencySwimup()` `:454-496` (2 วิ) → `TimeUp()` `:507-514` → `TickDrowningDamage()` `MovementCharacter.cs:808-817` | หมด → **ว่ายขึ้นอัตโนมัติ 2 วิ** → ถ้ายังจมอยู่ **เสีย 1 HP ทุก 1 วิ** (ไม่ใช่เสียเลือดทั้งหมด) |
| ยก/ช่วยเพื่อนขึ้นผิวน้ำ | ❓ | `HandleDuckInteraction()` ไม่ได้ห้ามยกตอนอยู่ใต้น้ำ | ตามโค้ดน่าจะทำได้ ต้องยืนยันตอนรัน |
| ทุบหิน | ✅ | `BreakableRock.cs:45-81`; `BreakAbleRock.prefab` (S1 ×3, S2 ×2, S3 ×2) | ต้องปลด `isSmashUnlocked` ก่อน |

### H. Animation (ตรวจเบา — ดูแค่ state ใน Animator ไม่นับเฟรม)
Controller: `Assets/Art-Source/ART/player/Duck_R/Duck_Controller.controller`, `.../Bird_R/Bird_Controller.controller` — ทุก state ไม่มี transition (ขับด้วย `animator.Play` จากโค้ด)

| รายการ | สถานะ | หลักฐาน | หมายเหตุ |
|---|---|---|---|
| Kael: Idle, Walk, Jump, Floating, Floating Idle, Floating+Friend (+Idle), Swim | ✅ | state `NormalMovementTree`, `Jump`, `Floating_walk`, `Floating_Idle`, `Floating_carry_walk`, `Floating_carry_Idle`, `Swim` | |
| Kael: Hit | ⚠️ | state `Smash` ใช้ clip `Ad_hit.anim` | ใช้เป็นท่าทุบหิน ไม่มีท่าโดนตี |
| Kael: Interact | ⚠️ | state ชื่อ **`interact`** (ตัวเล็ก) แต่โค้ดเรียก `"Interact"` (`CharacterAnimation.cs:216`) | `HasState` ไม่เจอ → ไม่เล่น (M-7) |
| Mira: Idle, Walk, Jump, Throw, Fly | ✅ | `NormalMovementTree`, `Jump`, `Aim`, `Throwing`, `Fly_Up`, `Fly`, `Falling` | |
| Mira: Hold Friend | ❌ | ไม่มี state | สอดคล้องกับที่กลไกจริงเป็น "เป็ดยกนก" |
| Mira: Interact | ❌ | ไม่มี state `Interact` ใน Bird controller | |
| Transition ถูกไหม | ❓ | ขับจากโค้ดทั้งหมด | ต้องดูตอนรัน |
| Sync ผ่าน network | ✅ | `CharacterAnimation.cs:13-18` (`[Networked] CurrentAnimState, AnimX, AnimY, FlipX`), `Render()` `:65-86` | |

### I. Enemies / AI
| รายการ | สถานะ | หลักฐาน | หมายเหตุ |
|---|---|---|---|
| Knight Jay | ⚠️ | `Enemy_KnightJay.MonsterSpecificUpdate()` `:73-166`; `Kinght_Jay.prefab` (S1 ×2, S2 ×6, S3 ×2) | เห็น → รอ 0.6 วิ → ล็อกตำแหน่งล่าสุด → **พุ่ง 1 ครั้ง** → หาย → **เกิดใหม่ที่เดิมหลัง 8 วิ** (GDD: เคลื่อน 3 ครั้งแล้วสลาย) |
| Puffy Jay | ⚠️ | `Enemy_PuffyJay.MonsterSpecificUpdate()` `:64-145`; `Puffy_Jay.prefab` (S1 ×3, S2 ×5, S3 ×2); `chaseTime = 5` | เดินตาม 5 วิ → หยุด → ระเบิด ✅; **ไม่มี action แจ้งว่าเจอ** และไม่มี animation เดิน; เกิดใหม่หลัง 8 วิ |
| AI รันบน Host + sync | ✅ | `BaseMonster.FixedUpdateNetwork()` `:59-67` (`HasStateAuthority`); ตำแหน่ง sync ผ่าน `[Networked] netPos` + `Render()` | หยุดตอน pause/dialogue (`IsGameplayFrozen`) |
| Weird Jay (old design) | ✅ | ไม่พบโค้ด/asset ค้าง (ค้นทุก branch) | |
| บอส | ❌ | ไม่มีโค้ด/prefab บอส; เจอแค่ `boss.png` ใน test assets ของ branch เก่า | ดูหัวข้อ 3 |

### J. Interact Objects / Traps / Level Elements
| รายการ | สถานะ | หลักฐาน | หมายเหตุ |
|---|---|---|---|
| Passel Key 1–2 + ประตูล็อก | ✅ | `Item_Passelkey_Mira/Kale.prefab` (`StoneItem`) → `GameManager.RPC_RequestAddStone()` `:461-468` → `InsertItem_Door.prefab` (`InsertItem_Mech.cs`) — S1, S2 | ต้องเก็บครบ 2 ชิ้นประตูจึงเปิด |
| Key Extra 1–5 + ห้องลับ | ⚠️ | `Item_Key.prefab`, `Item_GoldKey.prefab` (S1, S2) ใช้ `StoneKeyScript` ที่ **ตั้ง `isOrangeStone = 0` และ `isBlueStone = 0`** | กด E แล้วไม่เกิดอะไร (H-3); `KeyScript` / `GameManager.UseKey` ไม่มีใครใช้; สิ่งที่ใกล้เคียง "ห้องลับ" คือ `FadeWhenPlayerInside` 1 จุดใน S2 |
| กระดาษโน้ต | ✅ | `Note.cs`, `Item_Note.prefab` | |
| เถาวัลย์ปีนได้ (S2–S3) | ✅ | `ClimbingVine.cs` (S2 ×9, S3 ×23); `MovementCharacter` climb `:664-712` | ไม่มี animation ปีน (`UpdateClimbAnimation` ว่าง `:218-222`) |
| หินทำลายได้ | ⚠️ | `BreakableRock.cs` | ทุบได้เฉพาะก้อนที่ตั้ง `canDrop` + มี `itemToDrop` (`:47, 69`) — แบบ "ไม่มีของตก" ทุบไม่ได้; เศษซากเปลือกไข่ ❓ ไม่พบ |
| สวิตช์ 4 แบบ | ✅ | กดพร้อมกันครั้งเดียว/ค้าง: `CoopDoorManager.cs` + `CoopStepButton.cs` (`openOnceAndStayOpen`, `isSingleUse`); คนเดียวกดค้าง: `CoopDoorManager2.cs`, `PressurePlateHinge.cs`; ปาหินใส่: `ThrowButton.cs` (S2) | |
| แพลตฟอร์มเคลื่อนที่ / ร่วง / หมุน | ✅ | `MovingPlateform.cs` (S1 ×5, S2 ×4, S3 ×1), `CrumblingPlatform.cs` (7/8/3), `RotatingPlatform.cs` (S3 ×7) | |
| แพลตฟอร์มล่องหน (โผล่เมื่อแตะ) | ⚠️ | `FadingPlatform.cs:31-35`; `FadingPlatfrom.prefab` (S3, `minOpacity = 0`) | ทำงาน **กลับด้าน**: มองเห็นปกติ แล้วค่อย ๆ **จางหายเมื่อเหยียบ** (collider ยังอยู่) |
| กับดักหนาม / หมี / น้ำแข็งย้อย | ✅ | `DoDamage.cs` (`Trap_Simple_Thorn`, `Trap_Multi_Thorn`), `BearTrapScript.cs` (S1 ×9, S3 ×1), `Trap_Ice.cs` (`Trap_Stalactite`, `Small_Ice`, `Ice`) | |
| น้ำ (ตื้น/ลึก, oxygen) | ✅ | `NetworkInteractableWater.cs`; ตรวจหัว/ตัวจมที่ `MovementCharacter.CheckGround()` `:1300-1321` | น้ำชนิดเดียว ตื้น/ลึกตัดสินจากหัวจมหรือไม่ |
| ตาชั่ง + ของมีน้ำหนัก (S3) | ✅ | `WeighingScale_Mech.cs`; `WeighingScale.prefab` ใน S3: 4 โจทย์ (500/600/700/1000 เท่ากันสองฝั่ง), ของ 7 ชิ้น (`PuzzleItem`) | |
| ของที่ GDD ระบุ "ไม่ถูกนำมาใช้" | ⚠️ | ดูหัวข้อ 6 | สวิตช์ toggle = ของเหลือจริง; **เครื่องยิงหิมะถูกใช้อยู่** (`Snowball_Moshoot.prefab` S1 ×3, S2 ×7, S3 ×1) |
| (นอก GDD) Jellyfish, กับดักลมดัน/ดูด, ท่อนไม้กลิ้ง, ยางไม้, ประตูบานพับ, หินรูปทรง+แท่นวาง | — | `JellyfishTrap`, `TrapPressure`, `RollingLogTrap`, `TreeSapObstacle`, `HingeDoor`, `FStoneItem`/`StonePad`/`InsertStone_Mech` | ควรเพิ่มใน GDD หรือยืนยันว่าตั้งใจ |
| ทุก object sync ผ่าน network | ⚠️ | ส่วนใหญ่ใช้ `[Networked]` + `Render()` | มีจุดที่ใช้ RPC แทน state และจุดที่ยิงซ้ำ (ดูหัวข้อ 4 และตาราง Network); late join ไม่เกี่ยวเพราะห้องเต็มที่ 2 คน |

### K. Levels
| รายการ | สถานะ | หลักฐาน | หมายเหตุ |
|---|---|---|---|
| จุดเกิดต้นเกม | ✅ | `LevelData.SpawnPosition` ตั้งค่าแล้วทั้ง 3 ด่าน | ฉากป่าสน ❓ ไม่ได้ตรวจภาพ |
| Scene 1 (Tutorial) | ✅ | `Stage1-S1.unity`: TutorialTrigger ×11, จุดปลดสกิลครบ 4, BearTrap ×9, KnightJay ×2 + PuffyJay ×3, Passel key 2 + ประตู, น้ำ ×6 | องค์ประกอบครบ; ลำดับ puzzle ตรง GDD ไหม ❓ ต้องเล่นจริง |
| Scene 2 | ✅ | `Stage1-S2.unity`: เถาวัลย์ ×9, น้ำ ×4, มอน 11 ตัว, ThrowButton, บ้านนก (`Housetree`) | เช่นเดียวกัน ❓ ลำดับ |
| Scene 3 | ✅ | `Stage1-S3.unity`: ตาชั่ง 4 โจทย์, FadingPlatform ×19, Rotating ×7, Crumbling ×3, หนาม | |
| เปลี่ยนด่านพร้อมกัน + loading | ✅ | `WarpZone.cs:17-38` (ต้องอยู่ในโซนทั้ง 2 ตัว) → `GameManager.LoadNextLevel()` `:682-690` (`Runner.LoadScene`); S1→S2→S3 | มี loading screen (`GlobalLoadingManager`) |
| ฉากจบเกม / ปลดคำสาป | ❌ | `Stage1-S3`: `WarpZone` บน `InsertStone_Door` ตั้ง `returnToSession = 1` | ผ่านประตูสุดท้ายแล้วกลับ lobby ทันที ไม่มีฉากจบ/เครดิต |

### L. Art / Audio / Polish (ตรวจเบา)
| รายการ | สถานะ | หลักฐาน | หมายเหตุ |
|---|---|---|---|
| Tilemap, ต้นไม้, เมฆ, โมอาย, บ้านนก, parallax | ✅ | prefab `Tree-Tri`, `cloud big 1/2`, `moeye_0`, `Housetree`; `ParallaxBackground` + `ParallaxLayer` ×3 ทุกด่าน | ขนาด tile ❓ ไม่ได้ตรวจ |
| Pixel Perfect Camera / tile seam | ❌ | ไม่พบ component ในโปรเจกต์ | ดู 0.3 |
| BGM | ✅ | `AudioManager.PlayBGM("FirstBGM")` `:49, 61`; `BGM_Player.prefab` ทุกด่าน; lobby ใช้ `AudioSession` | เพลงเดียวทุกด่าน |
| SFX | ⚠️ | มี: เดิน, กระโดด, ลงพื้น, บิน, โดนตี, ตาย, เกิดใหม่, UI click, กับดักหมี, checkpoint, ตาชั่ง | **ไม่ได้ใส่ clip**: ว่ายน้ำ, ดำน้ำ/ขึ้นน้ำ, ยก/ปล่อยเพื่อน, ปาหิน (prefab = NONE); **ไม่มีโค้ด**: เก็บกุญแจ, เปิดประตู |
| SFX/BGM ผูกกับ slider | ✅ | `AudioManager.GetGlobalSFXVolume()` `:175`, `UpdateBGMVolumeRealtime()` `:201` | เสียงที่เล่นผ่าน `AudioSource.PlayOneShot` ตรง ๆ (กับดักหมี, checkpoint, ตาชั่ง, ป้อมยิง) **ไม่ผ่าน slider** |
| โลโก้ | ✅ | `logogame.png` ใน `SessionScene` | |
| Key Visual | ❓ | ไม่ได้ตรวจ | |

### ขั้นตอนที่ 2 — คุณภาพ Network (Fusion 2)
| รายการ | สถานะ | หลักฐาน | หมายเหตุ |
|---|---|---|---|
| ไม่เข้าถึง `[Networked]` ก่อน `Spawned()` | ✅ | `Awake()` ทุกตัวไม่แตะ networked property; `LevelData.Start()` `:52-56` รอ `Object.IsValid` | จุดเสี่ยงเล็ก: `OnTrigger*/OnCollision*` หลายตัวเช็ค `HasStateAuthority` โดยไม่เช็ค `Object.IsValid` (L-4) |
| Gameplay ใน `FixedUpdateNetwork`, visual ใน `Render/LateUpdate` | ⚠️ | โครงหลักถูก | มีตัวแปร **ไม่ networked** ถูกแก้ใน FUN จำนวนมาก (`isMoveAble`, `isOptional`, `_isEPressed`, `_wasJumpPressed`, `resetAnimation` ฯลฯ) → ไม่ถูก rollback ตอน resimulate (M-1) |
| RPC ถูกทิศ / ไม่ใช้แทน state | ⚠️ | 71 RPC: `RpcSources.All` 51, `StateAuthority` 19, `InputAuthority` 1 | เกือบทั้งหมดเปิด `All`; RPC ที่ควรเป็น state: `RPC_UpdateCarry` (`MovementCharacter.cs:1142`), `DeathMechanic_RPC` (`:852`), `SetVisuals_RPC` ของมอน, `RPC_BreakRock`, `CallDamageFlash_RPC` |
| Carry ใช้ `netRb.Teleport()` | ⚠️ | ตอนปล่อย/ตาย/เกิดใหม่ใช้ `Teleport` ✅ (`:1180-1183, 950-953`) | ตอน **กำลังถูกยก** ใช้ `rb2D.MovePosition()` (`:361-365`) บนทั้ง State และ Input authority; platform follow ใช้ `rb2D.position +=` (`:381`) |
| GUI/Sound gate ด้วย `HasInputAuthority && Runner.IsForward` | ⚠️ | `IsForward` มีแค่ 2 จุด (`:538`, `:1482`) | เสียงกระโดด (`:604`), กระโดดจากเถาวัลย์ (`:709`), ลงพื้น (`:1330`), ปาหิน (`Bird_Moveset.cs:546`) เช็คแค่ `HasInputAuthority` → ฝั่ง client อาจเล่นซ้ำตอน resimulate |
| Spawn/Despawn บน Host เท่านั้น | ⚠️ | Spawn ทุกจุดมี guard ✅ (`GameManager.cs:420, 437`, `LogSpawner`, `GameBootstrapper.cs:21`) | `FStoneItem.Collect()` `:14` เรียก `Runner.Despawn` โดยไม่เช็ค authority (M-3) |
| DontDestroyOnLoad ถูก cleanup | ⚠️ | `GameManager.ReturnToSession()` `:305-321` | HUD Canvas ไม่ถูกลบเพราะชื่อไม่ตรง (H-1); static ไม่ถูก reset (H-2, M-4) |

---

## 3. ความต่างระหว่าง GDD กับโค้ด (ต้องตัดสินใจ)

| # | เรื่อง | GDD | โค้ดทำจริง | หลักฐาน |
|---|---|---|---|---|
| D1 | ปุ่มดำน้ำ | ตาราง Control: F / Game Flow: Space ในน้ำ | **F** = ดำน้ำ/ขึ้น; Space บนผิวน้ำ = กระโดดขึ้นจากน้ำ | `Duck_Moveset.cs:317-330, 151-154` |
| D2 | เป็ด oxygen หมด | เสียเลือดทั้งหมดหลัง 2 วิ / หรือลด 1 | ดำได้ 8 วิ → ว่ายขึ้นอัตโนมัติ 2 วิ → ถ้ายังจม **เสีย 1 HP ทุก 1 วิ** จนกว่าจะพ้นน้ำ | `Duck_Moveset.cs:416-514`; `MovementCharacter.cs:108-109` |
| D3 | Respawn | จุดเริ่มต้น หรือ checkpoint ที่ใกล้สุด | checkpoint **ล่าสุดที่แตะ** (ถ้ายังไม่แตะ = จุดเริ่มด่าน); ไม่มีการคำนวณ "ใกล้สุด" | `GameManager.cs:33, 334-349`; `CheckpointTrigger.cs:18-30` |
| D4 | บอส | ถูกเอ่ยถึงในส่วนกล้อง ไม่มีดีไซน์ | ไม่มีบอสในโปรเจกต์ | — |
| D5 | แพลตฟอร์ม build | PC | CI build **WebGL** + deploy gh-pages จาก `main`; แต่ `AllowClientServerModesInWebGL = false` ขณะที่เกมใช้ Host/Client mode | `.github/workflows/build.yml`; `NetworkProjectConfig.fusion` |
| D6 | วิ่ง (Shift) | Shift + เดิน = วิ่ง | ไม่มี ความเร็วเดียว (`maxSpeed = 5`) | `MovementCharacter.cs:575-586` |
| D7 | Fall damage | มี | ไม่มี | — |
| D8 | การบินของ Mira | stamina: เปล่า −1, อุ้ม −3; กด Space ×2 toggle | จับเวลา 0.9 วิ / 0.8 วิ; ยกเลิกกลางทางไม่ได้ | `Bird_Moveset.cs:282-348, 393-417` |
| D9 | Glide | ~3 วิ | ไม่จำกัดเวลา | `Bird_Moveset.cs:370-380` |
| D10 | ใครอุ้มใคร | Mira อุ้ม Kael | Kael ยก Mira ขึ้นหัว แล้ว Mira บินพาทั้งคู่ | `Duck_Moveset.cs:230-276`; `Bird_Moveset.cs:337-343` |
| D11 | Mira ตกน้ำลึก | กลับ checkpoint | oxygen 6 วิ แล้วเสีย 1 HP/วิ จนตาย | `Bird_Moveset.cs:138-200` |
| D12 | Knight Jay | เคลื่อน 3 ครั้งแล้วสลาย | พุ่ง 1 ครั้ง หาย แล้วเกิดใหม่ทุก 8 วิ (มี class `DashMonster` ที่พุ่ง 3 ครั้งแล้ว despawn แต่ไม่ถูกใช้) | `Enemy_KnightJay.cs:73-166`; `DashMonster.cs` |
| D13 | Puffy Jay | มี action แจ้งว่าถูกเจอ; สลายไป | ไม่มี alert; เกิดใหม่ทุก 8 วิ | `Enemy_PuffyJay.cs:64-145` |
| D14 | แพลตฟอร์มล่องหน | มองไม่เห็น โผล่เมื่อแตะ | มองเห็น แล้วจางเมื่อเหยียบ | `FadingPlatform.cs:31-35` |
| D15 | เครื่องยิงหิมะ | ระบุว่าไม่ถูกนำมาใช้ | ใช้อยู่ 11 ตัวใน 3 ด่าน | `Snowball_Moshoot.prefab` |
| D16 | "1/2 Players" | จำนวนผู้เล่นในห้อง | จำนวนคนที่กด Ready; มีขั้นตอน Ready เพิ่ม | `SessionHub.cs:663-670` |
| D17 | เมนู Esc | เปิด/ปิด, Resume / Setting / Quit | เปิดอย่างเดียว; Resume ต้อง vote 2 คน; pause ทั้งห้อง; มี Reset, Tutorial เพิ่ม | `MenuController.cs` |
| D18 | Game over | หน้า YOU ARE FAILED + Retry / Home | เกิดใหม่อัตโนมัติ ไม่มีหน้า fail | `MovementCharacter.cs:349, 923` |
| D19 | ชื่อเกมใน build | Easter Bond | `Project_EggGame` | `ProjectSettings.asset` |
| D20 | ปุ่มเพิ่มจาก GDD | — | Q (ปา), G (วางของ), Tab (ปิด UI), ↑ = กระโดด, Gamepad | `INetworkStructure.cs:65-103` |

---

## 4. Bug / ความเสี่ยง

> ทุกข้อมาจากการอ่านโค้ด ยังไม่ได้ทำซ้ำตอนรัน ข้อที่ระบุ "ต้องยืนยันตอนรัน" คือข้อที่ผลขึ้นกับพฤติกรรม runtime ของ Fusion/Unity
> **อัปเดต 2026-10-16:** ผลเทส T1–T7 อยู่ในหัวข้อ 7.5 — สถานะของแต่ละข้อเขียนกำกับไว้ใต้หัวข้อนั้น

### Critical
**C-1 — ออกจากเกมแล้วไม่กลับ lobby** — **ยืนยันแล้ว 2026-10-16** (หัวข้อ 7.5): (b) เข้าประตูสุดท้าย S3 แล้วค้าง, (c) client หลุดแล้ว host อยู่ในด่านต่อ, (d) host หลุดแล้ว client ค้าง (log `OnShutdown … branch=LOBBY`) — ส่วน (a) Quit ผ่านเมนู Esc **กลับ lobby ได้ปกติ** (ไม่พบบั๊ก)
- **แก้แล้ว: `fix/c1-lobby-return` `6d6ae65a`** (2026-10-16, รอเทส): ใช้สถานะ `GameManager.IsInGameplay` (ตั้งใน `INetworkStructure.OnSceneLoadStart`, ล้างใน `ReturnToSession` / `ResetReturnGuard`) แทน `SessionManager.Instance == null` ใน `OnShutdown`, `OnPlayerLeft`, `OnDisconnectedFromServer`; `BackToSessionScene` เรียก `ReturnToSession` ต่อหลัง shutdown ถ้ายังอยู่ในเกม — เทสตามหัวข้อ 11.3
- `INetworkStructure.cs:27` ใช้ `SessionManager.Instance != null` ตัดสินว่า "อยู่หน้า lobby" และ `:21`, `:157` ใช้ `== null` ตัดสินว่า "อยู่ในเกม"
- แต่ `SessionManager` อยู่บน GameObject เดียวกับ `SessionHub` (`SessionManager.prefab`, เป็น root ใน `SessionScene`) และ `SessionHub` สืบทอด `SingletonNetwork` ซึ่งเรียก `DontDestroyOnLoad(gameObject)` (`SingletonNetwork.cs:30`) → object นี้ **ยังอยู่ตลอดตอนเล่น** (`GameManager.cs:316-318` ก็เขียน comment ยืนยันว่าค้างอยู่)
- ผล: ตอนอยู่ในเกม `OnShutdown` เข้า branch lobby (`DisconnedFromServer()`) ไม่เรียก `GameManager.ReturnToSession()` (`:35`) → runner ปิดแล้วแต่ไม่โหลด `SessionScene`; และ `OnPlayerLeft` / `OnDisconnectedFromServer` ไม่สั่ง shutdown เมื่ออีกฝั่งหลุด
- `dev` branch ไม่มีปัญหานี้ (โหลด scene ตรง ๆ ใน `BackToSessionScene`) — เป็นผลจากการ refactor ใน `branch-UpdateSession`

### High
**H-7 (ใหม่ 2026-10-16) — host ที่เหลือคนเดียวหลัง client หลุด กด Tab ปิด dialogue แล้วยังขยับไม่ได้** (T4-4d) — ผลพวงของ C-1(c): ตามเจตนาในโค้ด (`INetworkStructure.OnPlayerLeft` "อีกคนออก → ออกทั้งคู่") host ไม่ควรเหลืออยู่ในด่าน ถ้าแก้ C-1(c) ให้ทั้งคู่กลับ lobby สถานการณ์นี้จะไม่เกิด — **ยังไม่ได้หาสาเหตุในโค้ด**: `DialogueVoteManager.RPC_SubmitReady` เทียบกับ `Runner.ActivePlayers.Count()` ซึ่งน่าจะเหลือ 1 แล้ว จึงน่าจะมีตัวบล็อกอื่น (ต้องดูถ้าจะให้ host เล่นต่อคนเดียวได้)
- **แก้แล้ว: `fix/c1-lobby-return` `6d6ae65a`** (รอเทส): client หลุดแล้ว host ถูกพากลับ lobby ด้วย จึงไม่เหลือ host เล่นคนเดียว — ตัวบล็อกการขยับตอน host อยู่คนเดียวยังไม่ได้หาสาเหตุ (ไม่จำเป็นถ้าไม่ให้เล่นคนเดียว)

**H-1 — HUD Canvas ไม่ถูกลบตอนกลับ lobby** — `GameManager.cs:314` ลบ object ชื่อ `"PlayerInterfaceCanvas"` แต่ canvas จริงถูกตั้งชื่อ `"Canvas"` (`CenterHost.cs:60`) และเป็น `DontDestroyOnLoad` (`:61`) → HUD ในเกมค้างทับหน้า lobby และถูกใช้ซ้ำรอบถัดไป (ต้องยืนยันตอนรัน; จะเห็นผลหลังแก้ C-1) — **ไม่พบในเทส 2026-10-16** (T2: Quit แล้ว lobby ไม่มี UI ซ้อน) → เฝ้าดูหลังแก้ C-1 เส้นทางอื่น
- **2026-10-16 ไม่แก้ (เปลี่ยนจากแผน):** canvas `"Canvas"` ถูกใช้ซ้ำข้ามรอบโดยออกแบบ — `CenterHost.SetupGameCore` หาตัวเดิมด้วยชื่อ, `DialogueHUB` / `PlayerInterface` หา `"Canvas"` ใหม่ทุกครั้งที่โหลด scene และ `CenterHost` สร้าง canvas ใหม่*หลัง*เหตุการณ์โหลด scene → ถ้าลบตอนกลับ lobby เสี่ยงให้ UI รอบถัดไปหาไม่เจอ ขณะที่เทส T2 ไม่เห็น HUD ซ้อนใน lobby; บรรทัด `DestroyByName("PlayerInterfaceCanvas")` ใน `ReturnToSession` ไม่มีผล (ไม่มี object ชื่อนี้)

**H-2 — `DialogueManager.IsDialogueOpen` เป็น static ไม่ถูก reset** (`DialogueManager.cs:11, 31, 40`) — ถ้าออกจากเกม/หลุดระหว่าง dialogue เปิดอยู่ ค่าค้างเป็น `true` → รอบถัดไป `OnInput` (`INetworkStructure.cs:58-60`) ส่ง input ว่างตลอด = ขยับไม่ได้จนกว่าจะปิดเกม — **ยังไม่ได้เทสตามเงื่อนไข** (T4-4b ที่เริ่มเกมใหม่ถูกข้าม)
- **แก้แล้ว: `fix/c1-lobby-return` `a92d5117`** (รอเทส): `GameManager.ReturnToSession` เรียก `DialogueManager.ResetForNewSession()` และ `TutorialUIManager.HideTutorial()` ทุกเส้นทางออก (`HideTutorial` เปลี่ยนไปอ่าน `IsMenuOpenSafe` เพราะ `MenuController` อาจ despawn แล้ว)

**H-3 — กุญแจ `Item_Key` / `Item_GoldKey` เก็บไม่ได้** (S1, S2) — `StoneKeyScript.cs:10-20` ทำงานเมื่อ `isOrangeStone` หรือ `isBlueStone` เป็นจริง แต่ prefab ตั้งเป็น 0 ทั้งคู่ → ขึ้นปุ่ม E แต่กดแล้วไม่เกิดอะไร และไม่ despawn

**H-4 — Interact บางชนิดยิงซ้ำ 2 ครั้งเมื่อฝั่ง client เป็นคนกด** — `HandleInteraction()` (`MovementCharacter.cs:718-759`) รันทั้งบน host (จำลอง input ของ client) และบน client (prediction) object ที่เขียนแบบ "host ทำเอง / client ส่ง RPC" จึงโดน 2 รอบ:
- ใช้อยู่ในด่าน: `FStoneItem.Collect()` `:9-16` (นับหิน +2), `StoneItem.Collect()` `:9-16` และ `StoneKeyScript` (quest progress +2), `NetworkInteractableWater.Splash()` (น้ำกระเซ็น 2 ครั้ง), `Duck_Moveset.PickupFriend/DropFriend` → `RPC_UpdateCarry` ถูกส่ง 2 ครั้ง (ครั้งที่สองมาช้ากว่า อาจดึงนกกลับตำแหน่งเดิมหลังโยน)
- ยังไม่ถูกใช้ในด่าน แต่จะพังทันทีถ้านำมาใช้: `CoopSwitch.cs:12-28`, `B_Interact_Door.cs:29-45` (toggle 2 ครั้ง = ไม่เปลี่ยน)
- ตัวที่เขียนถูกแล้ว: `BreakableRock.Interact()` `:56`, `ToggleButton_ReversPresure.Interact()` `:28`, `Note.Interact()` `:31`
- **ไม่พบในเทส 2026-10-16** (T1: นกไม่ดีด/ไม่โยนซ้ำ, น้ำ/กุญแจปกติ — ตัดสินจากภาพ ไม่มี log นับ RPC) → ลดเป็นเฝ้าดู; ตัวที่ยังไม่ถูกใช้ (`CoopSwitch`, `B_Interact_Door`) ยังควรเขียนให้ถูกก่อนนำมาใช้

**H-5 — WebGL build ใช้ Host mode ไม่ได้** — `NetworkProjectConfig.fusion`: `AllowClientServerModesInWebGL: false` แต่ CI build + deploy เป็น WebGL ทุกครั้งที่ push `main` → build บน gh-pages สร้าง/เข้าห้องไม่ได้ (ถ้าเป้าหมายคือ PC อย่างเดียว ให้ถือเป็นเรื่อง CI ไม่ใช่เรื่องเกม)

**H-6 — Cutscene 501 MB** — `Assets/StreamingAssets/Stage1-S1/Cutscene1.mp4` ใหญ่เกินขีดจำกัด 100 MB ต่อไฟล์ของ GitHub Pages และกิน LFS quota; บน PC ทำให้ขนาด build เพิ่ม ~0.5 GB

### Medium
**M-1 — State ที่ไม่ networked ถูกแก้ใน `FixedUpdateNetwork`** — `isMoveAble`, `isOptional`, `isSpeedoptional`, `optionalGravity`, `resetAnimation`, `_isEPressed`, `_wasEscPressed`, `_wasDropPressed`, `wasMenuOpenLastTick`, `currentPlatform` (`MovementCharacter.cs`), `_wasJumpPressed`, `_wasisThrowItemPressed` (`Bird_Moveset.cs:30-32`), `_wasEPressed`, `_wasFPressed` (`Duck_Moveset.cs:54-56`) → ไม่ถูก rollback ตอน client resimulate เสี่ยง edge ของปุ่มซ้ำ/หาย และ mispredict
- **แก้แล้ว: `fix/net-redesign` `aa56741b` (NR-4, ปุ่ม), `7dbde7c2` (NR-5, isMoveAble/isOptional/isSpeedoptional/optionalGravity/wasMenuOpenLastTick/lastPlatformPos)** (2026-10-16, รอเทส) — ที่ยังเหลือ: `currentPlatform` (หาใหม่ทุก tick จาก raycast), `localIsBeingCarriedPredict` (ตั้งใจให้เป็น prediction ฝั่ง client)

**M-2 — GameBootstrapper spawn singleton ซ้ำทุกครั้งที่เปลี่ยนด่าน** — `GameBootstrapper.cs:26, 34` spawn `CenterHost` / `GameManager` ทุก `OnSceneLoadDone` ขณะที่ตัวเก่ายังอยู่ (DontDestroyOnLoad) → ตัวใหม่ทำลายตัวเองใน `SingletonNetwork.Awake()` (`:34`) ด้วย `Destroy(gameObject)` ไม่ใช่ `Runner.Despawn` → อาจมี error จาก Fusion ตอนเปลี่ยนด่าน (ต้องยืนยันตอนรัน) — **ไม่พบในเทส 2026-10-16** (T5: ไม่เห็น error, HUD ครบ) → เฝ้าดู

**M-3 — `FStoneItem.Collect()` เรียก `Runner.Despawn` โดยไม่เช็ค authority** (`FStoneItem.cs:14`) — ฝั่ง client จะเรียกด้วย

**M-4 — สถานะปลดสกิลค้างข้ามรอบเล่น** — `PlayerInterface._birdFlyUnlocked` ฯลฯ เป็น static (`:64-67`) ถูก reset เฉพาะตอน Quit ผ่านเมนู (`MenuController.cs:275-276`) แต่ไม่ reset เมื่อจบ S3 หรือหลุด → เริ่มเกมใหม่โดยไม่ปิดโปรแกรม ตัวละครจะมีสกิลครบตั้งแต่ต้น (`MovementCharacter.Spawned()` `:262-271` อ่านค่า static) — **เทส 2026-10-16:** ไม่พบผ่านเส้นทาง Quit (T3); เส้นทางหลุด/จบ S3 ยังไม่ได้เทส (4b, 6b ข้าม — ทั้งสองเส้นทางติด C-1 อยู่)
- **แก้แล้ว: `fix/c1-lobby-return` `a92d5117`** (รอเทส): `GameManager.ReturnToSession` ตั้ง static สกิลทั้ง 4 ตัวของ `PlayerInterface` เป็น false ทุกเส้นทางออก (Quit ยังเรียก `ResetAllSkillUnlocks` เหมือนเดิม)

**M-5 — S1 มีหินรูปทรง 4 ก้อน (`FStoneItem`) แต่ไม่มี `StoneInventory`** — `StoneInventory` อยู่เฉพาะ S3 → ใน S1 กด E แล้วไม่เกิดอะไร (`FStoneItem.cs:11`) น่าจะเป็นของเหลือจากการ copy

**M-6 — เสียงโดนตีได้ยินเฉพาะตัวละครของ host** — `RPC_TakeDamage` รันบน State Authority เท่านั้น แต่เช็ค `HasInputAuthority` ก่อนเล่นเสียง (`MovementCharacter.cs:771-778`) → ผู้เล่นฝั่ง client ไม่ได้ยินเสียงตัวเองโดนตี

**M-7 — เรียก animation state ที่ไม่มี + log ทุก tick** — `PlayAnimationNetworked()` (`CharacterAnimation.cs:132-146`) `Debug.Log("can't find ...")` เมื่อไม่มี state: `"Interact"` (Duck มีแต่ `interact`, Bird ไม่มี), `"Falling_carry"` / `"Floating_carry"` (`:237`) → เป็ดตกขณะแบกเพื่อนจะ log ทุก tick (60 ครั้ง/วิ)
- **แก้แล้ว: `fix/net-redesign` `c6c83cce` (NR-1)** (รอเทส): ท่าทางเลือกจาก state ที่ sync ใน `CharacterAnimation.Render()` — ไม่เรียกชื่อ state ที่ไม่มีอีก; `Interact` ของเป็ดเล่น `interact`; เป็ดแบกนกกลางอากาศใช้ `CarryMovementTree` (FB-8)

**M-8 — เสียงไม่ gate ด้วย `Runner.IsForward`** — `MovementCharacter.cs:604, 709, 1330`, `Bird_Moveset.cs:546` → client อาจได้ยินซ้อน

**M-9 — `SessionHub.OnDestroy()` ซ่อน `SingletonNetwork.OnDestroy()`** (`SessionHub.cs:230`, warning CS0114) → `instance` ไม่ถูกเคลียร์ และ method นี้ล็อก/ซ่อน cursor (`:237-238`) ทุกครั้งที่ SessionHub ถูกทำลาย รวมถึงตอนกำลังกลับเข้า lobby (ต้องยืนยันลำดับตอนรัน)

**M-10 — client ส่ง RPC ที่ไม่มีสิทธิ์ตอน prediction** — `StartThrowRelease()` เรียก `PlayThrowAnimation_RPC` (`RpcSources.StateAuthority`, `Bird_Moveset.cs:508-526`) จาก FUN ฝั่ง Input Authority ด้วย; `RPC_OnRespawned` / `OnDroppedEvent` เขียน `[Networked]` บน proxy (`Bird_Moveset.cs:253-270`)

### Low
- **L-1** `Singleton<T>.Awake()` เงื่อนไขกลับด้าน (`Singleton.cs:27` — instance แรกทำลายตัวเอง) กระทบเฉพาะ class ที่ไม่ได้ใช้ (`G_SceneManager`, `SceneLoadTransition`, `PlayerData`)
- **L-2** `KeyScript` สืบทอดจาก `NetworkObject` (`KeyScript.cs:4`) แทน `NetworkBehaviour` และไม่ despawn หลังเก็บ — ยังไม่ถูกใช้
- **L-3** `OffScreenIndicator` เป็น `NetworkBehaviour` + มี `NetworkObject` แต่ถูกสร้างด้วย `Instantiate` ผ่าน `Canvas.prefab` → `Spawned()` ไม่เคยถูกเรียก (ทำงานได้เพราะ logic อยู่ใน `Update/LateUpdate`)
- **L-4** `OnTriggerEnter2D/OnCollision*` หลายตัวเช็ค `HasStateAuthority` โดยไม่เช็ค `Object.IsValid` (เช่น `CheckpointTrigger.cs:20`, `WarpZone.cs:19`, `CoopStepButton.cs:20`, `DoDamage.cs:20`) — เสี่ยง exception ถ้า trigger ทำงานก่อน object ถูก attach
- **L-5** `GameManager.CheckMapLoading()` / `CheckGameStart()` hard-code จำนวนผู้เล่น = 2 (`:257, 266`) → ทดสอบคนเดียวไม่ได้ (รอ timeout 15 วิ)
- **L-6** `SessionManager.JoinSession()` คืน `true` เสมอ (`:205-210`); `Setup()` มี try/catch ที่ทั้งสองทางเรียก method เดียวกัน (`:73-81`)
- **L-7** `SnowBallProjectile.FixedUpdateNetwork()` เรียก `FindObjectsByType<MovementCharacter>` ทุก tick ต่อกระสุน (`:86`)
- **L-8** `LifeCycleObj` ใน `Player(Duck).prefab` ชี้ controller ของ Bird (ถูกแทนตอน runtime ใน `LifeCycle.Initialize`)
- **L-9** `GameSessionData.asset` / `GameSettings.asset` เป็น ScriptableObject ที่ถูกเขียนตอน runtime → เล่นใน Editor แล้วไฟล์ asset เปลี่ยน (git dirty)
- **L-10** `Loading Scene.mp4` commit เป็นไฟล์ดิบทั้งที่ `.gitattributes` กำหนด LFS
- **L-11** `.github/copilot-instructions.md` ล้าสมัย (อ้าง `SampleScene`, `Environment-Test`, `G_SceneManager`)
- **L-12** เสียงกับดัก/checkpoint/ตาชั่ง/ป้อมยิงเล่นผ่าน `AudioSource.PlayOneShot` ไม่ผ่านค่า SFX volume

---

## 5. งานที่ต้องทำเพื่อปิดเกม (ปรับตามการตัดสินใจ 2026-10-02)

ขนาด: S = ไม่เกินครึ่งวัน, M = 1–2 วัน, L = 3 วันขึ้นไป

**อัปเดต 2026-10-03:** เพิ่มข้อ 4a, 4b, 7a, 7b และรวม FB-8 เข้าข้อ 7
**อัปเดต 2026-10-16 — เรียงใหม่ตามผล T1–T7 (หัวข้อ 7.5):** C-1 (b), (c), (d) ยืนยันแล้ว + H-7 ใหม่ → ข้อ 2 เป็นงานแรก; H-4 และ M-2 ไม่พบในเทส → ย้ายลง Nice-to-have เป็น "เฝ้าดู"; แยก M-3 ออกจากข้อ 3 — **เลข # คงเดิม** (หัวข้ออื่นอ้างถึงอยู่) แต่ย้ายแถวตามลำดับความสำคัญ

### Must-have ก่อนส่ง
| # | งาน | ไฟล์ | ขนาด | Dependency |
|---|---|---|---|---|
| 1 | ✅ **เสร็จ 2026-10-16** — ทดสอบ T1–T7 ยืนยันบั๊ก (ผลหัวข้อ 7.5; 4b, 6b ข้าม) | — | S | — |
| 2 | **แก้แล้ว รอเทส: `fix/c1-lobby-return` `6d6ae65a`, `a92d5117` (หัวข้อ 11)** — แก้ flow กลับ lobby ที่ยืนยันแล้ว (C-1): **(b)** เข้าประตูสุดท้าย S3 แล้วค้าง, **(c)** client หลุด → ให้ทั้งคู่กลับ lobby ตามเจตนาใน `OnPlayerLeft` (ปิด **H-7** ไปด้วย), **(d)** host หลุด → client กลับ lobby — สำคัญสุดเพราะ "จบเกม = กลับ lobby" คือฉากจบที่เลือกไว้; หลังแก้ให้เทส M-4 (4b, 6b) และ H-1/H-2 ซ้ำในเส้นทางเหล่านี้ — (a) Quit ผ่านเมนูทำงานแล้ว ห้ามทำให้พัง — **ยังไม่แก้จนกว่าเจ้าของโปรเจกต์สั่ง** | `INetworkStructure.cs`, `GameManager.cs`, `SessionHub.cs` (+ `DialogueManager.cs`, `PlayerInterface.cs` ถ้าเทสหลังแก้พบ H-2 / M-4) | M | — |
| 4a | **แก้แล้ว รอเทส: `fix/feedback-1018` `713f4f96` (หัวข้อ 15)** — **FB-7** ของสำคัญใน S3 ถูก tilemap `Tree-Tiles` บัง — ย้าย `Scale_Door`, `platfrom_1` ×8 และหนาม 4 ชุดขึ้น layer เดียวกับของประเภทเดียวกัน (ไม่แตะ `Tree-Tiles`) | `Stage1-S3.unity` | S | แก้ scene: ผ่าน Unity MCP หรือทำเองใน Editor |
| 4 | ซ่อน/เอาของที่กดแล้วไม่เกิดอะไรออกจากด่าน: `Item_Key`, `Item_GoldKey` (S1, S2) และหินรูปทรง 4 ก้อนใน S1 (H-3, M-5) — เก็บ prefab ไว้สำหรับ Key Extra ในอนาคต | `Stage1-S1.unity`, `Stage1-S2.unity` | S | — |
| 4b | เทสรวม `integration/feedback-jitter` ตาม checklist หัวข้อ 10 แล้ว merge เข้า `branch-UpdateSession` (รวม NAV, FB-1, FB-2, FB-4, FB-5, NJ-2 ถึง NJ-6) | — | S | — |
| 5 | Merge `branch-UpdateSession` → `dev` → `main` | — | S | ข้อ 2, 4, 4a, 4b ผ่าน |
| 6 | ตั้งชื่อ product เป็น Easter Bond + version แล้ว build PC (Windows) ทดสอบ | `ProjectSettings.asset` | S | ข้อ 5 |

### Should-have
| # | งาน | ไฟล์ | ขนาด | Dependency |
|---|---|---|---|---|
| 7 | **แก้แล้ว รอเทส: `fix/net-redesign` `c6c83cce` (NR-1)** — แก้ animation state ที่หาย/ชื่อไม่ตรง (M-7) รวม **FB-8** เป็ดแบกนกแล้วไม่เป็นท่ายก — ก่อนแก้ให้ทดสอบสมมติฐานตามหัวข้อ 10 (กรณี S1-FB8 a/b/c): `Duck_Controller` ไม่มี `Falling_carry`, `Floating_carry` และมี `interact` (ตัวเล็ก) แต่โค้ดเรียก `Interact` — ส่วน "เอา log ทุก tick ออก" แก้แล้ว: `fix/network-jitter` `36588980` (NJ-2) | `CharacterAnimation.cs`, `Duck_Controller.controller`, `Bird_Controller.controller` | S | ผลเทส FB-8 |
| 7a | **แก้แล้ว รอเทส: `fix/net-redesign` `004b52af` (NR-3)** — **NJ-7** ให้ client จำลองเครื่องพ่นลมและวัตถุที่ผู้เล่นยืน/ถูกดัน (`Runner.SetIsSimulated(Object, true)` ใน `Spawned()`) — ลดอาการตัวเองสั่นฝั่ง client ในลม/บนแพลตฟอร์ม | `TrapPressure.cs`, `MovingPlateform.cs` | M | ต้องเทสทุก trap/แพลตฟอร์ม, หลังข้อ 4b |
| 7b | **แก้แล้ว รอเทส: `fix/net-redesign` `9d477aa4` (NR-6), `62c3695a` (NR-8)** — **NJ-8** ให้ client ทำนายการเริ่มบิน + sync การปิด collider ของ `Enemy_KnightJay` ตอนพุ่ง | `Bird_Moveset.cs`, `Enemy_KnightJay.cs` | M | หลังข้อ 4b |
| 8 | ใส่ SFX ที่ขาด + แก้เสียงโดนตีฝั่ง client (M-6) + gate `IsForward` (M-8) | `Player(Bird/Duck).prefab`, `MovementCharacter.cs`, `Bird_Moveset.cs` | S–M | ไฟล์เสียง |
| 3b | `FStoneItem.Collect()` เรียก `Runner.Despawn` โดยไม่เช็ค authority (M-3) — แยกจากข้อ 3 เดิม | `FStoneItem.cs` | S | — |
| 10 | ข้อความแจ้งเมื่ออีกฝั่งหลุด | `INetworkStructure.cs`, `SessionHub.cs` | S | ข้อ 2 |
| 11 | อัปเดต GDD ให้ตรงเกมตามตารางการตัดสินใจ + เพิ่มปุ่ม Q / G / Tab / Gamepad และกับดักที่อยู่นอก GDD | เอกสาร | S | — |
| 12 | Quest UI / ตัวนับกุญแจ: เปิดใช้หรือเอาออก (ยังไม่ได้ตัดสินใจ) | `TriggerDialogue.cs`, `PlayerInterface.cs`, `GameManager.cs` | S–M | รอตัดสินใจ |

### Nice-to-have
| # | งาน | ไฟล์ | ขนาด | Dependency |
|---|---|---|---|---|
| 13 | WebGL (ทดลอง): เปิด `AllowClientServerModesInWebGL`, ลดขนาด/ย้าย cutscene 501 MB (H-5, H-6) — หลัง merge เข้า `main` job WebGL ใน CI อาจ fail จนกว่าจะแก้ ไม่กระทบ build PC | `NetworkProjectConfig.fusion`, `.github/workflows/build.yml`, `Cutscene1.mp4` | S–M | ข้อ 5 |
| 14 | Esc ปิดเมนูได้ / ตัวเลข "1/2 Players" (D16, D17) | `MenuController.cs`, `SessionHub.cs` | S | รอตัดสินใจ |
| 15 | Pixel Perfect Camera | player prefab ทั้ง 2 | S–M | ต้องเทสภาพ |
| 16 | **แก้แล้ว รอเทส: `fix/net-redesign` `aa56741b` (NR-4), `7dbde7c2` (NR-5)** — ย้าย edge-detect ของปุ่มไปใช้ `NetworkButtons` + networked previous state (M-1) | `INetworkStructure.cs`, movesets | L | เสี่ยง regression ทำหลังส่ง |
| 17 | Hit animation, animation ปีน | controller + โค้ด animation | M | ต้องมี art |
| 18 | ลบ dead code / asset (หัวข้อ 6) | — | S | หลัง merge |
| 3 | (ย้ายลงจาก Must-have 2026-10-16) **เฝ้าดู H-4** Interact ยิงซ้ำเมื่อ client กด — ไม่พบในเทส T1; ถ้าจะใช้ `CoopSwitch` / `B_Interact_Door` ต้องแก้ให้ทำบน host อย่างเดียวก่อน | `MovementCharacter.cs`, `Duck_Moveset.cs`, `StoneItem.cs`, `StoneKeyScript.cs` | M | ถ้าเห็นอาการซ้ำ |
| 9 | (ย้ายลงจาก Should-have 2026-10-16) **เฝ้าดู M-2** GameBootstrapper spawn singleton ซ้ำตอนเปลี่ยนด่าน — ไม่พบ error ในเทส T5 | `GameBootstrapper.cs`, `SingletonNetwork.cs` | S | ถ้าเห็น error ตอนเปลี่ยนด่าน |

### ตัดออกจาก scope แล้ว
ฉากจบเกม, หน้า "YOU ARE FAILED", ระบบ Key Extra + ห้องลับ (เลื่อนไปอนาคต), วิ่ง (Shift), Fall damage, บอส, preview ภาพรวมด่าน, การปรับพฤติกรรม Mira / มอนสเตอร์ / แพลตฟอร์มล่องหนให้ตรง GDD เดิม

---

## 6. Dead code / asset ที่ไม่ได้ใช้ (ลิสต์เท่านั้น ยังไม่ได้ลบ)

"ไม่ได้ใช้" = ไม่ถูกอ้างถึงจาก 4 scene ใน build ทั้งทางตรงและผ่าน prefab ซ้อน และไม่มีโค้ดเรียก

### Script ที่ไม่ถูกใช้
| ไฟล์ | เหตุผล |
|---|---|
| `Scripts/OutputManager.cs`, `Scripts/Player_Data/PlayerData.cs`, `Scripts/Environment/ObjScript/Platform/Platform_Dissapear.cs` | class ว่าง |
| `Scripts/Utility/G_SceneManager.cs`, `SceneLoadTransition.cs`, `Singleton.cs` | stub + ไม่มีผู้ใช้ |
| `Scripts/Utility/Character_Utility/CharacterInteract.cs` | interface ไม่มีผู้ implement |
| `Scripts/Monster/DashMonster.cs` | ไม่อยู่ใน prefab/scene ใด (ดู D12 ก่อนลบ) |
| `Scripts/Item/KeyScript.cs` | ไม่อยู่ใน prefab/scene ใด |
| `Scripts/Environment/ObjScript/NetworkObj/Interact/CoopSwitch.cs`, `Toggle_Shooter_Trap.cs`, `NetworkObj/Interact_Door.cs` | ไม่อยู่ใน prefab/scene ใด |
| `Scripts/Environment/Trap/Etc_/RemoveOnClient.cs`, `Scripts/Session/DoTween_UI/ButtonPopupAnimation.cs` | ไม่อยู่ใน prefab/scene ใด |
| `B_Interact_Door.cs`, `StepOn_Pressure.cs`, `Toggle_Presure_Trap.cs`, `ToggleButton_ReversPresure.cs`, `SnowballShooterScript.cs` (`SnowballShooterTrap`), `DoDamgeClear.cs`, `MoveableRock.cs` + `MoveableObject.cs`, `ThrowAble.cs`, `NetworkDebugOverlay.cs` | อยู่แค่ใน prefab ที่ไม่ถูกใช้ในด่าน |

### Method / field ที่ไม่มีผู้เรียก
`GameManager.SpawnPlayersForNewScene()`, `UseKey()`, `gameOver`, `checkPoints`, `StartGlobalQuest()` / `ResetQuest()`; `MovementCharacter.RPC_ForceClientTeleport()`, `RPC_PlayRespawnSound()`; `PlayerInterface.UnlockBirdFly/Throw()`, `UnlockDuckDive/Smash()`; `CharacterStats.s_walkSpeed`, `s_maxStamina`; `CenterHost` ส่วน "ReTry Zone" (`:166-186`), `WaitForSecToSpawn`, `AddPlayerRef`, `CheckComponentPlayer`; `SessionManager.RemovePlayer()`; `CharacterAnimation.UpdateOnGroundTypeOnBird()`, `SetFlipToFalse()`, `BirdPrepareFallingAnimaion()`; `CustomTextGen.SkipEffect()`; `AudioManager.PlaySoundAtPosition()`
(`BearTrapScript.SetReturnTrigger` และ `BreakableRock.ChangeSprite` ถูกเรียกจาก Animation Event — **ไม่ใช่** dead code)

### Prefab ที่ไม่ถูกใช้
- `Resources/Prefabs/InteractAble_Prefabs/`: `Hold_Door`, `Stepon_Pressure`, `Stepon_Pressure_Reverse`, `Toggle_Pressure(Need to Change to Button)`, `Toggle_Pressure_Reverse(Need to Change to Button)`, `Toggle_ReversPressure` — ตรงกับ "สวิตช์ toggle / สวิตช์เหยียบค้าง" ที่ GDD ระบุว่าไม่ใช้
- `Resources/Prefabs/Traps_Prefabs/Snowball_Shooter.prefab` (รุ่นหมุนได้; รุ่นที่ใช้จริงคือ `Snowball_Moshoot`)
- `Resources/Prefabs/Door_Prefabs/DoorAcess_Kelp 100OP.prefab`
- `Resources/Prefabs/Interface/FriendTracker.prefab` (ซ้ำกับ `Character_Prefabs/FriendTracker.prefab`)
- `Resources/Prefabs/Dialogue/DialogueManager.prefab`, `Resources/NonScriptPrefabs/Network_DialogueVoteManager.prefab` (ตัวที่ใช้คือ `Dialogue/DialogueVote.prefab`)
- `Resources/Prefabs/Object_Prefabs/`: `RockTest`, `Tree-Bigtri`, `Tree-Bigwonglee`, `Tree-Wonglee`, `snowTree -Nobase`, `snowTree-Wonglee`
- `Resources/Prefabs/TestForceStart.prefab`, `Resources/Prefabs/TestNetwork/PingC.prefab`
- `Resources/Utility_ForTest/PlayerSpawn.prefab`, `Rock_Throw.prefab`
- `Art-Source/Tilesmap/Snow-Tiles.prefab`, `Stage-Tiles.prefab`, `Tree_Tile.prefab` (tile palette — ใช้ใน Editor อย่าลบถ้ายังแก้ด่าน)

### Scene / โฟลเดอร์
- `Assets/_Recovery/` (4 scene, 6 MB), `Assets/Scenes/Stage-Sample.unity`, `TestUIScene.unity`, `MadScene.unity`, `Showcase.unity`
- `Assets/Photon/FusionDemos/` (6 MB), `Assets/Photon/FusionMenu/` (2 MB), `Assets/Plugins/Demigiant/DOTweenPro Examples/`
- `Assets/Firebase/` (63 MB) + `ExternalDependencyManager/` + `google-services.json` + `StreamingAssets/google-services-desktop.json` — **ไม่มีโค้ดเรียก Firebase** (ตรวจก่อนลบว่าไม่ได้วางแผนจะใช้)
- `Assets/Resources/TestJsonData/`
- ไฟล์ `.csproj` / `.sln` / `.slnx` ที่ root จำนวนมาก (`birdMoveSets.csproj`, `d.csproj`, `NewAssembly.csproj`, `Project_EggGame.sln` ฯลฯ) — เป็นไฟล์ที่ Unity สร้าง หลายตัวมาจาก asmdef ที่ไม่มีแล้ว
- Branch ที่ merge แล้วและลบได้: `branch-DemoGame`, `branch-PreAssets`, `branch-Prototype`, `branch-InGameUI`, `branch-TestCharacterMechaics`, `branch-map-LevelDesign`, `branch-mechaics_boss&character`, `origin/temp-rollback`

---

## 7. ผลทดสอบยืนยัน (Must-have #1)

**สถานะ: ทดสอบแล้ว 2026-10-16** (4b, 6b ข้าม) — ผลในหัวข้อ 7.5: ยืนยัน C-1(b), C-1(c), C-1(d) และบั๊กใหม่ H-7; ไม่พบ C-1(a), H-1, H-4, M-2, M-4 (เส้นทาง Quit)

- Branch ทดสอบ: `test/verify` (แตกจาก `branch-UpdateSession` @ `17719e1a`) — มีแค่ log `[VERIFY]` และปุ่ม debug ชั่วคราว **ห้าม merge**
- ไฟล์ที่เพิ่ม: `Assets/Scripts/_Verify/VerifyDebug.cs`; ไฟล์เดิมที่เพิ่มบรรทัด log: `INetworkStructure.cs`, `GameManager.cs`, `MovementCharacter.cs`, `DialogueManager.cs`, `StoneInventory.cs`, `NetworkInteractableWater.cs`, `SingletonNetwork.cs`, `GameBootstrapper.cs` (ไม่เปลี่ยน logic; RPC 4 ตัวเพิ่ม parameter `RpcInfo` ท้ายสุดเพื่อ log ผู้ส่ง)

### 7.1 Multiplayer Play Mode — พร้อมใช้
- Package 1.6.3 ติดตั้งแล้ว และมี scenario `Assets/Settings/PlayMode/DuoGamePlay_Test.asset` (Main Editor + Player 2, ส่ง log ของ Player 2 เข้า Console หลัก)
- วิธีเปิด: dropdown ข้างปุ่ม Play → เลือก **DuoGamePlay_Test** → กด Play (ถ้าไม่เห็น: Window > Multiplayer > Play Mode Scenarios)
- ข้อจำกัด: Stop/Play ทำ domain reload ซึ่ง reset ค่า static ทั้งหมด → **T2 → T3 → T4 ต้องเล่นต่อกันโดยไม่กด Stop**

### 7.2 ปุ่ม debug (กดในหน้าต่างที่ต้องการให้เกิดผล)
| ปุ่ม | ทำอะไร |
|---|---|
| F6 | เขียน marker ลง log (`========== MARK n ==========`) — กดก่อนเริ่มแต่ละข้อ |
| F7 | (Host เท่านั้น) พาผู้เล่นทั้งคู่ไปข้าง Passel Key |
| F8 | (Host เท่านั้น) พาทั้งคู่เข้าประตู warp ของด่าน; ใน S3 จะเปิดประตูสุดท้ายให้ก่อน |
| F9 | บังคับกลับ lobby (ใช้เมื่อค้างในด่าน) |
| F10 | ตัดการเชื่อมต่อของหน้าต่างนั้น (จำลองฝั่งนั้นหลุด) |

### 7.3 การตั้งค่าทุกรอบ ("เริ่มเกม")
1. **Main Editor (Host):** Play → Create Room → เลือก **Mira** → Ready
2. **Player 2 (Client):** Play → Join Room → ใส่ code → เลือก **Kael** → Ready
3. Host กด Start → ทั้งคู่กด Skip cutscene → ทั้งคู่กด Tab ปิด dialogue ที่จุดเกิด
4. ใน Console พิมพ์ `[VERIFY]` ในช่องค้นหา ต้องเห็น `DebugKeys ready` จากทั้ง 2 ฝั่ง

(Client ต้องเป็น Kael เพราะการยกเพื่อนทำได้เฉพาะเป็ด และ H-4 ต้องให้ฝั่ง client เป็นคนกด)

### 7.4 Test script

**ลำดับการเล่นแบบย่อ** (รายละเอียดเต็มอยู่ในตารางด้านล่าง)
- **แนะนำ: เล่นบน `test/all` ตามสคริปต์หัวข้อ 10.0** (T1–T7 + ข้อเทสที่เหลือในรอบเดียว) — หรือเล่นเฉพาะ T1–T7 บน `test/verify` ก็ได้ (`git checkout <branch>` แล้วคลิกหน้าต่าง Unity ให้ compile เสร็จก่อนกด Play)
- กด **F6** ที่ Host ก่อนเริ่มทุกข้อ, "เริ่มเกม" = ขั้นตอน 7.3

| ข้อ | ทำอะไร | ดูอะไร |
|---|---|---|
| T1 (H-4) | เริ่มเกม → Mira ผ่าน x≈-13, Kael ไปบ่อแรก x≈-1.5 → **Client** E ยก, รอ 2 วิ, E โยน → Kael ลอยในบ่อ Space 1 ครั้ง แล้ว F 1 ครั้ง → Host F7 → **Client** E เก็บกุญแจ → Host F7 → **Host** E เก็บกุญแจ | นกดีดกลับ/ถูกโยนซ้ำ/กระตุกหรือไม่ |
| T2 (C-1a, H-1) | Esc → Quit → ถ้าค้าง F9 ทั้ง 2 หน้าต่าง → ลองกด Play / Create Room | กลับเมนูเองหรือค้าง; UI ในเกมซ้อนหน้าเมนูหรือไม่; ปุ่มกดติดหรือไม่ |
| — | **ห้ามกด Stop จนจบ T4** | |
| T3 (M-4) | เริ่มเกมใหม่ → Mira กระโดดแล้วกด Space กลางอากาศ | ไอคอน Fly/Dive ขึ้นตั้งแต่เกิดและใช้ได้เลยหรือไม่ |
| T4 (H-2, C-1c) | Mira ไป x≈-13 ให้ dialogue เปิด อย่าปิด → **F10 ในหน้าต่าง Player 2** → ถ้า Host ค้าง F9 ทั้งคู่ → เริ่มเกมใหม่ (ยังไม่กด Tab) → A/D ค้าง 5 วิ ทั้งคู่ → ถ้ามี dialogue ขึ้น Tab แล้วลองขยับอีก | Host ถูกพากลับเมนูหรือไม่; ขยับได้หรือไม่ทั้งที่ไม่มีกล่อง dialogue |
| — | จากนี้ Stop/Play ได้ | |
| T5 (M-2) | เริ่มเกม → Host F8 (ไป S2) → ปิด cutscene/dialogue → Host F8 (ไป S3) → เดิน/กระโดดแต่ละด่าน | error สีแดงตอนโหลดด่าน; HUD/ตัวละครครบทั้ง 2 ฝั่งหรือไม่ |
| T6 (C-1b) | อยู่ใน S3 → Host F8 → ถ้าค้าง F9 ทั้งคู่ | กลับเมนูหรือค้างหน้า loading |
| T7 (C-1d) | เริ่มเกม → S1 → **F10 ในหน้าต่าง Main Editor** → รอ 15 วิ | Player 2 กลับเมนูเองหรือค้าง |

ทุกข้อ: กด **F6** ที่ Host ก่อนเริ่ม เล่นจบแล้วแจ้งว่าข้อไหนเสร็จ + สิ่งที่เห็น

**T1 — H-4: Interact ยิงซ้ำเมื่อ client เป็นคนกด** (เริ่มเกมรอบที่ 1, ด่าน S1)
| ขั้น | ทำอะไร | ถ้าบั๊กจริง | ถ้าไม่บั๊ก | log ที่ดู |
|---|---|---|---|---|
| 1a | เดินขวาทั้งคู่ Mira ผ่าน x≈-13 (ปลด Fly), Kael ถึงบ่อน้ำแรก x≈-1.5 (ปลด Dive) กด Tab ปิด dialogue ทุกครั้ง | — (เตรียมสำหรับ T3/T4) | — | — |
| 1b | Client (Kael) ยืนชิด Mira กด **E** 1 ครั้ง (ยก) รอ 2 วิ กด **E** อีก 1 ครั้ง (โยน) | แต่ละครั้งที่กด มี `RPC_UpdateCarry` **2 บรรทัดต่อหน้าต่าง** (`source` ต่างกัน); นกดีดกลับมาที่หัวเป็ด / ถูกโยนซ้ำ / กระตุก | 1 บรรทัดต่อหน้าต่างต่อการกด นกลอยออกไปเรียบ | `RPC_UpdateCarry state=… source=… invokeLocal=…` |
| 1c | Client (Kael) ลงบ่อน้ำแรก ลอยนิ่ง กด **Space** 1 ครั้ง แล้วกด **F** ดำ 1 ครั้ง | ตรงจังหวะที่กด มี `RPC_TriggerSplash` 2 บรรทัดติดกัน (อันหนึ่ง `invokeLocal=True` อีกอัน `source` เป็น client) | 1 บรรทัดต่อการกด | `RPC_TriggerSplash … source=…` (log ตอนตัวละครเข้า/ออกน้ำมาจาก host อย่างเดียว ไม่นับ) |
| 1d | Host กด **F7** → Client (Kael) กด **E** เก็บกุญแจ 1 ครั้ง | ฝั่ง Host มี `RPC_RequestAddStone` **2 บรรทัด** และ `HandleInteraction INTERACT` ขึ้นทั้งฝั่ง Host (`stateAuth=True`) และ Client (`inputAuth=True`, อาจซ้ำหลายบรรทัดที่ `isForward=False`) | `RPC_RequestAddStone` 1 บรรทัด | `RPC_RequestAddStone`, `HandleInteraction INTERACT 'Item_Passelkey_…'` |
| 1e | (ตัวเทียบ) Host กด **F7** อีกครั้ง → **Host (Mira)** กด E เก็บกุญแจอีกดอก | — | ต้องได้ `RPC_RequestAddStone` 1 บรรทัดเสมอ | เดียวกับ 1d |

**T2 — C-1(a) + H-1: Quit ผ่านเมนู Esc**
| ขั้น | ทำอะไร | ถ้าบั๊กจริง | ถ้าไม่บั๊ก | log ที่ดู |
|---|---|---|---|---|
| 2a | ฝั่งไหนก็ได้กด Esc → Quit | ทั้งคู่ค้างอยู่ในด่าน (ตัวละคร/มอนหาย ฉากนิ่ง) ไม่กลับหน้าเมนู | ทั้ง 2 หน้าต่างกลับหน้าเมนูหลักเอง | บั๊ก: `OnShutdown … SessionManager.Instance null=False -> branch=LOBBY` และ**ไม่มี** `ReturnToSession called` / ไม่บั๊ก: `branch=IN-GAME` → `ReturnToSession called` → `sceneLoaded 'SessionScene'` |
| 2b | ถ้าค้าง: กด **F9** ทั้ง 2 หน้าต่าง แล้วดูหน้า lobby | **H-1:** แถบ HP / ไอคอนสกิล / รูปตัวละครของในเกมซ้อนทับหน้าเมนู | เห็นแต่ UI ของ lobby | `1s after sceneLoaded 'SessionScene' … rootCanvases:` — บั๊กถ้ามี `Canvas(DontDestroyOnLoad…)` ค้าง |
| 2c | ลองกดปุ่ม Play / Create Room | ปุ่มกดไม่ติด หรือ UI เพี้ยน | ใช้ได้ปกติ | — |

**ห้ามกด Stop** — ไป T3 ต่อ (ถ้า lobby ใช้ไม่ได้เลย ให้แจ้ง แล้ว Stop/Play ใหม่ ข้าม T3 ไป T4)

**T3 — M-4: สกิลค้างหลังออกด้วยเมนู Quit** (Play รอบเดิม)
| ขั้น | ทำอะไร | ถ้าบั๊กจริง | ถ้าไม่บั๊ก | log ที่ดู |
|---|---|---|---|---|
| 3a | "เริ่มเกม" ใหม่ แล้วดูมุมจอ + ลองใช้สกิลทันทีที่จุดเกิด (Mira: กระโดดแล้วกด Space ซ้ำกลางอากาศ) | ไอคอน Fly / Dive ขึ้นตั้งแต่เกิด และใช้ได้เลย | ไม่มีไอคอนสกิล บินไม่ได้ | ฝั่ง Host: `MovementCharacter.Spawned … birdFly=… duckDive=…` (บั๊ก = True) |

**T4 — H-2 + M-4 + C-1(c): client หลุดตอน dialogue เปิดอยู่** (เล่นต่อจาก T3)
| ขั้น | ทำอะไร | ถ้าบั๊กจริง | ถ้าไม่บั๊ก | log ที่ดู |
|---|---|---|---|---|
| 4a | Mira เดินไป x≈-13 ให้ dialogue ปลด Fly เปิดขึ้น **อย่าปิด** แล้วกด **F10 ในหน้าต่าง Player 2** | **C-1(c):** Host ยังอยู่ในด่าน ไม่ถูกพากลับ | ทั้งคู่กลับหน้าเมนูเอง | Host: `OnPlayerLeft … SessionManager.Instance null=False -> willShutdown=False` = บั๊ก |
| 4b | ถ้าค้าง: F9 ทั้ง 2 หน้าต่าง แล้ว "เริ่มเกม" ใหม่ (ยังไม่ต้องกด Tab ตอนเข้าด่าน) | **M-4:** ไอคอน Fly ขึ้นตั้งแต่เกิด | ไม่มีไอคอน | lobby: `sceneLoaded 'SessionScene' | IsDialogueOpen=True birdFly=True` = ค่าค้าง |
| 4c | หลัง cutscene กด A/D ค้างไว้ 5 วิ ทั้ง 2 ฝั่ง | **H-2:** ขยับไม่ได้ทั้งที่ไม่มีกล่อง dialogue บนจอ | ขยับได้ทันที | `OnInput BLOCKED dialogueBlocking=True` |
| 4d | ถ้ากล่อง dialogue ที่จุดเกิดขึ้นมาเอง ให้กด Tab ทั้งคู่ แล้วลองขยับอีกครั้ง | ยังขยับไม่ได้ | ขยับได้ = บั๊กหายเองเมื่อ dialogue แรกถูกปิด (บันทึกเป็น ⚠️) | `DialogueManager.CloseDialogue -> IsDialogueOpen=false` |

จากข้อนี้ไป Stop/Play ใหม่ได้ตามสะดวก

**T5 — M-2: เปลี่ยนด่าน S1 → S2 → S3**
| ขั้น | ทำอะไร | ถ้าบั๊กจริง | ถ้าไม่บั๊ก | log ที่ดู |
|---|---|---|---|---|
| 5a | "เริ่มเกม" → Host กด **F8** (ไป S2) → ปิด cutscene/dialogue → Host กด **F8** (ไป S3) ระหว่างนั้นเปิด filter Error ใน Console | มี error สีแดงตอนโหลดด่าน (เกี่ยวกับ NetworkObject / Despawn / MissingReference) และมีบรรทัด `SingletonNetwork<CenterHost> DUPLICATE …`, `SingletonNetwork<GameManager> DUPLICATE …` ทุกครั้งที่เปลี่ยนด่าน | ไม่มี `DUPLICATE` ไม่มี error | `GameBootstrapper start: CenterHost exists=True GameManager exists=True`, `DUPLICATE … -> Destroy(gameObject)` |
| 5b | ใน S2 และ S3 ลองเดิน/กระโดด ดู HUD ทั้ง 2 ฝั่ง | HUD หาย, ตัวละครไม่เกิด, ค้างหน้า loading | เล่นได้ปกติ | — |

**T6 — C-1(b): เข้าประตูสุดท้ายของ S3** (ต่อจาก T5)
| ขั้น | ทำอะไร | ถ้าบั๊กจริง | ถ้าไม่บั๊ก | log ที่ดู |
|---|---|---|---|---|
| 6a | อยู่ใน S3 → Host กด **F8** | ขึ้น loading แล้วค้าง / อยู่ใน S3 ต่อโดยไม่มีอะไรขยับ | ทั้งคู่กลับหน้าเมนู | ทั้ง 2 ฝั่ง: `BackToSessionScene called` → `OnShutdown … branch=…` |
| 6b | ถ้าค้าง: F9 ทั้ง 2 หน้าต่าง | — | — | lobby: ค่า `birdFly=… duckDive=…` (M-4 ผ่านเส้นทางจบเกม) |

**T7 — C-1(d): host หลุด**
| ขั้น | ทำอะไร | ถ้าบั๊กจริง | ถ้าไม่บั๊ก | log ที่ดู |
|---|---|---|---|---|
| 7a | "เริ่มเกม" → เข้า S1 แล้วกด **F10 ในหน้าต่าง Main Editor** รอ 15 วิ | Player 2 ค้างอยู่ในด่าน | Player 2 กลับหน้าเมนูเอง | Player 2: `OnDisconnectedFromServer … willShutdown=False` หรือ `OnShutdown … branch=LOBBY` = บั๊ก |

ข้อจำกัดของ T4/T7: F10 เป็นการปิดแบบปกติ ไม่ใช่โปรแกรมดับ — เส้นทาง timeout 10 วิของการหลุดจริงไม่ได้ทดสอบ

### 7.5 ผล (2026-10-16)
**ที่มา:** ฟอร์มผลเทสที่ผู้ทดสอบกรอก (ภาพหน้าจอที่เจ้าของโปรเจกต์ส่งมา 2026-10-16) — ผลตัดสินจากสิ่งที่ผู้ทดสอบเห็น **ไม่มี log `[VERIFY]` บนเครื่องนี้ให้ตรวจซ้ำ** (`Editor.log` ถูกเขียนใหม่ 10-13/10-14) ยกเว้นโน้ตที่คัด log มาใน T7; บริบทของ 4d ยืนยันเพิ่มโดยเจ้าของโปรเจกต์
✅ = ยืนยันว่าเป็นบั๊ก · ❌ = ไม่พบบั๊ก · ⚠️ = ยังไม่ได้เทสตามเงื่อนไข / ผลไม่ครบ

| รหัส | ข้อทดสอบ | ผล | หลักฐาน (log / โน้ตผู้ทดสอบ) | หมายเหตุ |
|---|---|---|---|---|
| H-4 | T1 1b–1e | ❌ | เลือก "ไม่บั๊ก" ทุกขั้น | ตัดสินจากภาพ (นกไม่ดีด/ไม่โยนซ้ำ) — ไม่มี log นับจำนวน RPC จึงยังตัดไม่ได้ 100% ว่า RPC ไม่ซ้ำ |
| C-1(a) | T2 2a | ❌ | เลือก "ไม่บั๊ก" | Quit ผ่านเมนู Esc กลับเมนูหลักได้ทั้ง 2 ฝั่ง |
| H-1 | T2 2b–2c | ❌ | เลือก "ไม่บั๊ก" | lobby ไม่มี UI ในเกมซ้อน, ปุ่มใช้ได้ |
| M-4 | T3 3a | ❌ (เฉพาะเส้นทาง Quit) | เลือก "ไม่บั๊ก" | 4b และ 6b (เส้นทาง client หลุด / จบ S3) **ข้าม** → ยังไม่ได้เทส |
| C-1(c) | T4 4a | ✅ | โน้ต "P2 หาย P1 ปกติ" (ผู้ทดสอบเลือก "ข้าม") | client หลุดแล้ว host ยังอยู่ในด่าน ขัดกับเจตนาในโค้ด `INetworkStructure.OnPlayerLeft` ("อีกคนออก → ออกทั้งคู่") และ 4d ยืนยันว่า host เล่นต่อไม่ได้ |
| H-2 | T4 4c | ⚠️ | เลือก "ไม่บั๊ก" + โน้ต "ก็ดูปกติ … เดินไปจนเจอ Dia ก็หยุดปกติ" | 4b (เริ่มเกมใหม่หลังหลุดตอน dialogue เปิด) ถูกข้าม → เงื่อนไขของ H-2 ยังไม่ได้เทสจริง |
| **H-7** | T4 4d | ✅ | เลือก "บั๊กจริง" | บริบท (เจ้าของโปรเจกต์ยืนยัน): host เล่นต่อคนเดียวหลัง client หลุด → เจอ dialogue กด Tab แล้วยังขยับไม่ได้ — ดูหัวข้อ 4 |
| M-2 | T5 5a–5b | ❌ | เลือก "ไม่บั๊ก" | ไม่มี error / `DUPLICATE` ที่เห็น, HUD และตัวละครครบใน S2, S3 |
| C-1(b) | T6 6a | ✅ | เลือก "บั๊กจริง" + โน้ต "ค้าง" | เข้าประตูสุดท้าย S3 แล้วค้าง ไม่กลับเมนู |
| C-1(d) | T7 7a | ✅ | เลือก "บั๊กจริง" + log `OnShutdown … branch=LOBBY` | ตรงกับกลไกที่วิเคราะห์ไว้ใน C-1 (`SessionManager.Instance != null` ตอนอยู่ในเกม) |

---

## 8. Feedback V0.902 เทียบกับ build 0.903 (ตรวจ 2026-10-03)

**ที่มาของข้อสรุป:** อ่านโค้ด + ไฟล์ scene/prefab + ประวัติ git — ไม่ได้เล่น build จริง
- Build `Egg Ver0.903` (`Desktop/Unity_Project/GameBuild_Test/2D/`) ถูก build 2026-06-28 08:53–09:06 ตรงกับ commit `4806544c`
- โค้ดปัจจุบัน (`branch-UpdateSession` @ `17719e1a`) ต่างจาก 903 แค่ commit เดียวที่แก้ flow ออกจากเกม → สถานะด้านล่างใช้ได้ทั้งกับ 903 และโค้ดปัจจุบัน
- Build 902 น่าจะอยู่ช่วง 06-27 02:33–22:27 (ช่วงที่ tilemap ด่าน 3 พัง ดู FB-6)

| รหัส | Feedback | สถานะใน 0.903 | หลักฐาน | แก้แล้ว |
|---|---|---|---|---|
| FB-1 | S1 พุ่มไม้สลายแปลก: เล่นท่าแตกเองโดยไม่มีอะไรไปโดน | ยังไม่แก้ | `CrumblingPlatform.Render()` สั่งเล่นท่าแตกพร้อมกับซ่อน sprite ในเฟรมเดียวกัน จึงไม่เห็นท่าแตกตอนพัง; ตอนโผล่กลับใช้ trigger `Reset` ที่มี transition 0.25 วิ จึงเห็นเฟรมแตกค้างก่อนกลับเป็นปกติ (commit 06-27 แก้แค่ collider) | `fix/feedback-v0902` `41dd0489` |
| FB-2 | เสียงประกอบหาย เกมเงียบ | BGM แก้แล้ว / SFX ยังไม่แก้ | BGM: `118cb268` (06-28 08:52) เพิ่ม `PlayBGM` ตอนโหลดด่าน. SFX: `AudioListener` อยู่บน `UI_Camera` ที่วางนิ่งที่ ~(0, -3) ทุกด่าน ไม่ตามผู้เล่น แต่ SFX เป็นเสียง 3D ได้ยินไม่เกิน 20 หน่วย (`AudioManager.cs:93-95`) — จุดเกิด S1 อยู่ที่ x ≈ -29 | `fix/feedback-v0902` `0bb743b8` |
| FB-3 | เลเยอร์ทับผู้เล่นทั้ง 3 ด่าน | น่าจะแก้แล้ว (เจ้าของโปรเจกต์ยืนยัน) | `118cb268` ย้าย `BreakAbleRock` ใน S1 ไป DecorationLayer. ลำดับปัจจุบัน: ผู้เล่น (PlayerLayer) อยู่หลัง PlatformLayer, SortingTexture, GroundLayer | — |
| FB-4 | ฟองน้ำนก: ขึ้น 8 ช่องแต่เต็มแค่ 6 | ยังไม่แก้ | `Player(Bird).prefab` มี `oxygenBubbles` 8 ช่อง แต่ `drowningTime = 6` | `fix/feedback-v0902` `6c4a49d1` |
| FB-5 | S2 เครื่องพ่นดันผู้เล่นทะลุแมพที่มุมดิน | แก้เฉพาะจุด ต้นเหตุยังอยู่ | `4806544c` ขยับ `Trap_Mix (1)` ใน S2. ต้นเหตุ: `Rigidbody2D` ของผู้เล่นเป็น Collision Detection แบบ Discrete และ `CompositeCollider2D` ของพื้นเป็น Outlines (กลวง) ทั้ง 3 ด่าน | `fix/feedback-v0902` `85cce42a` |
| FB-6 | S3 ท้ายแมพหมุนผิดด้านทั้งแมพ | แก้แล้ว | `3b842843` (06-27 02:33) ทำให้ tile ของ `Main-Ground` ทั้ง 708 ช่องเหลือทิศเดียว (เดิมมี 8 แบบ); `949c2b3b` "Map3" (06-27 22:27) ซ่อมกลับมามี 4 แบบ ก่อน build 903 | — |
| FB-7 | S3 ของสำคัญมองไม่เห็น (พื้น, ประตูตรงที่ชั่งน้ำหนัก) | น่าจะยังไม่แก้ — ยังไม่ได้เปิดดูภาพยืนยัน | `Scale_Door`, `G_PL`, `G_PR`, `platfrom_1` ×8, `WeighingScale` ใช้ sorting layer `Default` (หลังสุด) แต่ tilemap `Tree-Tiles` (BGLayer, tile ทึบ, วาดด้วยกล้องเดียวกัน) ทาอยู่ที่ตำแหน่งเดียวกัน → ถูกบัง ค่าเหล่านี้ไม่เปลี่ยนตั้งแต่ก่อน 902 — ยืนยันซ้ำในเทส 0.9 (2026-10-18) | **แก้แล้ว: `fix/feedback-1018` `713f4f96`** (หัวข้อ 15) |
| FB-8 | เป็ดยกเพื่อนบางทีไม่เป็นท่ายก | ยังไม่แก้ / ยังไม่รู้เงื่อนไข | เกี่ยวกับ M-7: โค้ดเรียก state `Falling_carry` / `Floating_carry` ที่ไม่มีใน `Duck_Controller` และ lock 0.5 วิ หลังกด E — ต้องรู้ว่าเกิดตอนไหน (กลางอากาศ / ในน้ำ / หลังยกทันที) | ยังไม่แก้ |
| NAV | (คำขอใหม่) กรอบ highlight ของเมนูขึ้นเฉพาะตอนเสียบจอย | — | `SelectionHighlightFollower.LateUpdate()` | `fix/feedback-v0902` `1c50ce09` |

### 8.1 สิ่งที่แก้ใน `fix/feedback-v0902` (แตกจาก `branch-UpdateSession`, ยังไม่ได้เทสตอนรัน)
Compile: ผ่าน 0 error (`Editor.log` 2026-10-03 00:13, ไม่มี warning ใหม่ในไฟล์ที่แก้) — พฤติกรรมตอนเล่นจริงยังไม่ได้ตรวจ ต้องเทสตามหัวข้อ 10
รวมแล้ว: `integration/feedback-jitter` merge commit `4894d443` (ยังไม่ merge เข้า `branch-UpdateSession`)
| รหัส | ไฟล์ : method | เปลี่ยนอะไร |
|---|---|---|
| NAV | `U_interface/SelectionHighlightFollower.cs` : `LateUpdate` | ถ้า `Gamepad.all.Count == 0` ไม่แสดงกรอบ (การเลื่อนเมนูด้วยคีย์บอร์ดยังใช้ได้) |
| FB-2 | `Character/CameraCharacter.cs` : `InitializeAsLocal`, `OnSceneChanged`, `LateUpdate` + method ใหม่ `ClaimAudioListener` | สร้าง `AudioListener` ติดกล้องของผู้เล่น local และปิด listener ตัวอื่นในฉาก (ทำซ้ำเมื่อเปลี่ยนด่าน) |
| FB-1 | `Environment/ObjScript/Platform/CrumblingPlatform.cs` : `Render` | เล่น `Platform_Break` ให้จบ (0.875 วิ) ก่อนซ่อน sprite; ตอนกลับมาใช้ `Play("Platform_Idle")` แทน trigger — ไม่แตะ logic network |
| FB-5 | `Character/MovementCharacter.cs` : `Awake` | `rb2D.collisionDetectionMode = Continuous` |
| FB-4 | `Character/Player_GUI/PlayerGUI.cs` : `StartOxygenTracking` | แสดงเฉพาะช่องฟองที่ไม่เกิน oxygen สูงสุดของตัวละคร |

### 8.2 Checklist เทส
ย้ายไปรวมกับ 9.2 ที่ **หัวข้อ 10** แล้ว (ข้อเช็คเดิมครบ + ข้อเพิ่มของ FB-1, FB-2, FB-8)

### 8.3 ยังไม่ได้แก้ (ต้องแก้ scene — รอสั่ง)
- ~~**FB-7:** ปรับ sorting ของ `Tree-Tiles`~~ — **แก้แล้วอีกวิธี: `fix/feedback-1018` `713f4f96`** ย้ายของที่ถูกบังขึ้น layer ที่ถูก แทนการย้าย `Tree-Tiles` (ดูหัวข้อ 15)
- **FB-5 (เสริม ถ้ายังทะลุ):** `Main-Ground` → Composite Collider 2D → Geometry Type = `Polygons` ทั้ง 3 ด่าน
- **FB-8:** มีสมมติฐานจากโค้ดแล้ว (ดูหัวข้อ 5 ข้อ 7) — รอผลเทสกรณี S1-FB8 a/b/c ในหัวข้อ 10

---

## 9. Jitter จาก network lag / เครื่อง lag (ตรวจ 2026-10-03)

**อาการที่เจ้าของโปรเจกต์ยืนยัน:** (ก) ตัวเราเองฝั่ง client สั่น (ข) ทั้งจอสั่นตอน FPS ตก

**ที่มาของข้อสรุป:** อ่านโค้ด + config + prefab + log ของ build 0.902 (`%USERPROFILE%\AppData\LocalLow\MadMakeGame\Project_EggGame\Player.log`, เล่นวันที่ 2026-06-27) — **ไม่ได้ profile และไม่ได้เล่นซ้ำ** ข้อที่พูดถึงภาระเครื่องเป็นการให้เหตุผลจากโค้ด ไม่ใช่ค่าที่วัดได้

**การตั้งค่าที่เกี่ยวข้อง:** tick 60, Host/Client; `RunnerSimulatePhysics2D` บน `NetworkRunner.prefab` = Physics Authority `Fusion`, Timing `FixedUpdateNetwork`, Client Physics Simulation `SimulateAlways`

| รหัส | สิ่งที่เจอ | กระทบอาการ | หลักฐาน | แก้แล้ว |
|---|---|---|---|---|
| NJ-1 | Client จำลอง physics ทั้งฉากซ้ำทุก tick ที่ย้อนคำนวณ — ping ยิ่งสูงยิ่งจำลองหลายรอบต่อเฟรม เครื่องที่ช้าอยู่แล้วจะช้าลงอีก | (ข) | `NetworkRunner.prefab` → `ClientPhysicsSimulation: 3` | ไม่แก้ — จำเป็นต่อการทำนายตัวละครที่เดินด้วย Rigidbody |
| NJ-2 | Log ยิงทุก tick: `can't find Falling_carry` 11,894 บรรทัดในการเล่น 0.902 รอบเดียว (และ `can't find Interact` 19 บรรทัด) | (ข) | `CharacterAnimation.PlayAnimationNetworked()`; `Player.log` | `fix/network-jitter` `36588980` |
| NJ-3 | FPS ไม่จำกัด: quality เริ่มต้น "Very Low" ปิด vSync และไม่มีโค้ดตั้ง `targetFrameRate` | (ข) | `QualitySettings.asset` (`m_CurrentQuality: 0`, `vSyncCount: 0`) | `fix/network-jitter` `f163f61c` |
| NJ-4 | จอง memory ทุก tick ใน `FixedUpdateNetwork` → GC กระตุก | (ข) | `MovementCharacter.CheckInClimbZone()` (`OverlapBoxAll` ทุก tick ต่อตัวละคร รวม tick ที่ย้อน); `TrapPressure.FixedUpdateNetwork()` (`new List` + `LayerMask.GetMask` ทุก tick, S2 มี 29 ตัว); `SnowBallProjectile.FixedUpdateNetwork()` (`FindObjectsByType` ทุก tick ต่อกระสุน) | `fix/network-jitter` `379f2539` (3 จุดนี้) — ที่เหลือดู 9.3 |
| NJ-5 | การเกลี่ยภาพใน `Render()` ใช้เวลาต่อ tick (`Runner.DeltaTime`) แทนเวลาต่อเฟรม → เครื่องที่ FPS ต่ำกว่า 60 เห็นวัตถุตามช้า | (ข) | `MoveSome.Render()`, `HingeDoor.Render()`, `RotatingPlatform.Render()`, `FadeWhenPlayerInside.Render()` (`FadingObj.cs`) | `fix/network-jitter` `f4324cf8` |
| NJ-6 | มอน `Kinght_Jay` / `Puffy_Jay` เป็น `Rigidbody2D` แบบ Dynamic ไม่มี NetworkRigidbody และฝั่ง client ไม่มีโค้ดหยุดความเร็ว — ถ้าผู้เล่นฝั่ง client ชนตัวมอน มอนจะไหลในแกน X ค้าง (Linear Damping 0) สู้กับ `Render()` ที่ดึงกลับไป `netPos` ทุกเฟรม และ NJ-1 ทำให้ physics ขยับมอนหลายรอบต่อเฟรม | มอนสั่นฝั่ง client | prefab ทั้งสอง: `m_BodyType: 0`, `m_LinearDamping: 0`, `m_Constraints: 6` (ล็อกแกน Y + การหมุน), layer `Default` ซึ่งชนกับ `Player`; `Enemy_KnightJay.Render()`, `Enemy_PuffyJay.Render()` | `fix/network-jitter` `c3f05e3e` |
| NJ-7 | เครื่องพ่นลม (`TrapPressure`) และวัตถุที่มี NetworkRigidbody (แพลตฟอร์มเคลื่อนที่ ฯลฯ) ไม่ถูกจำลองบน client → client ไม่ได้ทำนายแรงลม/การเคลื่อนของแพลตฟอร์ม ตัวเราจึงถูก host ดึงกลับตลอดเวลาที่อยู่ในลมหรือบนแพลตฟอร์ม | (ก) | Fusion เตือน 21 ครั้งใน `Player.log`: "NetworkRigidbody … is NOT simulated on the local client … Runner.SetIsSimulated(Object, true)"; `TrapPressure.FixedUpdateNetwork()` มีเงื่อนไข `player.HasInputAuthority` รองรับไว้แต่ไม่ถูกเรียกบน client; มีแค่ `SnowBallProjectile` ที่เรียก `SetIsSimulated` | `fix/net-redesign` `004b52af` (NR-3) — รอเทส |
| NJ-8 | สิ่งที่ทำบน host อย่างเดียวและ client ไม่ได้ทำนาย → ตัวเราดีดเมื่อผลมาถึง: เริ่มบิน (`Bird_Moveset.StartFlying()` return ถ้าไม่ใช่ State Authority), แรงกระแทกตอนโดนตี, และ `Enemy_KnightJay` ปิด collider ตอนพุ่งเฉพาะบน host (`col.enabled = false` ใน `MonsterSpecificUpdate()`) ทำให้ client ชนตัวมอนที่กำลังพุ่งแต่ host ไม่ชน | (ก) | โค้ด | `fix/net-redesign` `9d477aa4` เริ่มบิน (NR-6), `62c3695a` collider มอนพุ่ง (NR-8) — แรงกระแทกตอนโดนตียังไม่แก้ (ดู 12.2) — รอเทส |
| NJ-9 | ตัวเพื่อนบน client เป็น kinematic (proxy) แต่บน host เป็น dynamic → เดินชน/ดันกันแล้วผลไม่ตรงกับ host | (ก) | `NetworkRigidbody` ของ Fusion Physics addon ตั้ง proxy เป็น kinematic | ยังไม่แก้ (เป็นพฤติกรรมของ addon) |
| NJ-10 | state ที่ไม่ใช่ `[Networked]` ถูกแก้ใน `FixedUpdateNetwork` (ข้อ M-1) → ไม่ถูกย้อนตอน resimulate; และไม่มีการเกลี่ยภาพตัวละครตอนถูกแก้ตำแหน่ง | (ก) | `MovementCharacter` (`isMoveAble`, `isOptional`, `_isEPressed` ฯลฯ) | `fix/net-redesign` `7dbde7c2` (NR-5) ส่วน state; ยังไม่มีการเกลี่ยภาพตอนถูกแก้ตำแหน่ง — รอเทส + `fix/lag-polish` `54cc1f94` เกลี่ยภาพตอนถูกแก้ตำแหน่ง (LP-2) — รอเทส |
| NJ-11 | มอนฝั่ง client ขยับด้วยการไล่ตาม `netPos` ค่าล่าสุดที่ได้รับ (ไม่ใช่ค่าที่ Fusion interpolate ให้) → ตอนมอนพุ่ง/เดินและ packet มาไม่สม่ำเสมอ จะเห็นขยับเป็นจังหวะ | มอนสั่นฝั่ง client (เฉพาะตอนมอนเคลื่อนที่) | `Enemy_KnightJay.Render()`, `Enemy_PuffyJay.Render()` | `fix/net-redesign` `dd9563a9` (NR-2) — รอเทส |

**เรื่อง "มอน check คนถี่ไป":** แก้ไปแล้วจริงก่อนหน้านี้ — `BaseMonster.PlayerRadar()` ใช้รายชื่อจาก trigger (`MonsterRadarRelay`) แทนการสแกนทุก tick (`926e4b73`, `24e9ee5d`, `d37e5779` วันที่ 06-26/27)

**แก้ไขจากแผนที่เสนอไว้:**
- NJ-6: ตอนเสนอแผนเขียนว่า "แรงโน้มถ่วงดึงมอนลง" — **ผิด** prefab ล็อกแกน Y ไว้ (`m_Constraints: 6`) สิ่งที่ขยับได้คือแกน X เมื่อถูกผู้เล่นชน ผลของการแก้ข้อนี้จึงจำกัดอยู่ที่กรณี client ชนตัวมอน ส่วนการขยับเป็นจังหวะตอนมอนเคลื่อนที่คือ NJ-11 ซึ่งยังไม่ได้แก้
- NJ-4 (`SnowBallProjectile`): แผนเดิมจะใช้ `GameManager.activePlayers` แต่ list นี้ถูก `Clear()` ใน `ResetLoadingStateForNextLevel()` ตอนเปลี่ยนด่าน และไม่มีโค้ดอื่นใช้ จึงยืนยันไม่ได้ว่ามีผู้เล่นครบทุกด่าน → เปลี่ยนเป็นหาผู้เล่นครั้งเดียวตอนกระสุน spawn แทน

### 9.1 สิ่งที่แก้ใน `fix/network-jitter` (แตกจาก `branch-UpdateSession`, ยังไม่ได้เทสตอนรัน)
Compile: ผ่าน 0 error (`Editor.log` + `MainGame.Core.dll` build 2026-10-03 00:37:53 หลังการแก้ครั้งสุดท้าย, ไม่มี warning ใหม่ในไฟล์ที่แก้) — พฤติกรรมตอนเล่นจริงยังไม่ได้ตรวจ ต้องเทสตามหัวข้อ 10 ไม่ได้แตะ `.unity` / `.prefab` / `NetworkProjectConfig.fusion`
รวมแล้ว: `integration/feedback-jitter` merge commit `80356b8d` (ยังไม่ merge เข้า `branch-UpdateSession`)
| รหัส | ไฟล์ : method | เปลี่ยนอะไร |
|---|---|---|
| NJ-6 | `Monster/BaseMonster.cs` : `Spawned` | ถ้าไม่ใช่ State Authority ตั้ง `rb2D` เป็น Kinematic และล้างความเร็ว (host เหมือนเดิม) |
| NJ-5 | `Environment/Trap/MoveSome.cs`, `Environment/ObjScript/HingeDoor.cs`, `RotatingPlatform.cs`, `FadingObj.cs` : `Render` | `Runner.DeltaTime` → `Time.deltaTime` (ที่ 60 FPS ผลเท่าเดิม) |
| NJ-2 | `Character/CharacterAnimation.cs` : `PlayAnimationNetworked` | เอา `Debug.Log` ออก ยัง return เมื่อไม่มี state เหมือนเดิม |
| NJ-3 | `Session/SessionManager.cs` : `Awake` | `Application.targetFrameRate = 60` (ไม่รวม WebGL ที่ browser คุม frame rate เอง) |
| NJ-4 | `Character/MovementCharacter.cs` : `CheckInClimbZone` | ใช้ `Runner.GetPhysicsScene2D().OverlapBox` กับ buffer 8 ช่องที่ใช้ซ้ำ |
| NJ-4 | `Environment/Trap/TrapPressure.cs` : `Awake`, `FixedUpdateNetwork` | ใช้ list ซ้ำ + เก็บ layer mask ไว้ตั้งแต่ `Awake` |
| NJ-4 | `Environment/Trap/Etc_/SnowBallProjectile.cs` : `Spawned`, `FixedUpdateNetwork` | หาผู้เล่นครั้งเดียวตอน spawn (เฉพาะ host) แทนทุก tick |

**ผลข้างเคียงที่ควรรู้:**
- NJ-3 + NJ-5: เครื่องที่เคยรัน FPS สูงกว่า 60 จะเห็นประตูบานพับ/แพลตฟอร์มหมุน/แมงกะพรุน (`MoveSome`) ตามค่าจริงช้าลงเล็กน้อย (เท่ากับที่เครื่อง 60 FPS เห็นมาตลอด) และจอ 144 Hz จะแสดงผลที่ 60 FPS
- NJ-2: log ที่หายไปเคยเป็นร่องรอยของ FB-8/M-7 — state ที่โค้ดเรียกแต่ไม่มีใน controller คือ `Falling_carry`, `Floating_carry`, `Interact`
- NJ-4 (`SnowBallProjectile`): ผู้เล่นที่เพิ่ง spawn หลังกระสุนลูกนั้นถูกยิงออกมาจะไม่โดนลูกนั้น (กระสุนอยู่ได้ 7 วิ)
- NJ-4 (`CheckInClimbZone`): ตรวจ collider ใน layer ปีนได้สูงสุด 8 ชิ้นที่ซ้อนกับตัวละครพร้อมกัน

### 9.2 Checklist เทส
ย้ายไปรวมกับ 8.2 ที่ **หัวข้อ 10** แล้ว (ข้อเช็คเดิมครบ)

### 9.3 ยังไม่ได้แก้ — เรียงตามผลต่ออาการ (ก) "ตัวเราเองสั่น"
1. **NJ-7:** เรียก `Runner.SetIsSimulated(Object, true)` ใน `Spawned()` ของ `TrapPressure` และวัตถุที่ผู้เล่นยืน/ถูกดัน (ตามที่ Fusion เตือน) — เสี่ยงกลาง ต้องเทสทุก trap/แพลตฟอร์ม
2. **NJ-8:** ให้ client ทำนายการเริ่มบิน; sync การปิด collider ของ `Enemy_KnightJay` ตอนพุ่งให้ client ด้วย
3. **NJ-10:** เกลี่ยภาพตัวละครเมื่อถูกแก้ตำแหน่งเล็กน้อย / ย้าย state สำคัญเป็น `[Networked]` (งานเดียวกับ M-1)
4. **NJ-11:** ให้มอนใช้ตำแหน่งที่ Fusion interpolate (เช่น `NetworkTransform`) แทนการไล่ตาม `netPos` — ต้องแก้ prefab
5. **NJ-4 ที่เหลือ:** `PressurePlateHinge` (`OverlapBoxAll` ทุก tick), `WeighingScale_Mech` (`new HashSet` ทุก tick), `DashMonster.FindNearestPlayer()` (`new Collider2D[10]` ทุกครั้งที่เรียก)

---

## 10. Checklist เทสรวม `integration/feedback-jitter` (เล่นรอบเดียว, 2026-10-03)

**Branch:** `integration/feedback-jitter` = `branch-UpdateSession` (`17719e1a`) + merge `fix/feedback-v0902` (`4894d443`) + merge `fix/network-jitter` (`80356b8d`) — ไม่มี `test/verify`, merge ไม่มี conflict, compile 0 error (`Editor.log` 2026-10-03 05:38:52). ยังไม่ merge เข้า `branch-UpdateSession`

**ตั้งค่า:** MPPM scenario `DuoGamePlay_Test` → Host (Main Editor) = **Mira**, Client (Player 2) = **Kael** → เริ่มจาก `SessionScene` ตามขั้นตอน 7.3 ข้อ 1–3
**จดผล:** ช่อง "ผล" ใส่ ✅ / ❌ / ⚠️ + สิ่งที่เห็น และบอกว่าเห็นที่หน้าต่างไหน (Host / Client) — ข้อที่ขึ้นต้นด้วย "✅ โค้ด" ตรวจจากโค้ด/asset แล้ว (ไม่ได้รันจริง) ไม่ต้องเล่น

### 10.0 รอบเล่นเดียว (~15 นาที) — branch `test/all` (2026-10-03)
`test/all` = `integration/feedback-jitter` + merge `test/verify` (`10d0a696`) + log FB-8 (`f35667ee`) — **ห้าม merge ไปไหน** มีปุ่ม F6–F10 + log `[VERIFY]` ครบ compile 0 error (`MainGame.Core.dll` 06:12:19)
รอบนี้ครอบคลุม T1–T7 (หัวข้อ 7) + ข้อในหัวข้อ 10 ที่ยังต้องรันจริง — ผมอ่านผลจาก log เอง **คุณจดแค่สิ่งที่ "ดูแปลก"**
**ห้ามปิด/เปิด Unity ใหม่ก่อนผมอ่าน log** (`Editor.log` ถูกเขียนทับเมื่อ Unity เริ่มใหม่)

| ขั้น | ทำอะไร | ผมดูจาก log | คุณดูด้วยตา |
|---|---|---|---|
| 0 | Play (`DuoGamePlay_Test`) → "เริ่มเกม" ตาม 7.3 → Console ค้น `[VERIFY]` เห็น `DebugKeys ready` 2 ฝั่ง | — | — |
| | **ห้ามกด Stop จนจบขั้น 4** | | |
| 1 (S1) | F6 → Mira เดินผ่าน x≈-13, Kael ไปบ่อแรก x≈-1.5 (Tab ปิด dialogue) — ระหว่างทาง **เหยียบพุ่มไม้ 1 ครั้ง** และ **Client เดินชนมอนตัวแรก** | — | ท่าแตกของพุ่มไม้ (FB-1a); มอนไหล/สั่นในหน้าต่าง Client ไหม (NJ-6) |
| 1b | Client (Kael) ชิด Mira: E ยก, รอ 2 วิ, E โยน | T1/H-4: `RPC_UpdateCarry` ซ้ำไหม | นกดีดกลับ/โยนซ้ำ |
| 1-FB8 | Client: **(a)** E ยก รอ 1 วิ แล้วกระโดด → วาง **(b)** E ยก แล้วกระโดดทันที → วาง **(c)** แบกแล้วเดินตกขอบ | FB-8: `FB8 duck anim … state=… carrying=… grounded=…` | ไม่ต้องดู |
| 1c | Kael ลอยในบ่อ: Space 1 ครั้ง แล้ว F 1 ครั้ง | T1/H-4: `RPC_TriggerSplash` ซ้ำไหม | แถบฟองเป็ด 8 ช่อง |
| 1d–1e | Host F7 → **Client** E เก็บกุญแจ → Host F7 → **Host** E เก็บกุญแจ | T1/H-4: `RPC_RequestAddStone` | — |
| 2 | Esc → Quit → ถ้าค้าง F9 ทั้ง 2 หน้าต่าง → ลองกด Play / Create Room | T2: C-1(a), H-1 | กลับเมนูเองไหม; UI ในเกมซ้อนเมนูไหม; ปุ่มกดติดไหม |
| 3 | เริ่มเกมใหม่ → Mira กระโดดแล้วกด Space กลางอากาศ | T3: M-4 (`birdFly=…`) | ไอคอนสกิลขึ้นตั้งแต่เกิดไหม |
| 4 | Mira ไป x≈-13 ให้ dialogue เปิด อย่าปิด → **F10 ในหน้าต่าง Player 2** → ถ้า Host ค้าง F9 ทั้งคู่ → เริ่มเกมใหม่ (ยังไม่กด Tab) → A/D ค้าง 5 วิ ทั้งคู่ → ถ้ามี dialogue ขึ้น Tab แล้วลองขยับอีก | T4: C-1(c), M-4, H-2 | Host ถูกพากลับไหม; ขยับได้ไหม |
| | จากนี้ Stop/Play ได้ | | |
| 5 | เริ่มเกม → Host **F8** (ไป S2) → ปิด cutscene/dialogue → **ให้ลมดันเข้ามุมพื้น/ผนัง 2–3 จุด** → Host **F8** (ไป S3) → **ดูบริเวณตาชั่ง** | T5: M-2 (`DUPLICATE`, error) | ทะลุแมพไหม (FB-5); เห็น `Scale_Door` / แพลตฟอร์มข้างตาชั่งไหม (FB-7) |
| 6 | อยู่ใน S3 → Host F8 → ถ้าค้าง F9 ทั้งคู่ | T6: C-1(b) | กลับเมนูหรือค้าง loading |
| 7 | เริ่มเกม → S1 → **F10 ในหน้าต่าง Main Editor** → รอ 15 วิ | T7: C-1(d) | Player 2 กลับเมนูไหม |
| 8 (ถ้ามีเวลา) | Stop → เปิด Network Conditions (ดู "รอบเปิด lag") → เริ่มเกม → เล่น S1 2 นาที ชนมอน 1 ครั้ง → ปิด Network Conditions กลับ ห้าม commit | — | jitter ดีขึ้น/แย่ลง (LAG-ALL, LAG-NJ6) |

เสร็จแล้วพิมพ์ "เสร็จ" + สิ่งที่ดูแปลก (ถ้ามี) → ผมอ่าน `Editor.log` + `Library/VP/mppm293b93cf/Logs/Editor.log` แล้วกรอก 7.5 และหัวข้อ 10 ให้

### Lobby
| รหัส | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|
| L-NAV | ไม่เสียบจอย ดูเมนูหลัก; ในด่านกด Esc ดูเมนู Pause; ถ้ามีจอย เสียบแล้วกดเลื่อน แล้วถอด | ไม่เสียบ: ไม่มีกรอบ highlight / เสียบ: กรอบขึ้นและเลื่อนตาม / ถอด: หาย | ✅ (ส่วนไม่เสียบจอย) โค้ด: `LateUpdate` ไม่มี gamepad → `selected = null` → ซ่อนกรอบ — ส่วนเสียบจอยต้องมีจอยจริง |
| L-NJ3 | เปิด Stats ใน Game view | ไม่เกิน ~60 FPS (ถ้ากด Play จากด่านโดยตรงจะไม่ถูกจำกัด เพราะตั้งค่าใน `SessionManager`) | ✅ โค้ด: `SessionManager.Awake` ตั้ง 60; quality เริ่มต้น vSync 0 จึงมีผล |
| L-FB2 | กดปุ่มในเมนู | ได้ยินเสียงคลิก | ✅ โค้ด: lobby มี listener บน `Main Camera` ของตัวเอง ไม่ถูกแตะก่อนเข้าด่าน |

### S1
| รหัส | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|
| S1-FB2a | ที่จุดเกิด เดิน/กระโดดทั้ง 2 ฝั่ง | แต่ละฝั่งได้ยินเสียงตัวเอง; Console ไม่มี warning ว่ามี AudioListener 2 ตัว | ✅ โค้ด/asset: listener มีแค่บนกล้องของ scene (`Main Camera`, `UI_Camera` ×3) ไม่มีใน prefab ผู้เล่น; `ClaimAudioListener` ปิดตัวอื่น; กล้องห่างตัวละคร ~0.44 หน่วย (< min distance 5) → ดังเต็ม |
| S1-FB2b (ใหม่: ระยะได้ยิน) | Client เดินออกไปจน Host มองไม่เห็นเป็ด (ห่างเกิน ~1.5 จอ ≈ 20 หน่วย) แล้วกระโดด/ดำน้ำ; จากนั้นเดินกลับมาใกล้แล้วทำซ้ำ | **ทั้งใกล้และไกล Host ไม่ได้ยินเสียงตัวละครของ Client** — โค้ดปัจจุบันเล่นเสียงตัวละครเฉพาะเครื่องเจ้าของ (`HasInputAuthority` / `InputAuthority == LocalPlayer` ใน `MovementCharacter`, `Bird_Moveset`, `Duck_Moveset`) ไม่เกี่ยวกับ FB-2; เสียงของตัวเองต้องดังเท่าเดิมทุกที่ในด่าน; เสียงของในฉาก (ปืนหิมะ, กับดักหมี, checkpoint) ดังเมื่ออยู่ใกล้กล้องเราและเบาลงเมื่อไกล — ระยะขึ้นกับ AudioSource ของแต่ละ prefab (ยังไม่ได้ตรวจค่า) | ✅ (ข้อมูล) โค้ด: ทุกเสียงตัวละครเล่นเฉพาะเครื่องเจ้าของ → ไม่ได้ยินเสียงเพื่อนไม่ว่าระยะไหน (พฤติกรรมเดิม — ถ้าอยากให้ได้ยินเป็นงานใหม่) |
| S1-FB2c | เดินไปสุดด่าน | ยังได้ยินเสียงตัวเอง | ✅ โค้ด: listener ย้ายตามกล้องทุกเฟรม (`CameraCharacter.LateUpdate`) |
| S1-FB1a | เหยียบพุ่มไม้ (`CrumblingPlatform`) แล้วออกมา | สั่น ~1 วิ → เห็นท่าแตก → หาย → ~3 วิ กลับมาปกติทันทีไม่มีเฟรมแตกค้าง (ทั้ง 2 หน้าต่าง) | ⚠️ asset ตรง: `Platform_Break.anim` ยาว 0.875 วิ = `breakAnimDuration`, state `Platform_Break` / `Platform_Idle` มีใน controller — **ภาพต้องดูในรอบเล่น** |
| S1-FB1b (ใหม่: ตกทะลุ) | ยืนบนพุ่มไม้จนมันแตก | ผู้เล่น**ตกทันทีที่ท่าแตกเริ่ม** — collider ปิดตอนเริ่มท่าแตก จังหวะตกเท่าก่อนแก้ ถ้าอยากให้ยืนได้จนท่าแตกจบ = เปลี่ยน gameplay (ตกช้าลง 0.875 วิ) จดไว้ให้ตัดสินใจ | ✅ โค้ด: ตกทันที — **รอเจ้าของโปรเจกต์ตัดสินใจ** ว่าโอเคหรือไม่ |
| S1-FB8a (ใหม่) | Client (Kael) ยืนชิด Mira กด E ยก **รอ 1 วิ** แล้วเดิน, กระโดด | คาดจากโค้ด: เป็นท่ายกตลอด (ขณะกระโดดใช้ท่ายกแบบเดิน เพราะไม่มีท่ากระโดด/ตกขณะยก) | |
| S1-FB8b (ใหม่) | กด E ยกแล้ว**กระโดดทันที** (ภายในครึ่งวิ) | คาดจากโค้ด: **ท่าปกติ (ไม่ยก) ค้างจนแตะพื้น** — ถ้าเป็นแบบนี้ = ยืนยัน FB-8 สาเหตุคือ lock 0.5 วิของท่า `Interact` ที่หาไม่เจอ + ไม่มี `Falling_carry` | |
| S1-FB8c (ใหม่) | แบกอยู่แล้วเดินตกขอบ (ไม่กดกระโดด) | คาดจากโค้ด: เป็นท่ายกตลอด | |
| S1-NJ2 | ระหว่างทำ S1-FB8 ดู Console | ไม่มี `can't find Falling_carry` / `can't find Interact` | ✅ โค้ด: บรรทัด log ถูกลบแล้ว |
| S1-FB4 | Mira ลงน้ำลึกที่บ่อแรก (x≈-1.5); Kael กด F ดำน้ำ | นก: แถบฟอง 6 ช่องเต็ม / เป็ด: 8 ช่อง | ✅ asset: นก `drowningTime 6`, เป็ด `divingTime 8`, prefab ทั้งคู่มี 8 ช่อง → แสดง 6 / 8 |
| S1-NJ6 | Client เดินชนมอนที่ยืนนิ่ง (`Kinght_Jay` / `Puffy_Jay`) | หน้าต่าง Client: มอนไม่ไหล/ไม่สั่น; ทั้ง 2 ฝั่ง: มอนยังเห็นผู้เล่น พุ่ง/เดินตาม ระเบิด ทำดาเมจ เกิดใหม่ได้ | |
| S1-NJ5 | ดูแมงกะพรุนที่เคลื่อนที่ (`JellyTrap`) | ทั้ง 2 หน้าต่างเห็นตำแหน่งตรงกัน ชน/โดนดาเมจได้ตามเดิม | ✅ โค้ด: สูตรเดิม เปลี่ยนแค่ `Runner.DeltaTime` → `Time.deltaTime` ซึ่งเท่ากันที่ 60 FPS |
| S1-NJ4a | ยืนให้ปืนหิมะ (`Snowball_Moshoot`) ยิงโดน ทั้ง Mira และ Kael | กระสุนโดนทั้ง 2 คน เสียเลือด และกระสุนหายเมื่อโดน | ✅ โค้ด/asset: ปืนยิง `Snow.prefab` ที่ใช้ `SnowBallProjectile`; ผู้เล่นทั้งคู่มีอยู่ตอนกระสุน spawn; เงื่อนไขโดนเหมือนเดิม |

### S2
| รหัส | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|
| S2-FB2 | หลังเปลี่ยนด่าน เดิน/กระโดด | ยังได้ยินเสียงตัวเอง; ไม่มี warning AudioListener 2 ตัว | ✅ โค้ด: `OnSceneChanged` เรียก `ClaimAudioListener` ปิด `UI_Camera` ของด่านใหม่ |
| S2-FB5 | ให้เครื่องพ่นลมดันเข้ามุมพื้น/ผนังหลายจุด | ไม่ทะลุแมพ; เดิน กระโดด ยืนบนแพลตฟอร์มเคลื่อนที่รู้สึกเหมือนเดิม | |
| S2-NJ4b | เข้าเครื่องพ่นลมทั้งแบบดันและแบบดูด ทั้ง 2 ตัวละคร | ลมยังดัน/ดูดทั้ง 2 ฝั่ง | ✅ โค้ด: list และ layer mask ที่ใช้ซ้ำให้ผลเหมือนสร้างใหม่ทุก tick |
| S2-NJ4c | ปีนเถาวัลย์ แล้วปีนออกนอกเขต | ปีนได้ตามเดิม และหลุดเมื่อออกนอกเขต | ✅ โค้ด: query เดิม (ตำแหน่ง/ขนาด/mask/การนับ trigger เหมือนกัน) ต่างแค่รับได้ 8 ชิ้น |
| S2-NJ5 | เดินเข้าวัตถุที่จางเมื่อผู้เล่นอยู่ข้างใน (`FadeWhenPlayerInside`) แล้วออก | จางลงตอนเข้า กลับมาตอนออก ทั้ง 2 หน้าต่าง | ✅ โค้ด: เหมือน S1-NJ5 |

### S3
| รหัส | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|
| S3-NJ5 | ดูประตูบานพับ (`Timber 1_0`) และแพลตฟอร์มหมุน (`Rotate_platfrom`), ยืน/ชน | ทั้ง 2 หน้าต่างเห็นมุมตรงกัน ยืน/ชนได้ตามเดิม | ✅ โค้ด: เหมือน S1-NJ5 |
| S3-NJ4c | ปีนเถาวัลย์ | ปีนได้ตามเดิม | ✅ โค้ด: เหมือน S2-NJ4c |
| S3-FB7 | (ยังไม่แก้) ดูบริเวณตาชั่ง | จดว่ามองเห็น `Scale_Door` / แพลตฟอร์มข้างตาชั่งหรือไม่ — ใช้ยืนยันสาเหตุ | |

### รอบเปิด lag
Stop → เลือก `Assets/Photon/Fusion/Resources/NetworkProjectConfig` → **Network Conditions → Enabled** (delay 150 ms ± 50, loss 5%) → Play ใหม่ — เทสเสร็จแล้ว **ปิดกลับ และห้าม commit ไฟล์นี้** (`git status` ต้องไม่มี `NetworkProjectConfig.fusion`)
| รหัส | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|
| LAG-NJ6 | ทำ S1-NJ6 ซ้ำ | มอนฝั่ง Client ไม่ไหล/ไม่สั่นตอนชน (ตอนมอนพุ่ง/เดินอาจยังขยับเป็นจังหวะ = NJ-11 ยังไม่แก้) | |
| LAG-ALL | เล่น S1 ช่วงสั้น ๆ ทั้ง 2 ฝั่ง | "ทั้งจอสั่นตอน FPS ตก" ควรดีขึ้น; "ตัวเราเองสั่น" ในลม/บนแพลตฟอร์มคาดว่า **ยังเหลือ** (NJ-7 ถึง NJ-10) | |

### กลับ lobby
| รหัส | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|
| END-FB2 | กลับหน้าเมนู (เข้าประตูสุดท้าย S3 หรือ Esc → Quit) แล้วกดปุ่มในเมนู | ได้ยินเสียงคลิก — ถ้าค้างในด่านไม่กลับเมนู คือบั๊ก C-1 ที่ยังไม่แก้ ไม่ใช่ปัญหาของ branch นี้ ให้กด Stop | ✅ โค้ด: `GameManager.ReturnToSession` ทำลายกล้อง local ก่อน `LoadScene` → listener ของ lobby ตัวใหม่ทำงาน (เฉพาะเมื่อกลับ lobby ได้ — ถ้าค้างคือ C-1) |

---

## 11. ขั้น 1–2: แก้การกลับ lobby + วัด network (2026-10-16)

### 11.1 สิ่งที่แก้ — branch `fix/c1-lobby-return` (แตกจาก `integration/feedback-jitter` @ `80356b8d`, ยังไม่ merge, ยังไม่ได้เทสตอนรัน)
| รหัส | commit | ไฟล์ : method | เปลี่ยนอะไร |
|---|---|---|---|
| C-1, H-7 | `6d6ae65a` | `GameManager.cs` : `IsInGameplay`, `MarkInGameplay`, `BackToSessionScene`, `ReturnToSession`, `ResetReturnGuard`; `INetworkStructure.cs` : `OnShutdown`, `OnPlayerLeft`, `OnDisconnectedFromServer`, `OnSceneLoadStart` | สถานะ "อยู่ในเกม" ตั้งตอน Fusion เริ่มโหลดด่าน ล้างตอนกลับ lobby — ใช้แทน `SessionManager.Instance == null` ที่ไม่เคยเป็น null ในเกม; ทุกเส้นทางออกจึงเข้า `ReturnToSession`; อีกฝั่งหลุด → ปิด runner ตัวเองแล้วกลับ lobby ทั้งคู่; log `Fusion: Shutdown reason: … inGameplay=…` |
| M-4, H-2 | `a92d5117` | `GameManager.cs` : `ReturnToSession`; `DialogueManager.cs` : `ResetForNewSession` (ใหม่); `TutorialUIManager.cs` : `HideTutorial` | ล้าง static สกิล 4 ตัว, `IsDialogueOpen`, tutorial ที่เปิดค้าง ทุกเส้นทางออก |
| NJ-0 | `c5ec1bd4` | `Utility/NetworkStatsHotkey.cs` (ใหม่) | กด F11 เปิด/ปิดแผง Fusion Statistics (เฉพาะ Editor / Development Build) |

**Compile:** Unity ไม่ได้เปิดอยู่ตอนแก้ → คอมไพล์สคริปต์ทั้ง assembly `MainGame.Core` ด้วย Roslyn (`dotnet` SDK 10.0.400) ใช้ reference ชุดเดียวกับ `MainGame.Core.csproj` → **0 error** ทั้ง `fix/c1-lobby-return` และ `test/c1` (ทดสอบแล้วว่าวิธีนี้จับ error ได้) — ยังไม่ผ่าน Fusion IL weaving ของ Unity: เปิด Unity แล้วดู Console อีกครั้ง; Unity จะสร้าง `NetworkStatsHotkey.cs.meta` ให้ → commit ตาม
**เปลี่ยนจากแผน:** H-1 ไม่แก้ — ดูเหตุผลใต้ H-1 ในหัวข้อ 4

### 11.2 วัด network (NJ-0) — ค่า baseline ก่อนเฟส 3–5
กด **F11 ในแต่ละหน้าต่าง** → แผงขึ้นมุมขวาบน มีกราฟ RTT, In/Out Bandwidth, Resimulations, Forward Ticks, In/Out Object Updates (ถ้ากราฟไม่ขึ้น ให้เลือกในแผงเอง) — กด F11 อีกครั้งเพื่อปิด
จดค่าเฉลี่ยคร่าว ๆ (หรือแคปหน้าจอ) แล้วส่งมา ผมจะกรอกตาราง:

| จังหวะ | หน้าต่าง | RTT (ms) | In (B/s) | Out (B/s) | Resimulations | Forward ticks | Object updates in/out | FPS |
|---|---|---|---|---|---|---|---|---|
| S1 จุดเกิด ยืนนิ่ง 30 วิ | Host | | | | | | | |
| S1 จุดเกิด ยืนนิ่ง 30 วิ | Client | | | | | | | |
| S1 เดิน/กระโดด 30 วิ | Client | | | | | | | |
| เปิด Network Conditions (150 ms ± 50, loss 5%) เดิน 30 วิ | Client | | | | | | | |

### 11.3 Checklist เทส — branch `test/c1` (local, **ห้าม merge**) ~10 นาที
`test/c1` = `fix/c1-lobby-return` + ปุ่ม debug F6–F10 จาก `test/verify` (`88e9eb7b`) — MPPM `DuoGamePlay_Test`, Host = Mira, Client = Kael, "เริ่มเกม" ตาม 7.3
**ห้ามปิด Unity ก่อนผมอ่าน log** (`Editor.log` + `Library/VP/mppm293b93cf/Logs/Editor.log`)

| ขั้น | ทำอะไร | ควรเห็น | log ที่ผมจะดู | ผล |
|---|---|---|---|---|
| 1 | เริ่มเกม → F11 ทั้ง 2 หน้าต่าง → ทำตาราง 11.2 (ปิด Network Conditions กลับ ห้าม commit) | แผงสถิติขึ้น | `[NetStats]` | |
| 2 (C-1 d) | **F10 ในหน้าต่าง Host** รอ ~10 วิ | Player 2 กลับ lobby เอง และเข้าห้อง/เริ่มเกมใหม่ได้ | `Fusion: Shutdown reason … inGameplay=True` ฝั่ง Player 2 | |
| 3 (C-1 c, H-7) | เริ่มเกม → **F10 ในหน้าต่าง Player 2** | **Host กลับ lobby เองด้วย** (ไม่อยู่ในด่านคนเดียว) | Host: `Shutdown … inGameplay=True` | |
| 4 (H-2, M-4) | เริ่มเกม → Mira เดินไป x≈-13 ให้ dialogue เปิด อย่าปิด → F10 ที่ Player 2 → เริ่มเกมใหม่ | หลัง cutscene ขยับได้; ไม่มีไอคอนสกิล Fly ตั้งแต่เกิด | `sceneLoaded 'SessionScene' … IsDialogueOpen=False birdFly=False` | |
| 5 (C-1 b) | เริ่มเกม → Host F8 (S2) → ปิด cutscene/dialogue → F8 (S3) → F8 (ประตูสุดท้าย) | ทั้งคู่กลับ lobby; ไม่มี HUD ในเกมซ้อน; เริ่มเกมใหม่ได้ สกิลล็อก | `Shutdown … inGameplay=True` ทั้ง 2 ฝั่ง + `sceneLoaded 'SessionScene' … rootCanvases:` | |
| 6 (C-1 a ไม่พัง) | เริ่มเกม → Esc → Quit | ทั้งคู่กลับ lobby เหมือนเดิม | `Shutdown … inGameplay=True` | |

---

## 12. ขั้น 3–5: redesign network (2026-10-16)

**เป้าหมาย:** ตัวจำลองของแต่ละ object sync ค่าน้อยที่สุดเท่าที่การจำลองต้องใช้ ส่วนภาพ (ท่าทาง, การแสดง/ซ่อน, กระพริบ) แต่ละเครื่องคำนวณเองจากค่าที่ sync อยู่แล้ว และ client ทำนายได้ตรงกับ host ในจุดที่เคยดีด
**ความคาดหวังที่ตรงไปตรงมา:** ปริมาณข้อมูลลดลงเล็กน้อย (เช่น string ชื่อท่า 32 ตัว + float 2 ตัวต่อผู้เล่น, RPC 5 ตัว) — ผลหลักคือ client ทำนายถูกขึ้น (ลม, แพลตฟอร์ม, กดปุ่มตอน lag, เริ่มบิน, ชนมอนที่กำลังพุ่ง) และท่าทางตรงกันทุกเครื่อง

**Branch:** `fix/net-redesign` แตกจาก `fix/c1-lobby-return` (`cb4ddf35`) — ยังไม่ merge, **ยังไม่ได้เทสตอนรัน**; เทสบน `test/net` (= `fix/net-redesign` + ปุ่ม F6–F10, `b1bdd63c`, ห้าม merge)
**Compile:** Roslyn (reference ชุดเดียวกับ Unity) 0 error ทุก commit — Unity compile จริงดูในข้อความแจ้งผล (Unity MCP หลุดอีกครั้งหลัง Unity โหลดโค้ดใหม่)

### 12.1 สิ่งที่แก้
| รหัส | commit | ไฟล์ : method | เปลี่ยนอะไร |
|---|---|---|---|
| NR-1 | `c6c83cce` | `CharacterAnimation.cs` (เขียนใหม่) + call site ใน `MovementCharacter.cs`, `Bird_Moveset.cs`, `Duck_Moveset.cs`, `BreakableRock.Interact` | เลิก sync `CurrentAnimState` (string 32), `AnimX`, `AnimY`, `State`, `isFrozen`; `Render()` เลือกท่าจาก `isDead`, action, `IsReleasingThrow`/`_prepareToThrow`/`IsFlying`/`AlreadyFloating` (นก), `onDiving`/`isWaterSurface` (เป็ด), ลอย/ตก/ติดพื้น/แบก + ความเร็วของภาพในเครื่องตัวเอง; sync แค่ `FlipX` + `CurrentAction` + `AnimationTimer`; ลบ `PlayThrowAnimation_RPC`, `PlayHitAnimation_RPC`; FB-8 + `interact` ของเป็ด |
| NR-2 | `dd9563a9` | `Enemy_KnightJay.Render`, `Enemy_PuffyJay.Render` | client ใช้ `NetworkBehaviourBufferInterpolator` (NJ-11) |
| NR-3 | `004b52af` | `TrapPressure.Spawned`, `MovingPlateform.Spawned` | `Runner.SetIsSimulated(Object, true)` (NJ-7) |
| NR-4 | `aa56741b` | `NetworkInputData` + `InputButton` (`INetworkStructure.cs`); `MovementCharacter.FixedUpdateNetwork` / `SimulateTick` / `WasPressed` / `PreviousButtons`; `Bird_Moveset`, `Duck_Moveset` | ปุ่มเป็น `NetworkButtons`, จับจังหวะกดเทียบ `[Networked] PreviousButtons` แทน bool 10 ตัว (M-1) |
| NR-5 | `7dbde7c2` | `MovementCharacter` | `isMoveAble`, `isOptional`, `isSpeedoptional`, `optionalGravity`, `wasMenuOpenLastTick`, `lastPlatformPos` เป็น `[Networked]` (M-1, NJ-10) |
| NR-6 | `9d477aa4` | `Bird_Moveset.StartFlying` | client ที่คุมนกเริ่มบินเองได้ (NJ-8) |
| NR-8 | `62c3695a` | `BaseMonster` (`SetHitBoxActive`, `ApplyVisualState`), `Enemy_KnightJay`, `Enemy_PuffyJay` | เลิก `SetVisuals_RPC` / `TriggerHitBox_RPC`; การแสดงผลและ collider คำนวณจาก `isHidden`/`hasFinishedDash`/`isDashing` ทุกเครื่อง → collider ตอนพุ่งตรงกัน (NJ-8) |
| NR-9 | `37b2e9ec` | `DamageFlash.StartFlash`, `MovementCharacter.RPC_TakeDamage` | timer กระพริบ sync อยู่แล้ว → host ตั้งค่าเดียว ไม่ต้อง RPC |

### 12.2 เปลี่ยนจากแผน / ยังไม่ได้ทำ
- **NR-7 (แรงกระแทกตอนโดนตี) ไม่ทำ:** client รู้ว่าโดนตีก็ต่อเมื่อ state จาก host มาถึงอยู่แล้ว (hitbox ตัดสินบน host) การย้าย `AddForce` เข้า FUN แทบไม่ต่าง ต้องให้ client ทำนายการโดนตีเองซึ่งใหญ่กว่ามาก
- **ไม่แตะ:** ลำดับตาย/เกิดใหม่ (`DeathMechanic_RPC` → `RPC_OnRespawned`, ผูกกับ GUI/LifeCycle/collider หลายจุด), RPC เสียงของกับดัก (เกิดไม่บ่อย), RPC ขอสิทธิ์ client → host (จำเป็น)
- **ผลข้างเคียงที่ควรรู้:** ค่า blend ของท่าเดินมาจากการเคลื่อนที่จริงของภาพ (เดินชนกำแพงจะเป็นท่ายืน); ท่า `interact` ของเป็ดเล่นแล้ว (เดิมไม่เคยเล่นเพราะชื่อไม่ตรง)

### 12.3 Checklist เทส — branch `test/net` (MPPM `DuoGamePlay_Test`, Host = Mira, Client = Kael) ~15 นาที
**ห้ามปิด Unity ก่อนผมอ่าน log**
| ขั้น | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|
| 1 วัด | F11 ทั้ง 2 หน้าต่าง → จดตาราง 11.2 อีกชุด ทั้งปิด/เปิด Network Conditions (ปิดกลับ ห้าม commit) | เทียบกับ `test/c1` ถ้าวัดไว้ | |
| 2 ท่าทาง | ดูทั้ง 2 จอ: เดิน/หยุด, กระโดด/ตก, **เป็ดแบกนกแล้วกระโดดทันที**, เป็ดลอยน้ำ (แบก/ไม่แบก) / ดำน้ำ / ทุบหิน, นกบิน / ร่อน / เล็ง / ปา, กด E หยิบของ, โดน stun, ตาย-เกิดใหม่ | ท่าตรงสถานการณ์และตรงกันทั้ง 2 จอ; FB-8 เป็นท่ายกตลอด | |
| 3 ปุ่ม | E / Esc / Tab / G / Q / F / Space กดครั้งเดียว ทั้งปิดและเปิด lag | ทำงานครั้งเดียว ไม่ซ้ำ ไม่หาย | |
| 4 jitter (เปิด lag) | Client ยืนในลม S1/S2, ยืนบนแพลตฟอร์มเคลื่อนที่, นกเริ่มบิน, เดินชนมอนที่กำลังพุ่ง | ตัวเองไม่ถูกดึง/ดีดกลับ | |
| 5 มอน | ดูมอนพุ่ง/เดินฝั่ง Client, พุ่งจบแล้วหาย → เกิดใหม่ | เคลื่อนนุ่ม หาย/โผล่ตรงกันทั้ง 2 จอ, ยังโดนดาเมจ | |
| 6 ไม่พัง | Host F8 ไป S2 → S3 → ประตูสุดท้าย; ปีนเถาวัลย์; ลมดัน/ดูด; ไอคอนสกิลทุบ (E) ของเป็ด | กลับ lobby ได้, ทุกอย่างทำงานเหมือนเดิม | |

---

## 13. ปรับจูนเพิ่ม: 60 FPS, กล้อง, อาการตัวเองสั่นตอน lag (2026-10-17)

**การตัดสินใจของเจ้าของโปรเจกต์:** ล็อก 60 FPS ด้วย VSync; กล้องเลือกทาง "ลด Bloom"; อาการตัวเองสั่นตอน lag แก้ทั้งหมดยกเว้นการทำนายการโดนตี (ใช้การเกลี่ยภาพแทน); tick rate คงเดิม (60); ขยะ memory ที่เหลือทำทีหลัง
**Branch:** `fix/lag-polish` แตกจาก `fix/net-redesign` (`37b2e9ec`) — ยังไม่ merge, **ยังไม่ได้เทสตอนรัน**; เทสบน `test/lag` (= `fix/lag-polish` + ปุ่ม F6–F10, `19296eca`, ห้าม merge)
**Compile:** Unity 0 error (ตรวจผ่าน Unity MCP — `Unity_GetConsoleLogs`, 2026-10-17 00:04) + Roslyn 0 error ทุก commit

### 13.1 สิ่งที่แก้
| รหัส | commit | ไฟล์ : method | เปลี่ยนอะไร |
|---|---|---|---|
| NJ-3 (ปรับ) | `9c04cb30` | `SessionManager.ApplyFrameRateCap` (ใหม่, เรียกจาก `Awake`) | เปิด VSync ทุก N รอบของจอ (60Hz → 1, 120Hz → 2, 240Hz → 4) ให้ได้ 60 FPS พอดีและจังหวะเฟรมสม่ำเสมอ; จอที่หาร 60 ไม่ลงตัว (144/165Hz) ปิด VSync แล้วจำกัดด้วย `targetFrameRate = 60` (อาจเห็นภาพฉีกเล็กน้อย); ทับค่า VSync ของ quality ระดับ Medium ขึ้นไปด้วย |
| CAM-1 | `c84043af` | `Resources/Volume/Global Volume Profile.asset` (แก้ผ่าน Unity MCP) | Bloom: ปิด High Quality Filtering, จำกัด 4 รอบ (เดิม 6) — scatter 0.15 อยู่แล้ว ภาพแทบไม่เปลี่ยน; กล้อง blur 2 ตัว (Depth of Field) ไม่แตะ |
| LP-1 | `ce74fc22` | `NetworkStatsHotkey.OnGUI` | F11 แสดงว่าต่อกันตรง (Direct) หรือผ่าน relay ของ Photon + RTT (ms) ที่มุมซ้ายบน |
| LP-2 | `54cc1f94` | `MovementCharacter.LateUpdate` → `SmoothPredictionCorrection` / `ResetCorrectionSmoothing` | ตัวละครของเราเอง: ส่วนที่ภาพกระโดดเร็วเกิน 30 หน่วย/วิ (แต่ไม่เกิน 2 หน่วย = ไม่ใช่วาร์ป) ถือเป็นการแก้ตำแหน่งจาก host เก็บเป็นค่าเหลื่อมแล้วลดลงภายใน ~0.1 วิ → ไหลแทนดีด (ไม่แตะการจำลอง) |
| LP-3 | `406b2402` | `MovementCharacter` : `DeathMechanic`, `OnDeathCountChanged`, `ApplyDeathLocal`, `CharacterDie`, `Respawn`, `OnRespawnCountChanged`, `ApplyRespawnLocal`, `OnRespawned`, `DoResetToSpawn`; `Bird_Moveset.OnRespawned`, `Duck_Moveset.OnRespawned` | ตาย/เกิดใหม่: host ตัดสินที่เดียวแล้วเพิ่ม `DeathCount` / `RespawnCount` (+ `IsPrimaryDeath`, `LastRespawnWasReset`) ทุกเครื่องทำ UI เสียง LifeCycle และ rigidbody ของตัวเองใน `OnChangedRender`; ลบ RPC 6 ตัว (`DeathMechanic_RPC`, `RPC_OnRespawned`, `RPC_PlayDieSound`, `RPC_PlayFriendDeathEffect` และที่ไม่มีคนใช้ `RPC_ForceClientTeleport`, `RPC_PlayRespawnSound`) |

RPC ทั้งเกม: 71 (`branch-UpdateSession`) → 65 (`fix/net-redesign`) → **59** (`fix/lag-polish`) → **58** (`fix/feedback-1017`, หัวข้อ 14) → **57** (`fix/feedback-1018`, หัวข้อ 15)

### 13.2 ไม่ทำ / ทำทีหลัง
- **ทำนายการโดนตีฝั่ง client:** ไม่ทำ (เจ้าของโปรเจกต์อนุมัติ) — client เห็นมอนช้ากว่า host ราวครึ่ง ping จะตัดสินผิดบ่อยแล้วดีดหนักกว่าเดิม; LP-2 เกลี่ยภาพตอนแรงกระแทกจาก host มาถึงแทน
- **กล้อง blur ทาง ข / ค:** ปุ่มปิด blur ในเมนู, เปลี่ยนเป็นภาพเบลอไว้ล่วงหน้า — รอวัดผลก่อน
- **ขยะ memory ที่เหลือ:** `CheckGround` (`LayerMask.GetMask` 2 ครั้งทุก tick), `CheckHeadBump`, `CheckInteractable` (`OverlapCircleAll` ทุกเฟรม), `PressurePlateHinge`, `WeighingScale_Mech` — ทำทีหลัง
- tick rate คง 60

### 13.3 Checklist เทส — branch `test/lag` (MPPM `DuoGamePlay_Test`, Host = Mira, Client = Kael) ~15 นาที
รวมข้อของ 11.3 (C-1) และ 12.3 (redesign) ไว้ในรอบนี้ได้ เพราะ `test/lag` มีทุกอย่าง — **ห้ามปิด Unity ก่อนผมอ่าน log**
**แนะนำให้เทสบน `test/all2`** (2026-10-17) = `fix/lag-polish` + merge `test/verify` ทั้งหมด (`eaf408ac`, แก้ conflict ใน `OnShutdown` / `BackToSessionScene` โดยคงโค้ดที่แก้แล้วและใส่ log `[VERIFY]` กลับ) + log FB-8 ของระบบท่าใหม่ (`7d23904d`) — มีปุ่ม F6–F10 และ log `[VERIFY]` ครบให้ผมตัดสินผลจาก log ได้ Unity compile 0 error — **ห้าม merge**
| ขั้น | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|
| 1 FPS | เปิด Stats ใน Game view (หรือ build) | ~60 FPS นิ่ง | |
| 2 การเชื่อมต่อ | F11 ทั้ง 2 หน้าต่าง | มุมซ้ายบนบอก Direct/Relayed + RTT; มุมขวาบนกราฟของ Fusion | |
| 3 Bloom | ดูแสงเรืองในฉาก S1 เทียบกับภาพเดิม | แทบไม่ต่าง | |
| 4 ดีดตอน lag | เปิด Network Conditions → Client ยืนในลม, บนแพลตฟอร์ม, นกเริ่มบิน, **โดนมอนตี** | ตัวเองไหลไปตำแหน่งที่ถูก ไม่กระโดดวาบ | |
| 5 ตาย/เกิดใหม่ | ให้ตัวหนึ่งตาย (มอน/จมน้ำ) | ทั้งคู่ตาย (อีกตัวเล่นเอฟเฟกต์เพื่อนตาย), เสียงตาย, ไอคอน/แถบอากาศหาย, ~3 วิ เกิดใหม่ที่ checkpoint พร้อมเสียง/เอฟเฟกต์ ทั้ง 2 จอ ขยับได้ปกติ | |
| 6 รีเซ็ตจากเมนู | Esc → Reset (โหวตครบ) | กลับจุด checkpoint ทั้งคู่ ขยับได้ | |
| 7 ข้อ 12.3 + 11.3 | ท่าทาง, ปุ่ม, มอน, ประตูสุดท้าย S3, หลุดแล้วกลับ lobby | ตามตารางเดิม | |

## 14. Feedback ชุด 2026-10-17 (6 ข้อ)

**ที่มา:** เจ้าของโปรเจกต์เล่น `test/all2` แล้วเจอสิ่งที่อยากแก้ + บั๊กเพิ่ม 6 ข้อ
**การตัดสินใจของเจ้าของโปรเจกต์:** หินที่วางอยู่ให้เดินทะลุได้; ผู้เล่นเดินทะลุตัวมอนได้; dialogue คงการโหวตแต่บอกว่ารออีกคน
**เพิ่มเติม (2026-10-17):** ตัวพุ่ง (Knight Jay) ชนผู้เล่นตอนยืนเตรียมพุ่ง ตอนยืนเฉยเดินทะลุได้ ตอนพุ่งใช้ hitbox ตี + กระเด็น; ตัวระเบิด (Puffy Jay) เดินทะลุได้ตลอด ระเบิดโดนปกติ; ไอคอนมึนตอนดำน้ำอ่านทิศจากท่าที่เล่นอยู่
**Branch:** `fix/feedback-1017` แตกจาก `fix/lag-polish` (`406b2402`) — ยังไม่ merge เข้าสายหลัก, **ยังไม่ได้เทสตอนรัน**; เทสบน `test/all2` (merge แล้ว, ห้าม merge)
**Compile:** Roslyn 0 error ทุก commit + Unity ผ่าน Unity MCP

### 14.1 สิ่งที่แก้
| รหัส | อาการ | ต้นเหตุ | commit | ไฟล์ : method | เปลี่ยนอะไร |
|---|---|---|---|---|---|
| FB2-1 | กับดักหมียังทำดาเมจระหว่าง animation | เปิด collider ดาเมจตอนงับแล้วเปิดค้างจนหมด cooldown (2 วิ) — เดินเข้ากับดักที่หุบอยู่ก็โดน | `ab1bcab0` | `BearTrapScript.FixedUpdateNetwork` | `[Networked] DamageWindowTimer` เปิด collider ดาเมจแค่ `damageWindow` (0.15 วิ, ปรับใน Inspector) ตอนงับ แล้วปิด — animation/cooldown เหมือนเดิม |
| FB2-2 | ยืนบนหินที่เก็บได้แล้วตัวละครกระตุก | หิน (Dynamic, หมุนได้) ชนกับผู้เล่น — host หินกลิ้งใต้เท้า, client หินเป็น proxy ย้อนเวลา ตำแหน่งถูกแก้ตลอด; `CheckGround` ไม่นับหินเป็นพื้น | `6ef7244f` | `RockObject` : `ApplyCollisionLayer`, `Render`, `FixedUpdateNetwork` | `[Networked] IsDangerous` = ถูกปา (`ThrowerId`) + ยังเร็วพอจะ stun → layer `ThrowAbleProjectile` (ชนเป็ดให้ stun/ฆ่ามอนเหมือนเดิม); นอกนั้น → `ItemDrop` (ไม่ชนผู้เล่น ยังชนพื้น) ทุกเครื่อง; กด E เก็บได้เหมือนเดิม |
| FB2-3 | เป็ดโดน stun แล้ว Puffy Jay ชน → ทะลุพื้น จอเพี้ยนสักพัก | Puffy/Knight Jay ขยับด้วย `transform.position` วาร์ปซ้อนตัวผู้เล่น → ฟิสิกส์ดันผู้เล่นออกทางที่สั้นที่สุด ทะลุพื้นที่เป็นขอบบางออกนอกแมพ กล้องติดขอบจนเกิดใหม่ | `824b09f2` | `BaseMonster.IgnorePlayerCollision` (ใหม่) เรียกจาก `BaseMonster.Spawned` + `MovementCharacter.Spawned` | `Physics2D.IgnoreCollision` ระหว่างตัวมอนกับ collider ผู้เล่นทั้ง 3 ตัว ทุกเครื่อง → เดินทะลุตัวมอน; ดาเมจยังมาจาก hitbox (trigger), หินที่ปายังชนมอน |
| FB2-4 | ท่าทุบของเป็ดจบก่อนหินแตก | `SetTrigger` แล้ว animator รอ exit time 0.75 + blend 0.25 ของ state ว่าง ได้ถึง ~1 วิก่อนเริ่ม clip แตก | `1c109186` | `BreakableRock` : `Interact`, `FixedUpdateNetwork` (ใหม่), `OnRockBroken`, `PlayBreakClip` (ใหม่) | host ตั้ง `[Networked] BreakTimer` (`breakDelay` 0.1 วิ) → ครบแล้วหินแตก (ของหล่น + `isBroken`); ทุกเครื่องเล่น clip แตกทันทีด้วย `animator.Play` (ข้าม exit time) → รอยร้าวขึ้น ~0.53 วิ ตรงจังหวะค้อนลง; ลบ `RPC_BreakRock` (RPC 59 → **58**); กันทุบซ้ำระหว่างรอ |
| FB2-5 | ไอคอนมึนไม่ตรงหัวเป็ด | `StunAni` วางไว้ตำแหน่งตายตัวสำหรับตอนหันซ้าย — หันขวา (`flipX`) หัวย้ายไปอีกฝั่งแต่ไอคอนค้างซ้าย (ตรวจจากภาพเรนเดอร์ prefab ผ่าน Unity MCP) | `461333a0` | `PlayerGUI` : `AlignStunIcon` (ใหม่), `ShowStunIcon`, `Update` | กลับค่า x ของไอคอนตาม `flipX` ของ sprite (pivot ของ sprite นก/เป็ดอยู่กลางพอดี) — ไม่แก้ prefab |
| FB2-7 | ตัวพุ่งควรชนผู้เล่นตอนโจมตี (ต่อจาก FB2-3) | FB2-3 ทำให้มอนทุกตัวเดินทะลุได้ตลอด | `aff0d4ba` | `BaseMonster` : `BlocksPlayers` / `UpdatePlayerCollision` / `SetSolidWith` (ใหม่); `Enemy_KnightJay` : `BlocksPlayers`, `ApplyVisualState` | Knight Jay ชนผู้เล่นเฉพาะตอนยืนเตรียมพุ่ง (0.6 วิ, ยืนนิ่ง); ยืนเฉยเดินทะลุได้; ตอนพุ่งตัวยังทะลุ (เหมือนเดิมตั้งแต่ NJ-8) hitbox ตี + กระเด็น — ไม่ให้ตัวชนตอนพุ่งเพราะพุ่งด้วยการย้ายตำแหน่งทีละ tick จะดันผู้เล่นทะลุพื้นแบบ FB2-3; ผู้เล่นที่ยืนซ้อนตัวมอนอยู่ตอนเปลี่ยนเป็นตันจะทะลุต่อจนกว่าจะเดินออก (ไม่ถูกดีด); คำนวณทุกเครื่องจาก state ที่ sync (`isPreparing`) — Puffy Jay ไม่เปลี่ยน |
| FB2-8 | ไอคอนมึนอยู่ผิดที่ตอนเป็ดดำน้ำ (ต่อจาก FB2-5) | ท่าว่ายวาดหันคนละทางกับท่าอื่น (`UpdateSwimFlip`) และตัวนอนราบ | `cae95e50` | `CharacterAnimation.IsSwimPose` (ใหม่); `PlayerGUI.AlignStunIcon` | ตอนเล่นท่าว่ายใช้ตำแหน่งแยก `stunIconSwimOffset` (28, 35) กลับค่า x ตาม `flipX` ในทิศตรงข้ามกับท่าอื่น — วัดจากภาพเรนเดอร์ท่าว่าย 3 เฟรม × 2 ทิศผ่าน Unity MCP |
| FB2-6 | dialogue ปิดไม่ได้ถ้าอีกคนยังไม่กด | ออกแบบให้โหวต (ปิดเมื่อพร้อมครบ) แต่ไม่มีอะไรบอกว่ารออีกคน | `f1acd292` | `DialogueVoteManager.Render` (ใหม่), `DialogueHUB.SetVoteStatus` / `CreateVoteStatusText` (ใหม่), `CloseDialogue` | อ่าน `HostReady`/`ClientReady` ที่ sync อยู่แล้ว → ข้อความเล็กข้างปุ่มเลื่อนบรรทัด: คนที่กดแล้วเห็น "Waiting for partner... (1/2)", อีกคนเห็น "Partner is ready (1/2)"; สร้างด้วยโค้ด ใช้ font เดียวกับ dialogue; ภาษาอังกฤษอย่างเดียว เพราะ dialogue ทุกฉากเป็นอังกฤษ (`isThaiLanguage` = 0 ทั้งหมด) และ font `FredokaOne` ไม่มีตัวไทย |

### 14.2 หมายเหตุ
- FB2-2: `RockObject.AlreadyThrow` ไม่มีที่ไหนตั้งเป็น true — หินที่ถูกปาดูจาก `ThrowerId` (ตั้งใน `Bird_Moveset.ExecuteThrow`)
- FB2-4: controller `HardRockBreak` มีพารามิเตอร์ `IsVer1` แต่ไม่มีโค้ดตั้งค่า → ใช้ clip `Version2` เสมอเหมือนเดิม
- FB2-5: ตอนเป็ดดำน้ำ sprite หันกลับด้าน (`UpdateSwimFlip`) — แก้ต่อใน FB2-8
- FB2-7: Knight Jay ไม่มีช่วง cooldown ที่มองเห็นตัว — หลังพุ่งจะหายไป `respawnTime` (5 วิ) แล้วกลับมาที่จุดเดิม; "ติด cooldown" จึงตีความเป็นตอนยืนเฉย (เจ้าของโปรเจกต์เลือก)
- FB2-7: ฝั่ง client เห็นมอนเปลี่ยนเป็นตันช้ากว่า host เล็กน้อย (interpolation) ถ้าเดินเข้าตัวมอนพอดีตอนเริ่มเตรียมพุ่งอาจถูกดึงกลับนิดหน่อย (LP-2 เกลี่ยภาพให้)

### 14.3 Checklist เทส — branch `test/all2` (MPPM `DuoGamePlay_Test`, Host = Mira, Client = Kael) ~10 นาที
| ขั้น | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|
| 1 กับดักหมี | เดินเข้ากับดักที่หุบอยู่ (หลังมันงับไปแล้ว) / ยืนตอนมันงับ | หุบอยู่ไม่โดน / ตอนงับโดน | |
| 2 หินที่วาง | เดินผ่าน/ยืนบนหินที่วางอยู่ แล้วกด E เก็บ | เดินทะลุ ไม่กระตุก, เก็บได้ | |
| 3 ปาหิน | นกปาหินใส่เป็ด / ใส่มอน | เป็ดมึน / มอนตาย เหมือนเดิม | |
| 4 มอนชน | ให้เป็ดมึนแล้วให้ Puffy Jay เข้าหา, เดินผ่านมอน | ไม่ทะลุพื้น จอไม่เพี้ยน; เดินทะลุตัวมอนได้แต่ยังโดนดาเมจ | |
| 5 ทุบหิน | เป็ดทุบหินแตกได้ (ทั้ง Host และ Client) | หินร้าว/แตกตรงจังหวะค้อนลง ของหล่นปกติ ทุบซ้ำไม่ได้ | |
| 6 ไอคอนมึน | ให้เป็ดมึนตอนหันซ้ายและหันขวา | ไอคอนอยู่เหนือหัวทั้ง 2 ทิศ | |
| 4b ตัวพุ่ง | เข้าใกล้ Knight Jay ตอนยืนเฉย (ยังไม่เห็นเรา) / ตอนยืนเตรียมพุ่ง / ตอนพุ่ง | ยืนเฉยเดินทะลุได้ / เตรียมพุ่งชนตัน / พุ่งโดนตี + กระเด็น; ยืนซ้อนตัวมันอยู่ตอนเริ่มเตรียมพุ่งไม่ถูกดีดทะลุพื้น | |
| 4c ตัวระเบิด | เดินผ่าน Puffy Jay แล้วยืนใกล้ตอนมันระเบิด | เดินทะลุได้ตลอด, ระเบิดโดนดาเมจ | |
| 6b ไอคอนมึนใต้น้ำ | นกปาหินใส่เป็ดตอนดำน้ำ ทั้งหันซ้าย/ขวา | ไอคอนอยู่เหนือหัวเป็ด | |
| 7 dialogue | คนหนึ่งอ่านจบ/กด Tab ก่อน | คนที่กดเห็น "Waiting for partner... (1/2)", อีกคนเห็น "Partner is ready (1/2)"; อีกคนกดแล้วปิดทั้งคู่ ข้อความหายไป | |

## 15. Feedback เทส 0.9 (2026-10-18)

**ที่มา:** เทส build 0.9 (`test/all2`) — เล่นจบด่านได้ เกมลื่นขึ้น ("ต้องลองให้คนอื่นเล่นเผื่อกระตุก"); เจอ 4 เรื่องด้านภาพ/UI + กับดัก
**การตัดสินใจของเจ้าของโปรเจกต์:** "ปุ่มสคริป" = ปุ่ม Skip คัตซีน; หน้าสอนเล่นให้รอบทพูดจบ (แก้ในโค้ด ไม่ย้ายจุด trigger)
**Branch:** `fix/feedback-1018` แตกจาก `fix/feedback-1017` (`cae95e50`) + cherry-pick commit "update" ของเจ้าของโปรเจกต์ 2 ตัว (`8c5b55f2` ← `01c760b3`: DOTween 1.0.435 / ai.assistant / GDD_AUDIT.md / CLAUDE.md, `bb69528a` ← `7406e581`: คืน font atlas) — ยังไม่ merge เข้าสายหลัก, **ยังไม่ได้เทสตอนรัน**; merge เข้า `test/all2` แล้ว
**Compile:** Roslyn 0 error ทุก commit + Unity 0 error (Unity MCP); prefab/scene แก้ผ่าน Unity MCP ทั้งหมด

### 15.1 สิ่งที่แก้
| รหัส | อาการ | ต้นเหตุ | commit | ไฟล์ : method | เปลี่ยนอะไร |
|---|---|---|---|---|---|
| FB3-1 | ปุ่ม Skip คัตซีนค้างทั้งเกม เฉพาะฝั่ง client | client เปิดปุ่ม Skip ใน `Setup()` ที่รอ `LevelData`/`PlayerInterface` ก่อน; S2/S3 ไม่มีไฟล์คัตซีน host จบทันที → `RPC_EndCutsceneAll` ถึง client ก่อน `Setup()` (หรือก่อน object spawn แล้วหาย) → ปุ่มถูกเปิดทีหลังและไม่มีอะไรปิด | `848b3218` | `CutsceneManager` : `CutsceneEnded` (ใหม่), `EndCutscene` / `OnCutsceneEndedChanged` (ใหม่), `Setup`, `OnVideoError`, `RPC_SubmitSkipVote`, `OnVideoPlaybackFinished` | การจบคัตซีนเป็น `[Networked] CutsceneEnded` ที่ host ตั้ง (จบเอง / วิดีโอ error / โหวต Skip ครบ) ทุกเครื่องปิดคัตซีนจาก `OnChangedRender`; `Setup()` ที่มาช้าจะปิดทันทีไม่เปิดปุ่ม; ลบ `RPC_EndCutsceneAll` (RPC 58 → **57**) |
| FB-7 | พื้นและประตูไม้ S3 ล่องหน (ชน/กดได้ปกติ) | ผนังในต้นไม้ = tilemap `Tree-Tiles` (`BGLayer`) วาดด้วยกล้องเดียวกับของในเกม; ของที่อยู่ sorting layer `Default` (หลัง `BGLayer`) จึงหายตรงที่ผนังทึบ | `713f4f96` | `Stage1-S3.unity` (ผ่าน Unity MCP) | `Scale_Door` → `SortingTexture` (เหมือนประตูอื่นในฉาก); `platfrom_1` ×8 → `PlatformLayer` (เหมือนแพลตฟอร์มอื่น 23 ชิ้น); หนาม `Trap_Multi_Thorn` 4 ชุดใต้ `platfrom_1 (8)` / `Rotate_platfrom` → `TrapLayer` (เดิมก็ล่องหน = หนามที่มองไม่เห็น); order เดิม; ยืนยันด้วยภาพจากกล้อง mask เดียวกับ Camera Sharp ก่อน/หลัง — ตาชั่ง (`WeighingScale`) มองเห็นอยู่แล้ว ไม่แตะ, `Tree-Tiles` ไม่แตะ |
| FB3-2 | หน้าสอนเล่นขึ้นพร้อมบทพูด (ภาพจากวี่) | `TutorialTrigger` เปิดหน้าสอนเล่นทันทีไม่ดูบทพูด และทั้งสองปิดด้วย Tab → กด Tab ครั้งเดียวปิดสอนเล่น + โหวตปิดบทพูด; เพิ่มเติม: ปุ่ม Tab ใน `MovementCharacter` ปิดหน้าสอนเล่นจาก input ของตัวไหนก็ได้ (บน host = client กด Tab แล้วหน้าสอนเล่นของ host ปิด) | `dc3b0833` | `TutorialUIManager` : `ShowTutorialPanel`, `Update`, `HideTutorial`; `MovementCharacter` : ส่วนกด Tab | ระหว่าง `DialogueManager.IsDialogueOpen` หน้าสอนเล่นเข้าคิวรอ เปิดเมื่อบทพูดปิด; บทพูดเริ่มตอนสอนเล่นเปิดอยู่ → ซ่อนแล้วกลับมาหน้าเดิม (ล็อค Tab ต่อถ้ายังไม่ครบเวลา); `HideTutorial` ล้างคิว; Tab ใน `MovementCharacter` ทำงานเฉพาะเครื่องเจ้าของตัว (`HasInputAuthority && Runner.IsForward`) |
| FB3-3 | กับดักน้ำแข็งไม่มีดาเมจ | ดาเมจมาจาก `DoDamage.OnTriggerEnter2D` บน `Trap_HitBlock` ต้องมี trigger: `Small_Ice` ถูกเปลี่ยน trigger เป็น polygon ตัน (`5a673e78`, 05-22), `Ice` ถูกลบ collider ทั้งหมด (`66ff68be`, 06-25) | `1dc64bd7` | `Small_Ice.prefab`, `Ice.prefab` (ผ่าน Unity MCP) | เพิ่ม `PolygonCollider2D` isTrigger ที่ `Trap_HitBlock`: `Small_Ice` ใช้รูป polygon ตันเดิม, `Ice` ใช้รูป polygon ก่อน 06-25 — ขยาย 6% จากจุดกึ่งกลางให้แตะผิวก็โดน (ภาพตรวจกรอบทับรูปน้ำแข็งพอดี); `Small_Ice` ยังตัน, `Ice` ยังเดินทะลุ (ตามที่ทีมทำไว้); ฉาก S1/S2 (`Ice` ×11, `Small_Ice` ×26) ไม่มี override จึงได้ทั้งหมด |

### 15.2 หมายเหตุ
- `Trap_Ice.OnCollisionStay2D` บน root ของ prefab น้ำแข็ง/หนามไม่เคยทำงาน (root ไม่มี collider/rigidbody) — ไม่แตะ ดาเมจมาจาก `DoDamage` อย่างเดียว
- ใน S3 ยังมีของบน `Default` อีก (คบเพลิง, `Water + Shade (ForTest)`, ของตกแต่งบางชิ้น, ตัวมอนใน `=== Monster ===`) — จากภาพตัวมอนและตาชั่งมองเห็น จึงไม่แตะในรอบนี้ ถ้าเทสแล้วเจอของหายอีกให้แจ้งตำแหน่ง
- working tree มี `ProjectSettings.asset` / `DOTweenSettings.asset` ที่ DOTween Pro ใส่ define `DOTWEENPRO` เอง (หลังอัปเดต DOTween) — ไม่ได้ commit ให้เจ้าของโปรเจกต์ตัดสิน
- `Stage1-S3.unity` เก็บด้วย Git LFS

### 15.3 Checklist เทส — branch `test/all2` (MPPM `DuoGamePlay_Test`, Host = Mira, Client = Kael) ~10 นาที
| ขั้น | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|
| 1 Skip | เริ่มเกม S1 → ทั้งคู่กด Skip; แล้ว Host **F8** ไป S2 และ S3 | S1 Skip ได้ (โหวต 2/2); S2/S3 จอ **client** ไม่มีปุ่ม Skip ค้าง | |
| 2 S3 | ไปบริเวณตาชั่ง S3 + แพลตฟอร์มหิมะในต้นไม้ | เห็นประตูไม้ในช่องกำแพงข้างตาชั่ง, แพลตฟอร์ม `platfrom_1` ทุกอัน, หนามใต้แพลตฟอร์ม; ผู้เล่นยังมองเห็นตอนยืนบนแพลตฟอร์ม | |
| 3 สอนเล่น | จุดที่วี่แคป (สอนเล่น + บทพูดพร้อมกัน) | บทพูดขึ้นก่อน หน้าสอนเล่นขึ้นหลังบทพูดปิด; กด Tab ปิดทีละอัน; client กด Tab แล้วหน้าสอนเล่นของ host ไม่ปิด | |
| 4 น้ำแข็ง | แตะ/กระโดดชนน้ำแข็งย้อย `Small_Ice` และ `Ice` ใน S1–S2 | โดนดาเมจ + กระเด็น (หลังโดนมีช่วงอมตะ 1.5 วิ); หนาม/น้ำแข็งย้อย S3 เหมือนเดิม | |

## 16. แพลตฟอร์มกระตุก + ท่าทางเร็วฝั่ง client (2026-10-21)

**ที่มา:** เทส `test/all2` — (1) moving platform ทุกแบบวาร์ปไปมาบนจอ client แม้ไม่มีใครยืน (2) ท่าทางของตัวที่ client คุมเล่นเร็ว/แปลก เช่นเป็ดกระโดด (3) error `GetBool can only be called from the main thread` และ `DisconnectByServerTimeout`
**Branch:** แก้บน `test/all2` โดยตรง (เจ้าของโปรเจกต์สั่ง) — **ยังไม่ได้เทสตอนรัน**; compile Roslyn 0 error (Unity MCP ต่อไม่ได้ตอนแก้ → ต้องดู Console เอง)

### 16.1 สิ่งที่แก้
| รหัส | อาการ | ต้นเหตุ (จากโค้ด) | commit | ไฟล์ : method | เปลี่ยนอะไร |
|---|---|---|---|---|---|
| NR-10 | แพลตฟอร์มวาร์ปไปมาบนจอ client | `MovingPlateform` คำนวณตำแหน่งจากเวลาเองทุกเครื่อง (NR-3) แต่ prefab ยังมี `NetworkRigidbody2D` ซึ่งฝั่ง client ทับ root ด้วยค่าจาก host ตอน resimulate และเขียน root อีกรอบตอน `Render` จาก buffer interpolation — สองแหล่งเขียนตำแหน่งเดียวกัน | `4f85c8a6` | `MovingPlateform` : `Awake` (ใหม่), `IBeforeTick.BeforeTick` (ใหม่), `FixedUpdateNetwork`, `Render` (ใหม่), `PositionAt` (ใหม่) | ตำแหน่งเป็นฟังก์ชันของ tick ล้วน (`tick × DeltaTime`) ไม่ต้อง sync: ต้น tick (รวม resimulate) วางแพที่ตำแหน่งปลาย tick ก่อน → FUN `MovePosition` ไปปลาย tick นี้ → `Render` วาดที่เวลา local ระหว่าง 2 tick ล่าสุด; `Awake` ตั้ง `InterpolationTarget` ของ `NetworkRigidbody2D` เป็น child เปล่า ให้มันไม่เขียน root อีก (แก้ด้วยโค้ด ไม่ถอด component จาก prefab เพราะฉากอ้างลำดับ behaviour) — ใช้กับ `moving_platfrom` (S1×5, S3×1), `Trap_Mix`, `Trap_Mix_2` (S2) |
| NR-11 | ท่าตัวเราฝั่ง client เร็ว/กระพริบ (เป็ดกระโดด) | เลือกท่าจากความเร็วของภาพ ซึ่งฝั่ง client กระโดดทุกครั้งที่ host แก้ตำแหน่ง (อ่านก่อน LP-2 เกลี่ย) → สลับ Jump/Falling/ยืน ถี่ ๆ แต่ละครั้งเริ่ม clip ใหม่; และช่วง apex (`isJumping` ล้างที่ vy < -0.1 แต่ Falling ต้อง vy < -0.5) ตกไปท่ายืนแวบหนึ่ง | `8e378774` | `CharacterAnimation` : `Render`, `ResolveState`, `IsAirborne` (ใหม่) | ตัวที่จำลองในเครื่องนี้ (ตัวเราเอง / ทุกตัวบน host) ใช้ `rb2D.linearVelocity` เลือกท่า, proxy ใช้ความเร็วของภาพเหมือนเดิม; ท่ากลางอากาศค้างไว้จนแตะพื้น/กระโดดใหม่ (ไม่กลับท่ายืนกลางอากาศ); ยืนบนแพลตฟอร์มเลื่อน = ท่ายืน |
| (log) | — | — | `ecb6e7f2` | `CharacterAnimation.Render` | `[VERIFY] anim …` ทุกครั้งที่ท่าของตัวเราเปลี่ยน (frame, เวลา, ท่า, vy จำลอง/ภาพ, isJumping, IsGrounded) — **test เท่านั้น ห้าม merge** |

### 16.2 ไม่แก้ (อธิบาย)
- **`GetBool can only be called from the main thread`:** มาจากเครื่องมือ Editor ไม่ใช่เกม — `com.unity.ai.assistant` (MCP bridge) เขียน log จาก thread pool ตอนตรวจการเชื่อมต่อ แล้ว MPPM (`StandardCloneWorkflow.OnLogMessageReceived`) อ่าน `SessionState` นอก main thread; ไม่มีใน build
- **`DisconnectByServerTimeout`:** timeout ของ Fusion = `NetworkProjectConfig` → `Network.ConnectionTimeout` = **10 วินาที**; error นี้คือ server ของ Photon ไม่ได้รับอะไรจากเครื่องนั้นเกินเวลา — ใน log ฝั่งที่หลุดคือหน้าต่างที่อยู่เบื้องหลัง (`AppOutOfFocus`) ระหว่างเทส MPPM; Player Settings `runInBackground = 1` อยู่แล้ว build จริงสลับหน้าต่างไม่ควรหลุด → ไม่แก้ค่า (แนะนำ: ตอนเทสอย่าย่อหน้าต่าง Editor/Player 2)
- เจอระหว่างตรวจ (ไม่ได้แก้): เสียงกระโดดใน `MovementCharacter.HandleJump` gate แค่ `HasInputAuthority` ไม่มี `Runner.IsForward` → ฝั่ง client อาจได้ยินซ้ำตอน resimulate

### 16.3 Checklist เทส — `test/all2` (MPPM, Host + Client) ~10 นาที
| ขั้น | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|
| 0 | เปิด Unity ดู Console หลัง compile | 0 error | |
| 1 แพลตฟอร์ม | จอ **client**: ยืนดูแพลตฟอร์ม S1 (ไม่ขึ้นยืน) แล้ว S2 `Trap_Mix` | เลื่อนลื่น ไม่วาร์ป | |
| 2 ยืนบน | client ขึ้นยืนบนแพลตฟอร์มแนวนอน/แนวตั้ง | ไม่ถูกดึงกลับ ไม่ตก ไม่จม; ลมบน `Trap_Mix_2` ยังดัน | |
| 3 ท่ากระโดด | จอ **client**: ตัวเรากระโดด-ตก-ลงพื้นหลายครั้ง, เดินตกขอบ | Jump → Falling → ยืน ต่อเนื่อง ไม่เร็ว/ไม่กระพริบ; ยืนบนแพลตฟอร์มเลื่อน = ท่ายืน | |
| 4 host | ทำข้อ 1–3 บนจอ host | เหมือนเดิม | |
| 5 lag | เปิด Network Conditions แล้วทำข้อ 1–3 | แพลตฟอร์มยังลื่น; ท่าไม่สลับถี่ | |
| 6 ถ้ายังเร็ว | ส่ง Editor.log | ผมอ่าน `[VERIFY] anim` ดูว่าสลับท่าตรงไหน | |

## 17. NR-12 เจ้าของคุมการเคลื่อนที่ตัวเอง (2026-10-21)

**ที่มา:** จอ client — ยกเพื่อน / ยืนแพลตฟอร์มเลื่อน / บินออกจากการยกแล้วชนขอบ → ตัวละครวาร์ป-โยก บางทีตกแพลตฟอร์ม; จอ host ปกติ
**ต้นเหตุ:** โมเดลเดิม host เป็นเจ้าของทุกตัว client ทำนายตัวเองแล้วถูก host แก้ — แต่ "เพื่อน" บนจอ client เป็นภาพย้อนหลัง (ไม่มี input ของเพื่อนให้ทำนาย) และแกนเกมคือการยกเพื่อน → host/client คำนวณต่างกันเสมอเมื่อสองตัวผูกกัน → ถูกดึงกลับ; เสริมด้วยโค้ดยก/ปล่อยที่แก้สถานะตัวเพื่อนระหว่างทำนายแล้วไม่ถูกย้อน
**การตัดสินใจของเจ้าของโปรเจกต์:** เลือก "เจ้าของคุมตัวเอง บน Host mode" (แทนย้ายไป Shared Mode ซึ่งต้องแก้ทั้งเกม — เทียบไว้ในบทสนทนา 2026-10-21) งบ ~8 วัน
**Branch:** `feature/owner-movement` แตกจาก `test/all2` — `test/all2` เป็นทางสำรอง; **ยังไม่ได้เทสตอนรัน**; Roslyn 0 error

### 17.1 โมเดลใหม่
| เครื่อง / ตัว | ทำอะไร |
|---|---|
| เจ้าของตัว (input authority) | จำลองการเคลื่อนที่ตัวเอง (Rigidbody2D dynamic) เฉพาะ tick ใหม่ ไม่ถูกแก้/ย้อน; client ตั้ง `ClientPhysicsSimulation = SimulateForward` |
| host กับตัวของ client | kinematic (`useFullKinematicContacts` ให้ trigger กับดัก/จุดตาย/checkpoint ยังทำงาน) วางตาม pos ที่ client ส่งมาใน input ทุก tick |
| client กับตัวของ host | kinematic, interpolate จาก `RepPosition` |
| host (ทุกตัว) | ตัดสินโลกเหมือนเดิม: E/ไอเท็ม/ทิ้ง, มึน, ดาเมจ, ตาย/เกิดใหม่, ยก/ปล่อย, ปาหิน, ดาเมจจมน้ำ |

- สถานะการเคลื่อนที่ (`IsGrounded`, น้ำ, กระโดด, ปีน, ดำน้ำ, บิน, ทิศหัน ฯลฯ) เป็นของเจ้าของ — property เดิมอ่าน/เขียนธงในเครื่องเจ้าของ เครื่องอื่นอ่าน `RepFlags` ที่ host sync
- ผลจาก host ที่ต้องกระทบตัวเจ้าของ (แรงกระเด็น, วาร์ปตอนเริ่มด่าน/เกิดใหม่/รีเซ็ต, ถูกปล่อย/ถูกโยน) ส่งเป็น event (`EventSeq` + ตำแหน่ง/แรง) เจ้าของทำแล้วตอบรับผ่าน input; host ไม่รับตำแหน่งจาก client จนกว่าจะตอบรับการวาร์ป
- ภาพของเจ้าของ interpolate ระหว่างตำแหน่งปลาย 2 tick ล่าสุด; ลบ LP-2 (ไม่มีการแก้ตำแหน่งแล้ว)
- `NetworkRigidbody2D` ยังอยู่ใน prefab ผู้เล่นแต่วาดลง child เปล่า และ client คืนค่า body ของตัวเองก่อนทุก tick (กันมันตั้งกลับเป็นค่าจาก host)

### 17.2 ไฟล์ที่แก้ — commit `d13a907a`
| ไฟล์ | เปลี่ยนอะไร |
|---|---|
| `MovementCharacter.cs` | region "Owner Movement": ธง/`Rep*`, `OwnerSimulate` / `HostRules` / `ApplyOwnerReport`, event host→owner, คืนค่า body ฝั่ง client (`IBeforeTick`/`IAfterTick`), ภาพเจ้าของ/ตัวอื่น; ยก/ปล่อยผ่าน `HostSetCarried`/`HostReleaseCarried` (ลบ `RPC_UpdateCarry` และการทำนายยกฝั่ง client → RPC 57 → **56**); กระเด็น/เกิดใหม่/รีเซ็ตเป็น event |
| `Duck_Moveset.cs` | น้ำ/ดำน้ำ/ลอยน้ำเป็นของเจ้าของ; ยก/ปล่อยที่ host; collider ตอนยกตาม `IsCarry` ทุกเครื่อง; นกที่ถูกยกบิน → เป็ดลอยตัวเอง; oxygen หมด → ธงให้ host ทำดาเมจ |
| `Bird_Moveset.cs` | บิน/ลอย/จมน้ำ (การเคลื่อนที่) เป็นของเจ้าของ; ปาหิน + จับเวลา oxygen ที่ host; เล็ง/เหวี่ยง → ล็อกการเดินของเจ้าของ; เสียงปาฝั่ง client |
| `CharacterAnimation.cs` | `FlipX` เป็นธงของเจ้าของ; ความเร็วเลือกท่า = ของจริง (เจ้าของ) / `RepVelocity` (ตัวอื่น); ท่าครั้งเดียวตั้งที่ host |
| `INetworkStructure.cs` | input เพิ่ม `OwnerPosition`, `OwnerVelocity`, `OwnerFlags`, `OwnerAckSeq` (ส่งทุก tick แม้ปุ่มถูกปิดระหว่าง dialogue) |
| `TrapPressure.cs` | แรงลมใส่ให้ตัวของเจ้าของเท่านั้น เฉพาะ tick ใหม่ |
| `SnowBallProjectile.cs` | เลิกให้ client จำลอง (ใช้ภาพจาก host) |
| `SessionManager.cs` | client ตั้ง `SimulateForward` ก่อน `StartGame` |

### 17.3 ข้อจำกัดที่ยอมรับ
- ผลจาก host ที่กระทบตัว client (เริ่มถูกยก, กระเด็น, มึน, ท่ากด E) มาช้า ~ครึ่ง RTT (ไม่ทำนายแล้ว แต่ไม่ดีด)
- ผลักวัตถุฟิสิกส์ของ host ฝั่ง client จะหน่วงตาม ping
- client ส่งตำแหน่งเอง (โกงได้) — ไม่สำคัญสำหรับ co-op 2 คน

### 17.4 Checklist เทส — `feature/owner-movement` (MPPM, Host + Client, ลองทั้ง Host เป็นเป็ดและเป็นนก) ~20 นาที
| ขั้น | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|
| 0 | เปิด Unity ดู Console | 0 error | |
| 1 เดิน | ทั้งคู่เดิน กระโดด ตก ปีนเถา ในจอตัวเองและจออีกฝั่ง | ตัวเราไม่ดีด; อีกตัวเลื่อนลื่น; ท่าถูก | |
| 2 แพลตฟอร์ม | client ยืน/เดินบนแพลตฟอร์มเลื่อน (แนวนอน+แนวตั้ง) และ `Trap_Mix` S2 | ไม่โยก ไม่ตก | |
| 3 ยก | เป็ดยกนก เดิน/กระโดดไปมา (สลับว่าใครเป็น client) | ทั้งคู่ไม่โยก; นกติดหัวเป็ด | |
| 4 ปล่อย/โยน | เป็ดกด E ปล่อย/โยนนก | นกออกทางที่หันอยู่ ไม่วาร์ปกลับ | |
| 5 บินตอนถูกยก | นกที่ถูกยกกดบิน แล้วชนขอบ/เพดาน (เคสที่เจอ) | เป็ดลอยขึ้น ไม่วาร์ปกลับไปจุดชน | |
| 6 ดาเมจ | โดนมอน / หนาม / น้ำแข็ง / ลูกหิมะ | เลือดลด + กระเด็น 1 ครั้ง | |
| 7 มึน | นกปาหินใส่เป็ด | เป็ดมึน ขยับไม่ได้จนหมดเวลา | |
| 8 ตาย/เกิด | ตกเหว/เลือดหมด, Esc → Reset | ทั้งคู่ตาย → เกิดที่ checkpoint ขยับได้ปกติ | |
| 9 น้ำ | เป็ดดำน้ำจน oxygen หมด, นกตกน้ำ | แถบ oxygen, ดาเมจจมน้ำ, ขึ้นจากน้ำแล้วหยุด | |
| 10 ลม/ปา | ยืนในลม `TrapPressure`; นกเก็บหินแล้วเล็ง-ปา | ลมดัน; ปาได้ (ฝั่ง client มีเสียงปา) | |
| 11 กด E | ประตู ไอเท็ม หินทุบ ปุ่ม ตาชั่ง | ทำงานเหมือนเดิม | |
| 12 ทั่วไป | dialogue, cutscene, ไปด่านถัดไป, กลับ lobby | เหมือนเดิม | |
| 13 lag | เปิด Network Conditions แล้วทำข้อ 1–5 | ตัวเรายังลื่น ไม่ดีด | |

## 18. MV ความรู้สึกการเคลื่อนที่ (feedback advisor 2026-10-24)

**ที่มา:** Advisor ลองเล่นเองแล้วบอกว่า "การขยับยังไม่ได้" ให้เป็น issue 1 และเสนอให้เปลี่ยนจาก Rigidbody2D เป็น translate — เวอร์ชันที่เล่นคือเวอร์ชันที่โชว์ในงาน (ก่อนเริ่มแก้)
**ตรวจแล้ว:** โค้ดและค่าการเดิน/กระโดดเหมือนกันทุกตัวตั้งแต่ `main` (2026-06-08), `dev`, `branch-UpdateSession` จนถึง `feature/owner-movement` → ปัญหาความรู้สึกยังอยู่ในเวอร์ชันปัจจุบัน (ส่วนอาการ network ของเวอร์ชันเก่า — ตัวเราดีด/หน่วงบนจอ client, แพลตฟอร์มสั่น, ท่าเร็ว — แก้แล้วใน NR-10/11/12)
**การตัดสินใจของเจ้าของโปรเจกต์:** ไม่รื้อเป็น translate (ต้องเขียนการชนเองและรื้อน้ำ/บิน/ยก/ลม/แพลตฟอร์ม ~5–7 วัน) — คง Rigidbody2D แต่ตั้งความเร็วตรง + ชุดตัวช่วยแบบเกม platformer ใหญ่ (Hollow Knight, Celeste)
**Branch:** `fix/movement-feel` แตกจาก `feature/owner-movement` — Roslyn 0 error; **ยังไม่ได้เทสตอนรัน**

### 18.1 ต้นเหตุและการแก้ — commit `4c3a2e09`
| รหัส | ต้นเหตุ | การแก้ | แก้แล้ว |
|---|---|---|---|
| MV-1 | `HandleMovement` ใช้ `AddForce(speedDif × acceleration)` โดย acceleration = 3 → ~1 วินาทีกว่าจะถึง maxSpeed; friction 0.4 ของพื้นกินความเร็วบนพื้น (~2.4 จาก 5 ตามการคำนวณ) แต่กลางอากาศไม่มี → กระโดดแล้วพุ่ง ลงพื้นแล้วหนืด | ตั้งความเร็วแนวนอนตรง (`MoveTowards`) ตามเวลาเร่ง/หยุด, กลางอากาศคูณ `airControl`, เร็วเกิน maxSpeed ไปทางที่กดอยู่ → ลดช้า ๆ | แก้แล้ว: `fix/movement-feel` `4c3a2e09` |
| MV-2 | กระโดดด้วยปุ่มค้าง + ต้อง `IsGrounded` พอดี tick นั้น; ไม่มี coyote time / jump buffer; `JumpCooldownTimer` ใน prefab = 1 วินาที → ลงพื้นแล้วกดซ้ำไม่ติด | กระโดดตามจังหวะกด + jump buffer 0.1 วิ + coyote time 0.1 วิ; cooldown 0.1 วิ (field ใหม่ `jumpCooldownTime` แทน `JumpCooldownTimer`); นกไม่เริ่มบินใน tick ที่เพิ่ง coyote jump และล้างปุ่มที่จำไว้ตอนเริ่มบิน | แก้แล้ว: `fix/movement-feel` `4c3a2e09` |
| MV-3 | เช็คพื้นด้วย ray เส้นเดียวจากกลางตัว → ยืนเกินขอบครึ่งตัวแล้วไม่นับว่ายืน กดกระโดดไม่ไป | ray 3 เส้น (กลาง/ซ้าย/ขวา ที่ ±80% ของครึ่งความกว้าง collider) ระยะเดิม | แก้แล้ว: `fix/movement-feel` `4c3a2e09` |
| MV-4 | ตัวละครมี friction ตอนลอย → ดันเข้าหากำแพงกลางอากาศแล้วหนืด/ติด | `normalCollider` ใช้ material ไม่มี friction เฉพาะตอนลอย ยืนแล้วกลับไปใช้ของ Rigidbody2D (ที่นกสลับเองตอนบิน) | แก้แล้ว: `fix/movement-feel` `4c3a2e09` |
| MV-5 | ตั้งความเร็วตรงจะทับแรงลม แรงกระเด็น และการถูกโยน | ใช้สูตรเดิม (AddForce) เมื่อ: ในน้ำ/ร่อน/ลอย (`isSpeedoptional` / `isOptional`), อยู่ในลม (`TrapPressure` เรียก `NotifyExternalForce`), หลังกระเด็น/ถูกโยนจนลงพื้น → ลม ระยะกระเด็น ระยะโยน เท่าเดิม ไม่ต้องจูนด่าน | แก้แล้ว: `fix/movement-feel` `4c3a2e09` |

### 18.2 ค่าที่จูนได้ — Inspector ของ `Duck_Moveset` / `Bird_Moveset` หัว "Movement Feel (MV)"
| ค่า | เริ่มต้น | ปรับแล้วเป็นยังไง |
|---|---|---|
| `groundAccelTime` | 0.06 | วินาทีจากหยุดจนถึงความเร็วเต็ม — มาก = ออกตัวหนักขึ้น |
| `groundDecelTime` | 0.05 | วินาทีจากความเร็วเต็มจนหยุด — มาก = ไถลก่อนหยุด |
| `airControl` | 0.65 | คุมกลางอากาศได้กี่ส่วนของบนพื้น — 1 = เท่าบนพื้น |
| `overSpeedReduce` | 0.4 | หลุดจากลมแล้วเก็บโมเมนตัมนานแค่ไหน — น้อย = นานขึ้น |
| `coyoteTime` | 0.1 | เดินพ้นขอบแล้วยังกระโดดได้กี่วินาที |
| `jumpBufferTime` | 0.1 | กดกระโดดก่อนแตะพื้นได้ล่วงหน้ากี่วินาที |
| `jumpCooldownTime` | 0.1 | เวลาขั้นต่ำระหว่างการกระโดดสองครั้ง |
| `externalForceTime` | 0.25 | หลังโดนกระเด็นใช้การคุมแบบเดิมอย่างน้อยกี่วินาที |
| `groundCheckWidth` | 0.8 | ความกว้างที่ใช้เช็คพื้นเทียบกับตัว |
| `CharacterStats.maxSpeed` | 5 | ความเร็วสูงสุด — ตอนนี้วิ่งได้เต็ม 5 จริง (เดิมบนพื้นได้ราว 2.4) ถ้ากระโดดข้ามช่องง่ายไปให้ลด |

วิธีจูน: Play Mode → Hierarchy เลือกตัวละครของ Host → แก้ค่าแล้วลองเล่นทันที → จดค่าที่ชอบ → ออก Play Mode แล้วใส่ใน prefab `Assets/Resources/Prefabs/Character_Prefabs/Player(Duck).prefab` และ `Player(Bird).prefab` (ค่าที่แก้ระหว่าง Play Mode หายเมื่อออก)

### 18.3 ไม่เปลี่ยน
network (แก้เฉพาะฝั่งเจ้าของใน `OwnerSimulate` — ไม่เพิ่มอะไรที่ส่งผ่านเน็ต), prefab/scene, ว่ายน้ำ/ดำน้ำ/ร่อน, บิน/ยก/ปีน, แรงลม, ระยะกระเด็นและโยน

### 18.4 Checklist เทส — `fix/movement-feel` (MPPM, Host + Client, ลองทั้งตัวของ host และตัวของ client) ~15 นาที
| ขั้น | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|
| 0 | เปิด Unity ดู Console | 0 error | |
| 1 เดิน | กดซ้าย/ขวาบนพื้น ปล่อย กลับทิศ | ออกตัวทันที หยุดทันที; อีกจอเห็นเราเดินลื่นปกติ | |
| 2 ขอบ | ยืนเกินขอบ platform ครึ่งตัวแล้วกระโดด | กระโดดได้ | |
| 3 coyote | เดินพ้นขอบแล้วกดกระโดดทันที | ยังกระโดดได้ | |
| 4 buffer | กดกระโดดก่อนตกถึงพื้นนิดหนึ่ง; กดค้างไว้ | กระโดดทันทีที่แตะพื้น; กดค้างไม่เด้งซ้ำ | |
| 5 กระโดดรัว | ลงพื้นแล้วกดกระโดดซ้ำทันทีหลายครั้ง | กระโดดได้ทุกครั้ง (เดิมติด cooldown 1 วินาที) | |
| 6 กำแพง | กระโดดชิดกำแพงแล้วกดเข้าหากำแพง | ไม่ติด ตกลงปกติ | |
| 7 ลม | ลมแนวนอน S1/S3 และลมแนวตั้ง | แรงเท่าเดิม ฝ่าได้/ไม่ได้เหมือนเดิม | |
| 8 กระเด็น/โยน | โดนมอนตี; เป็ดโยนนก | ยังกระเด็น; ระยะโยนเท่าเดิม | |
| 9 น้ำ/ร่อน | เป็ดว่าย/ดำน้ำ; นกร่อนหลังบินหมด | ความรู้สึกเหมือนเดิม | |
| 10 นก | กระโดดแล้วกดบิน; เดินพ้นขอบแล้วกดทันที | บินได้ตามปกติ; กรณีพ้นขอบได้กระโดด ไม่เสียการบิน | |
| 11 ยก | นกยกเป็ด / เป็ดยกนก เดิน กระโดด | เหมือนเดิม | |
| 12 แพ/ปีน | ยืน-เดินบนแพลตฟอร์มเลื่อน; ปีนขึ้นลงแล้วกระโดดออก | ไม่ไถล; ปีนเหมือนเดิม | |
| 13 ด่าน | เล่นช่องกระโดดใน S1–S3 | ยากพอดี (ง่ายไป → ลด `maxSpeed`) | |

### 18.5 ผลเทสอัตโนมัติผ่าน Unity MCP (2026-10-24) — 14/14 ผ่าน, Console 0 error
วิธี: probe ชั่วคราว (ไม่ได้ commit, ลบแล้ว) รัน Host คนเดียวบน `fix/movement-feel` → เข้า Stage1-S1 (กด Skip คัตซีน + นับผู้เล่นคนที่สองแทน) → สร้างพื้น/ขอบ/กำแพงทดสอบที่ (1000, 1000) ไกลจากด่าน → ใส่ปุ่มทีละ tick ผ่าน `OnInput` แล้ววัดผล (ตัวเป็ด, maxSpeed 5, tick 60)
| เทส | ผล | ค่าที่วัด |
|---|---|---|
| T1 ออกตัว | ✅ | ถึง 95% ของความเร็วเต็มใน 4 tick (0.07 วิ) — เดิมประมาณ 60 tick |
| T1 ความเร็วบนพื้น | ✅ | 4.87 จาก 5 — เดิมราว 2.4 เพราะแรงเสียดทาน |
| T2 หยุด | ✅ | หยุดใน 3 tick |
| T3 กลับทิศ | ✅ | ขวาเต็ม → ซ้าย 95% ใน 8 tick |
| T4 กดค้าง | ✅ | กดค้าง 2.5 วิ กระโดด 1 ครั้ง (สูง 2.01) ไม่เด้งซ้ำ |
| T5 กระโดดซ้ำทันทีที่ลงพื้น | ✅ | 4 ครั้งติด ลงพื้น→กระโดด 1 tick ทุกครั้ง (เดิมติด cooldown 1 วิ) |
| T6 jump buffer (ใกล้) | ✅ | กดก่อนลงพื้น 1 tick → กระโดดทันทีที่แตะพื้น |
| T6 jump buffer (ไกล) | ✅ | กดก่อนลงพื้น 14 tick (เกิน 6) → ไม่กระโดด ถูกต้อง |
| T7 coyote 0.083 วิ | ✅ | เดินพ้นขอบแล้วกด → กระโดดได้ |
| T7 coyote 0.233 วิ | ✅ | เกิน 0.1 วิ → ไม่กระโดด ถูกต้อง |
| T8 ยืนเกินขอบ | ✅ | ray กลางตัวไม่โดนพื้นแต่ยังนับว่ายืน → กระโดดได้ |
| T9 ไม่ติดกำแพง | ✅ | ตกอิสระ 6.06 / กดเข้ากำแพง 6.63 หน่วยใน 38 tick |
| T10 แรงกระเด็นคงอยู่ | ✅ | ความเร็วแนวนอนหลัง 0.1 วิ = 1.67 (ถ้าตั้งความเร็วตรงจะเหลือ ~0), ไถลไป 0.49 |
| T11 กลับมาคุมได้หลังลงพื้น | ✅ | ถึง 95% ใน 4 tick |

ยังต้องเทสเองด้วย MPPM: ฝั่ง client (ตัวที่ client คุม), ลม `TrapPressure`, ว่าย/ดำน้ำ, นกบิน/ร่อน, ยก/โยน, แพลตฟอร์มเลื่อน, ปีน, ความยากของช่องกระโดดในด่าน (ข้อ 7–13 ของ 18.4)

## 19. Feedback เทส MV (2026-10-24): แพเลื่อน, เสียงกับดัก, หน่วงเมื่อเล่นนาน, ความเร็วเดิน

**ที่มา:** ผู้ใช้เทส `fix/movement-feel` แล้ว — การเคลื่อนที่ดีขึ้นมาก แต่ตัวละครเร็วไป; สั่งรวมเข้า `test/all2` แล้วจะเทสอีกรอบ
**Branch:** `fix/feedback-1024` (แตกจาก `fix/movement-feel`) → fast-forward เข้า `test/all2` แล้ว — Roslyn + Unity 0 error; เทสอัตโนมัติผ่าน MCP 19/19

### 19.1 ต้นเหตุและการแก้
| รหัส | อาการ / ต้นเหตุ | การแก้ | แก้แล้ว |
|---|---|---|---|
| MV-6 | เดินเร็วไป (เดินได้ 5 เต็ม) | field ใหม่ `runSpeed` = 3.5 (เลือกโดยเจ้าของโปรเจกต์) ใช้กับการคุมแบบตั้งความเร็วตรง; `CharacterStats.maxSpeed` = 5 คงไว้ให้สูตรเดิมในน้ำ/ร่อน/ลม/กระเด็น | แก้แล้ว: `fix/feedback-1024` `d150ef22` |
| P-1 | แพเลื่อนไม่ตรงกัน ผิดทั้งสองจอ (จอ Host เห็น Client ลอย, จอ Client เห็น Host ลอย) — `MovingPlateform` อ่าน `transform.position` ตอน `Spawned` เป็นจุดตั้งต้น แต่ฝั่ง client `NetworkRigidbody2D.Spawned` ย้าย transform ไปตำแหน่งปัจจุบันของ host ได้ก่อน → แพของ client แกว่งรอบจุดที่เลื่อนไป | host ส่งจุดที่วางไว้ในฉาก (`[Networked] StartPosition`, เก็บจาก `Awake`) ทุกเครื่องใช้ค่านี้; log `[VERIFY] platform ... start= transformAtSpawned=` ไว้ยืนยันตอนเทส | แก้แล้ว: `fix/feedback-1024` `9b45c0fe` |
| P-2 | จอ Client วาดตัว Host ย้อนหลัง (interpolate) แต่วาดแพตามเวลาที่ทำนาย → Host ดูเลื่อนจากแพ | host ส่ง `RepPlatformId` (แพที่ยืนอยู่); จอ client เลื่อนตัว Host ตามระยะที่แพขยับระหว่างสองเวลานั้น (เกลี่ย 0.12 วิตอนขึ้น/ลงแพ) — บน host ได้ 0 เอง | แก้แล้ว: `fix/feedback-1024` `9b45c0fe` |
| SFX-1 | เสียงกับดักไม่อยู่ใน SFX — กับดักหมี, ปืนหิมะ (และ checkpoint, ตาชั่ง) เล่นด้วย `AudioSource.PlayOneShot` ของตัวเอง (2D, ไม่คูณระดับ SFX, ได้ยินทั้งด่าน) | เล่นผ่าน `AudioManager.PlayObjectSfx` → SFX pool เหมือนเสียงตัวละคร (ตามระดับ SFX, เบาลงตามระยะ) ใช้ volume เดิมของแต่ละตัวเป็นความดังเฉพาะ | แก้แล้ว: `fix/feedback-1024` `521090f8` |
| PF-1 | `PressurePlateHinge` จอง array ทุก tick, `PuzzleItem` จอง array + string ทุกเฟรม → GC ทำงานบ่อย | ใช้ buffer ซ้ำ (`Physics2D.OverlapBox/OverlapCircle` + `ContactFilter2D`), เช็ค `HeldItemName.Length` แทน `ToString()` | แก้แล้ว: `fix/feedback-1024` `5ae01926` |
| PF-0 | หน่วงเมื่อเล่นนาน (ผู้ใช้คาดว่าเป็นทั้งสองฝั่ง) | soak test ด้านล่าง → ในเกมไม่มีอะไรสะสม; ที่โตคือ log ทดสอบ → ลด log `OnInput BLOCKED` (เดิมทุกวินาทีระหว่าง dialogue → เฉพาะตอนสถานะเปลี่ยน) และลบ log `anim` ของ NR-11 | test-only `432c3483` |
| PF-2 | ยังไม่รู้ว่าฝั่ง client / ใน build เป็นอย่างไร | `_Verify/VerifyPerfMonitor`: log `[VERIFY] perf role=HOST\|CLIENT ...` ทุก 30 วิ ทั้งสองฝั่ง (เฟรม, GC, allocKB/frame, heap, จำนวน object/network object/เสียง/particle/material/texture, tween, log) — กด F12 ดูบนจอ | test-only `8839cff6`, `432c3483` |

### 19.2 ผล soak test ผ่าน Unity MCP (Host คนเดียว, Stage1-S1, 15 นาที, ตัวละครเดิน/กระโดดตลอด)
- เฟรมเฉลี่ย 16.7 ms ตลอด (60 FPS), ทุกช่วง 30 วิมี GC 1 ครั้ง และเฟรมเกิน 50 ms 0–2 ครั้ง (สูงสุด ~100 ms) — ไม่แย่ลงตามเวลา
- object 714, network object 114, Rigidbody2D 16, AudioSource 33, particle 9, material 253, texture 1109, tween 0 — **คงที่ทั้ง 15 นาที**
- managed heap ของ Editor ~1 GB (heap สำรอง 3.4 GB) ขึ้นลงตามรอบ GC ไม่โตสะสม — GC ใน Editor หยุดนานเพราะ heap ใหญ่ (build จะเล็กกว่ามาก)
- สิ่งเดียวที่โต: จำนวน log ~150 รายการ/นาที (`OnInput BLOCKED` ทุกวินาทีระหว่าง dialogue + `anim` 3 บรรทัดต่อการกระโดด) → Editor.log ~40,000 บรรทัดใน 15 นาที — ลดแล้ว (PF-0)
- ข้อสรุป: ยังไม่พบต้นเหตุในโค้ดเกมสำหรับ Host คนเดียว — รอบเทส 2 คนให้ดู log `perf` ของทั้งสองฝั่ง ถ้าค่าใดโตตามเวลาจะเห็นทันที; ถ้าค่าคงที่แต่ยังหน่วง → ฝั่ง Editor (Console, AI Assistant ที่ log error ซ้ำ, Inspector/Scene view) — ลองใน build เทียบ

### 19.3 ผลเทสอัตโนมัติ (MCP, Host คนเดียว) — 19/19 ผ่าน
T1–T11 ของหัวข้อ 18.5 ผ่านทั้งหมดด้วย `runSpeed` 3.5 (ความเร็วบนพื้น 3.37, ถึงความเร็วเต็มใน 4 tick) + T12 ยืนบน `moving_platfrom` ทั้ง 5 ตัวใน S1 นาน 3.5 วิ อยู่บนแพ 209/209 tick ทุกตัว; log `platform ... start=` บน host ตรงกับตำแหน่งในฉากทุกตัว

### 19.4 Checklist เทส — `test/all2` (MPPM, Host + Client) ~30 นาที
| ขั้น | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|
| 0 | เปิด Unity ดู Console | 0 error | |
| 1 แพ | S1/S2: ทั้งสองคนยืนบนแพเลื่อน (สลับกันขึ้น) | บนจอแต่ละฝั่งเห็นอีกคนอยู่บนแพ ไม่ลอย; log `[VERIFY] platform ... start=` ของสองเครื่องตรงกัน (ฝั่ง client ถ้า `transformAtSpawned` ต่างจาก `start` = ยืนยันต้นเหตุ P-1) | |
| 2 เสียง | ตั้ง SFX = 0 แล้วเดินผ่านกับดักหมี/ปืนหิมะ/checkpoint/ตาชั่ง; ตั้งกลับแล้วยืนไกลกับดัก | SFX 0 = เงียบ; ไกลแล้วเบาลง | |
| 3 ความเร็ว | เดิน/กระโดดข้ามช่องใน S1–S3; ว่ายน้ำ, ร่อน, ลม | เดินช้าลง (3.5) ยังข้ามช่องได้; น้ำ/ร่อน/ลมเหมือนเดิม | |
| 4 หน่วง | เล่น 2 คนต่อเนื่อง 20–30 นาที; ถ้าเริ่มหน่วงจดเวลาไว้ | ผมอ่าน log `perf` ของ Host (Editor.log) และ Client (`Library/VP/<id>/Logs/Editor.log`) เอง; ถ้าเทส build ส่ง Player.log ทั้งสองเครื่อง | |

## 20. Performance: เสียง, ฟอนต์, แมงกะพรุนตรงกันสองเครื่อง (2026-10-25)

**ที่มา:** เจ้าของโปรเจกต์ถามว่าปรับอะไรให้ลื่น/ปิงนิ่งได้บ้าง แล้วเลือกทำ A (เสียง), B (ฟอนต์), E (แมงกะพรุน) — ไม่เปลี่ยนหน้าตาเกม
**Branch:** `fix/perf-1025` (แตกจาก `test/all2`) → fast-forward เข้า `test/all2` แล้ว — Roslyn + Unity 0 error; เทสอัตโนมัติผ่าน MCP 20/20

### 20.1 ต้นเหตุและการแก้
| รหัส | ต้นเหตุ | การแก้ | แก้แล้ว |
|---|---|---|---|
| PF-3 | เสียงทั้ง 43 ไฟล์ตั้ง Decompress On Load + **ปิด Preload Audio Data** → ถอดเป็น PCM ทั้งไฟล์บน main thread ตอนเล่นครั้งแรก (เสียงป่า 442 วิ ~78 MB, ใต้น้ำ 240 วิ ~42 MB ตอนดำน้ำครั้งแรก, เพลงคัตซีน 229 วิ ~40 MB รวม ~280 MB) → เกมค้างกลางเกม / host ค้างเสี่ยง client หลุด | เพลง/เสียงบรรยากาศยาว 7 ไฟล์ → Streaming; เสียง 3–60 วิ 14 ไฟล์ (ลม, ต่อยหิน, ว่ายน้ำ, เดินหิมะ, เควส, UI) → Compressed In Memory + Preload; เสียงสั้น 22 ไฟล์ → Decompress On Load + Preload (ถอดตอนโหลดด่าน) — ตั้งผ่าน `AudioImporter` (MCP), คุณภาพเสียงเดิม | แก้แล้ว: `fix/perf-1025` `c938316c` |
| PF-4 | `FredokaOne-Regular SDF` (ใช้ใน 11 ไฟล์) Dynamic + Clear Dynamic Data On Build → build เริ่มที่ 0 ตัวอักษร สร้าง glyph ตอนเล่น (กระตุก) และ Editor เขียนไฟล์ฟอนต์ใหม่ทุกครั้งที่เล่น; `Fredoka-VariableFont SDF` atlas เต็มที่ 49 ตัว ที่เหลือไปใช้ LiberationSans | ฟอนต์ Fredoka ทั้งสองมี ASCII + `— – ‘ ’ “ ” … •` + U+200B (103 ตัว, Fredoka Variable ใช้ 2 atlas) เป็น **Static**, ไม่ล้างตอน build; `→` (Fredoka ไม่มี) เติมใน `LiberationSans SDF - Fallback` ซึ่งคง Dynamic ไว้เป็นตาข่าย — ตัวอักษรที่เกมใช้มาจาก JSON ช่อง `eng` (ทุกชุด `isThaiLanguage`/`useThai` = 0), `m_text` ในฉาก/prefab และ string ในโค้ด | แก้แล้ว: `fix/perf-1025` `328e5569` |
| P-3 | `MoveSome` (JellyTrap ×16 ใน S1/S2) คำนวณตำแหน่งบน host คนเดียว client แค่ lerp ตามค่าล่าสุด → จอ client เห็นแมงกะพรุนช้ากว่าจริง ระเบิด (ตัดสินบน host) ไม่ตรงกับที่เห็น; `JellyfishTrap` หยุดการเคลื่อนที่ด้วย `moveScript.enabled` ใน Render แต่ละเครื่องคนละจังหวะ | ตำแหน่งเป็นฟังก์ชันของ tick ทุกเครื่องคำนวณเอง (แบบ `MovingPlateform`): host ส่ง `StartPosition` (จุดในฉาก) + `HoldTick` (tick ที่หยุด — ตอน pause/dialogue/สอนเล่น หรือ `JellyfishTrap` ขอหยุดตอนชาร์จ/ระเบิด/ซ่อน) | แก้แล้ว: `fix/perf-1025` `d0850374` |

### 20.2 ผลเทสอัตโนมัติ (MCP, Host คนเดียว, Stage1-S1) — 20/20 ผ่าน
- T1–T12 (หัวข้อ 18.5 / 19.3) ผ่านเหมือนเดิม
- T13 แมงกะพรุน `JellyTrap (1)`: ขยับตอน Idle → พาตัวละครไปใกล้ → ระหว่างชาร์จ/ระเบิดขยับไม่ถึง 0.01 หน่วย → ซ่อน → กลับมาขยับต่อ
- เสียงที่โหลดอยู่ระหว่างเล่น S1: 13 ไฟล์ รวม 1.5 MB (เช่นเพลง `悲しみのトランシーバー` 170 วิ แบบ Streaming ใช้ 0.2 MB — แบบเดิมคำนวณได้ ~30 MB)
- หลังเล่นจบ ฟอนต์ทั้ง 3 ตัวไม่ถูกแก้ (`EditorUtility.IsDirty` = false, ฟอนต์สำรองยัง 18 ตัวเท่าเดิม) และ `git status` สะอาด

### 20.3 Checklist เทส — `test/all2` (MPPM, Host + Client)
| ขั้น | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|
| 1 เสียง | เล่น S1–S3 ตามปกติ: BGM, คัตซีน, เป็ดดำน้ำ/ลอยน้ำ, ลม, เดินหิมะ, ต่อยหิน, เมนู | เสียงดังครบเหมือนเดิม; ดำน้ำครั้งแรกไม่ค้าง | |
| 2 ฟอนต์ | อ่าน dialogue, โน้ต, เมนู, HUD | ตัวอักษรครบ ไม่มีสี่เหลี่ยม; หลังเล่น `git status` ไม่มีไฟล์ฟอนต์เปลี่ยน | |
| 3 แมงกะพรุน | S1/S2: client เดินเข้าใกล้แมงกะพรุน ดูทั้งสองจอ | ตำแหน่งตรงกันสองจอ; ระเบิดโดนเมื่ออยู่ในวงที่เห็นจริง | |

## 21. SW-1 เปลี่ยนรูปปุ่ม (switch) เก่าเป็นชุดใหม่ (2026-10-25)

**ที่มา:** เจ้าของโปรเจกต์ขอให้เปลี่ยนรูปปุ่มชุดเก่า (`Bell/Switch/ไม่ต้องกดค้าง/`, `แบบกดค้าง/`) เป็นชุดใหม่ใน `Bell/Switch/` — เลือก: แก้ของในเกม + prefab ที่ยังใช้รูปเก่าทั้งหมด; ปุ่ม coop สีส้มใช้แมวลาย `switch2` เหมือนที่ S1 ใช้อยู่
**Branch:** `fix/switch-art` (แตกจาก `test/all2`) → fast-forward เข้า `test/all2` แล้ว — แก้ผ่าน Unity MCP (TextureImporter / PrefabUtility) ไม่แก้ YAML ตรง ไม่มีโค้ด

### 21.1 สิ่งที่เจอและการแก้
| เรื่อง | ต้นเหตุ | การแก้ | แก้แล้ว |
|---|---|---|---|
| ปุ่มขว้างหิน S2 (`ThrowButton`) ไม่มีรูปตอนปกติ | ไฟล์รูปชุดใหม่ถูกเปลี่ยนชื่อใน working tree (ยังไม่ commit): 5 ไฟล์คง GUID เดิม แต่ `newHSwichClose.png` ถูกแทนด้วย `WoodSwitch.png` ที่ได้ GUID ใหม่ → prefab อ้างไฟล์ที่หายไป | commit การเปลี่ยนชื่อ + `WoodSwitch.png`; ตัด sprite `WoodSwitch` ให้พอดีภาพ (6,9,84×45) PPU 80 pivot 20 px เหนือแถบดิน เหมือน `woodSwitch_Pressed` (ยืนยัน `Sprite.bounds`: ฐาน y −0.25 เท่ากัน ความกว้างเท่ากัน) แล้วใส่เป็นรูปปกติของ `ThrowButton` | แก้แล้ว: `fix/switch-art` `cd0d27eb` |
| prefab ยังใช้รูปเก่า | `OrangeButton_Coop` (Mi), `Hold_Door` (M), `Stepon_Pressure` ×2 / `Toggle_Pressure` (swich0), `Toggle_Pressure_Reverse` (Mi), `Toggle_ReversPressure` (swich0.1) | ปุ่มกดพร้อมกัน/กดค้าง → แมวลาย `switch2`/`switch2_Pressed` (`OrangeButton_Coop`, `Hold_Door`, `Stepon_*`); ปุ่มกดครั้งเดียว/สลับคนเดียว → แมวส้ม `switch1`/`switch_Pressed` (`Toggle_*`) — ตรงกับปุ่มที่ใช้ในด่านอยู่แล้ว (`BlueButton_Coop`, `Hold_Hinge` = switch2; `Single_Button_Door` = switch1) | แก้แล้ว: `fix/switch-art` `cd0d27eb` |
| `Stepon_*`, `Toggle_Pressure`, `Toggle_ReversPressure` ล่องหนเมื่อวางลงด่าน | ช่องรูปของสคริปต์ว่าง (prefab บันทึกก่อนสคริปต์มีช่องนี้) แต่สคริปต์ตั้ง `sr.sprite` จากช่องนี้ทุกเฟรม | ใส่รูปตามข้อบน | แก้แล้ว: `fix/switch-art` `cd0d27eb` |

- ฉาก S1–S3 ไม่ได้แก้: S1 `OrangeButton_Coop (1)` override เป็น switch2 อยู่แล้ว, S2 `ThrowButton` ไม่มี override จึงได้รูปจาก prefab
- หลังแก้ ไม่มี prefab/ฉากใดอ้างรูปใน `ไม่ต้องกดค้าง/` และ `แบบกดค้าง/` แล้ว (เหลือ `swich0.mat` ในโฟลเดอร์เดียวกัน) — ยังไม่ลบไฟล์เก่า
  - ปัจจุบันสองโฟลเดอร์นี้ชื่อ `Bell/Switch/NoHold/` และ `Bell/Switch/Hold/` (AS-1 หัวข้อ 25)
- บันทึก prefab ใหม่ทำให้ช่องที่สคริปต์เลิกใช้ (`targetTrap`, `_isRevers`) หายและช่องใหม่ได้ค่าเริ่มต้น — ไม่กระทบเกม

### 21.2 Checklist เทส — `test/all2` (MPPM, Host + Client)
| ขั้น | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|
| 1 | S2: เดินไปที่ปุ่มขว้างหิน (≈ x 25.5, y −3.1) | เห็นต้นไม้ตั้ง (`WoodSwitch`) ก่อนโดน | |
| 2 | นกขว้างหินใส่ปุ่ม | เปลี่ยนเป็นเถาโค้งที่ตำแหน่ง/ขนาดเดิม ทั้งสองจอ | |
| 3 | S1/S3 กดปุ่มทุกแบบ | รูปเหมือนเดิม (แมวลาย = กดพร้อมกัน/ค้าง, แมวส้ม = ปุ่มเดี่ยว) | |

## 22. Feedback build Test3: เพลงด่าน, เสียงปุ่ม, โน้ตฝั่ง Client, layer S3, ตาชั่ง, หินกระเด็น (2026-10-25)

**ที่มา:** เจ้าของโปรเจกต์เทส build Test3 แล้วแจ้งว่า
- เพลงคัตซีนซ้อนกับเพลงด่าน 1 และเพลงด่านดับหลัง dialogue จบทุกด่าน
- เสียงธง/คลิกไม่ตามค่า SFX และอยากได้เสียง "ติก" ตอนกดปุ่ม
- Client (เป็ด) อ่านโน้ต S3 ไม่ได้
- ตาชั่ง, เครื่องผลัก/ดึง และประตู S3 วาดทับผู้เล่น/ไอเทม
- วางของลงตาชั่งแล้วจานกดผู้เล่นทะลุพื้น
- ปาหินใส่อีกคนแล้วกระเด็นไกลเกิน

**Branch:** `fix/feedback-1025` (แตกจาก `test/all2`) → fast-forward เข้า `test/all2`
- Unity compile 0 error
- ฉาก/prefab แก้ผ่าน Unity MCP ไม่แก้ YAML ตรง
- เทสอัตโนมัติผ่าน MCP (Host คนเดียว)

### 22.1 ต้นเหตุและการแก้
| รหัส | ต้นเหตุ | การแก้ | แก้แล้ว |
|---|---|---|---|
| AU-1 | `DialogueVoteManager.RPC_CloseDialogueAll` เรียก `StopBGM()` ทุกครั้งที่ปิด dialogue (ของเหลือจากสมัย dialogue มีเพลงเอง) และไม่มีอะไรเล่นกลับ | ลบ `StopBGM()` | แก้แล้ว: `fix/feedback-1025` `6266039d` |
| AU-2 | `AudioManager` เล่น `FirstBGM` ทันทีที่โหลดฉาก ขณะคัตซีน S1 และวิดีโอหน้าโหลด (มีเสียง ทุกด่าน) ยังเล่นอยู่ | `HoldBGM`/`ReleaseBGM`: ฉากด่านที่หน้าโหลดยังไม่ปิดจะจำชื่อเพลงไว้ (หยุด `BGM_Player` ที่ Play On Awake ด้วย) → `GameManager.Render` ปล่อยตรงที่ปิดหน้าโหลด/หยุดวิดีโอ (ทุกเครื่องทำเอง); ถ้าเกมพร้อมแล้วตอนสร้าง manager ไม่รอ | แก้แล้ว: `fix/feedback-1025` `79b3beac` |
| AU-3 | วัดแล้ว (22.2): คลิก (`AudioClick` → `PlayDirectSound("Click")`) และธง (`CheckpointTrigger` → SFX pool) ตามค่า SFX ทั้งคู่ — SFX 0 = เงียบสนิท | ไม่แก้โค้ด — ต้องการข้อมูลเพิ่มจากเจ้าของโปรเจกต์ว่าเสียงที่ยังดังคือเสียงไหน/ปรับจากเมนูไหน | ยังไม่แก้ (รอยืนยัน) |
| SW-2 | ปุ่ม 5 แบบ (`PressurePlateHinge`, `CoopStepButton`, `CoopSwitch`, `ToggleButton_ReversPresure`, `ThrowButton`) ไม่มีเสียง | `AudioManager.PlaySwitchTick` (SFX pool) เล่นใน `Render` เมื่อสถานะที่ sync มาเปลี่ยน: แผ่นเหยียบ/ปุ่มหิน = ตอนกดลง, สวิตช์กด E = ทุกครั้งที่สลับ; ไม่เพิ่ม RPC; ใช้เสียง `SwitchTick` ถ้าเพิ่มใน `sfxSoundList` ไม่งั้นใช้ `Click` | แก้แล้ว: `fix/feedback-1025` `8fede2b6` |
| NT-1 | หลัง NR-12 การกด E รันบน host แต่ `Note.Interact` เช็ค `HasInputAuthority` แล้วเปิดโน้ตบนเครื่องที่รันโค้ด → ตัวของ Client ไม่ผ่าน (ไม่เกี่ยวกับเป็ด) | host ส่ง `RPC_ShowNote(ผู้อ่าน)` เปิดเฉพาะเครื่องผู้อ่าน; dialogue ต่อจากโน้ตเรียกครั้งเดียวบน host | แก้แล้ว: `fix/feedback-1025` `a5273938` |
| LY-1 | S3 ตัวแม่ `WeighingScale` มี SortingGroup (ฉากเพิ่มเอง) บน `SortingTexture` ซึ่งอยู่หน้า `ItemLayer`/`PlayerLayer` | SortingGroup → `DecorationLayer` | แก้แล้ว: `fix/feedback-1025` `8fc0dc0b` |
| LY-2 | เครื่องผลัก/ดึง `Wood_Pressure(+ EX)` / `Wood_Pull(+ EX)` อยู่ `SortingTexture` (S1 ×3, S2 ×4, S3 ×10) | renderer หลักใน 4 prefab → `TrapLayer` (แบบ S2 `Trap_Mix`); revert override ในฉาก S1/S3; FX ลมคง `ParticleLayer` | แก้แล้ว: `fix/feedback-1025` `8fc0dc0b` |
| LY-3 | ประตู S3 อยู่ `SortingTexture`: `Scale_Door`, `DoorAcess_Wood` ×2, `InsertStone_Door`, `HingeDoor` (`Timber 1_0` ×5) | → `DecorationLayer` (แบบประตู S1) — ทุกข้อ LY คง sorting order เดิมและยังอยู่หน้า `Tree-Tiles` (FB-7 ไม่กลับมา) | แก้แล้ว: `fix/feedback-1025` `8fc0dc0b` |
| SC-2 | จาน `CheckWeight_L/R` + พื้นยืน `G_PL/G_PR` (kinematic) ขยับด้วย transform จมได้ 1.5 หน่วย แต่ใต้จานสูง 1.66 ตัวละครสูง 0.83 → น้ำหนัก ≥ 600 จานลงมาทับคนที่อยู่ใต้จานแล้วดันทะลุพื้น | collider ทึบของจานและพื้นยืนใช้ `PlatformEffector2D` แบบ one-way (ยืน/วางของด้านบนได้เหมือนเดิม จานลงมาผ่านตัวคนข้างใต้); นับน้ำหนักผู้เล่นเฉพาะตัวที่ยืนบนจาน (เดิมคนใต้จานถูกนับเมื่อจานจมลงมาซ้อน) | แก้แล้ว: `fix/feedback-1025` `8fc0dc0b`, `6af80886` |
| KB-1 | หินมวล 1 ความเร็ว 16 ชนเป็ดด้วย physics ล้วน; บนจอ Client หินเป็น proxy kinematic (มวลไม่จำกัด) ดันตัวของ Client แรงเต็มที่ แล้วไถลต่อระหว่างมึน | หินชนผู้เล่นได้เฉพาะฝั่ง host; ตอนมึนล้างแรงชนแล้วใส่แรงกระเด็นคงที่ผ่าน `RaiseOwnerEvent` (field ใหม่ `stunKnockback` = (2.5, 2) ปรับใน `Rock.prefab`) → ระยะเท่ากันทั้งตัวของ Host และ Client | แก้แล้ว: `fix/feedback-1025` `34163a91` |

### 22.2 ผลเทสอัตโนมัติ (MCP, Host คนเดียว = เป็ด, S1 → S2 → S3)
probe ชั่วคราว (ไม่ commit) log เสียงที่ดังอยู่ทุก 0.25 วิ

**เสียง**
- ก่อนแก้:
  - S1 ช่วงคัตซีน: `BGM_Player` ดังพร้อม `Cutscene1.mp4`
  - S1 ช่วงหน้าโหลด: `BGM_Player` ดังพร้อม `Loading Scene`
- หลังแก้:
  - คัตซีน S1 และหน้าโหลดทุกด่าน มีแต่เสียงวิดีโอ
  - เพลงด่านเริ่มตอนเกมพร้อม (`READY … BGM playing=True`)
  - S2/S3 ปิด dialogue ด้วยการโหวตแล้ว `BGM playing=True`
- AU-3 (Music 0, ฟัง output ด้วย `AudioListener.GetOutputData`):

| เสียง | SFX 100 | SFX 0 |
|---|---|---|
| คลิก | peak RMS 0.4603 | 0.0000 |
| ธง (`lolo_s-mistery`) | 0.0203, pool vol 1.00 | 0.0000, pool vol 0.00 |

**ปุ่ม, หิน, ตาชั่ง, โน้ต, layer**
- SW-2: เหยียบ `CoopStepButton` 4 ครั้ง (2 รอบ) → tick 1 ครั้งต่อการเหยียบทุกครั้ง (SFX pool, 3D)
- KB-1: หิน 16 หน่วย/วิ ชนเป็ด (ตัวของ host) → มึน, กระเด็น 0.76 หน่วย
- SC-2 (เป็ดยืนใต้จานซ้าย, วางของ 1000 บนจาน):
  - ปิด one-way (แบบเดิม): เป็ดถูกดันลง y −33.58 (ทะลุพื้นที่ −28.0)
  - เปิด one-way: เป็ดอยู่ที่ −27.58 ตลอด, น้ำหนักจาน = 1000 (ไม่นับเป็ดข้างใต้)
- NT-1: host เรียก `Note.Interact` ด้วยตัวของตัวเอง → หน้าโน้ตเปิด (ผ่าน `RPC_ShowNote`)
- LY: ภาพจากกล้องเกมใน S3 — เป็ดยืนหน้าตัวตาชั่ง ถูกวาดอยู่หน้า

### 22.3 ข้อสังเกต (ไม่ได้แก้ในรอบนี้)
- S2/S3 สั่งเล่นคัตซีน `StreamingAssets/Stage1-S2|S3/Cutscene1.mp4` ที่ไม่มีไฟล์
  - `CutsceneManager` log error แล้วข้ามเอง
  - ใน Editor ถ้าเปิด Error Pause จะหยุดเกมจน Photon หลุด
- เพลงด่านเดิมยังดัง ~0.6 วิระหว่างหน้าโหลดขึ้นจนฉากถัดไปโหลด (ก่อนฉากเปลี่ยน)
- ยังอยู่ `SortingTexture` แต่ไม่ได้แจ้งมา:
  - ประตู S1/S2: `InsertItem_Door`, S1 `DoorAcess_Wood (1)`, `DimensionsDoor`
  - แผ่นกด `Hold_Hinge` (S3)
- ออกจาก Play Mode ขณะตัวละครอยู่ในน้ำ: `NetworkInteractableWater.OnTriggerExit2D` ส่ง RPC ตอน runner ปิด → NullReferenceException (ตอนปิดเท่านั้น)

### 22.4 Checklist เทส — `test/all2` (MPPM, Host + Client)
| ขั้น | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|
| 1 เพลง | เริ่มเกม S1: ดูคัตซีน → หน้าโหลด → เข้าเกม → อ่าน dialogue จบ (ทั้งสองคนกด) | คัตซีนได้ยินแต่เพลงคัตซีน, หน้าโหลดได้ยินแต่เสียงหน้าโหลด, เพลงด่านเริ่มหลังหน้าโหลดปิดและยังเล่นหลัง dialogue ทั้งสองจอ | |
| 2 เพลง | ไป S2, S3 (ผ่านประตู/F8) | เหมือนขั้น 1 ทุกด่าน | |
| 3 SFX | เมนู: SFX = 0 แล้วเข้าธง, คลิกปุ่ม, เหยียบปุ่ม; แล้ว SFX = 100 | SFX 0 เงียบ, 100 ดัง — ถ้าเสียงไหนยังดังตอน 0 ให้จดชื่อเสียง/จุดที่ได้ยิน และเมนูที่ใช้ปรับ (lobby หรือในเกม) | |
| 4 ปุ่ม | เหยียบแผ่น/ปุ่ม coop, กด E สวิตช์, ปาหินโดนปุ่ม S2 | ได้ยินติก 1 ครั้งต่อการกด ทั้งสองจอ | |
| 5 โน้ต | Client กด E ที่โน้ตทุกด่าน (ลองทั้งเป็ดและนก) | หน้าโน้ตเปิดเฉพาะจอ Client; dialogue ต่อจากโน้ต (ถ้ามี) ขึ้นทั้งสองจอครั้งเดียว; Host อ่านได้เหมือนเดิม | |
| 6 layer | S3: เดินผ่านตาชั่ง, ประตูทุกบาน, เครื่องลม; S1/S2: เครื่องลม; วางไอเทมบนจาน | ผู้เล่นและไอเทมอยู่หน้าทุกชิ้น และทุกชิ้นยังเห็นหน้าผนังต้นไม้ | |
| 7 ตาชั่ง | S3: คนหนึ่งยืนใต้จาน อีกคนวางของจนจานจม (ลองทั้งตัว Host และตัว Client อยู่ใต้จาน) | ไม่ทะลุพื้น; เดิน/กระโดดออกได้; ยืนบนจานยังนับน้ำหนัก, ใต้จานไม่นับ | |
| 8 หิน | นกปาหินใส่เป็ด (เป็ดเป็น Host และเป็ดเป็น Client) | มึนและกระเด็นสั้น ๆ ระยะเท่ากันทั้งสองแบบ; หินยังฆ่ามอน/กดปุ่มหินได้ | |

## 23. Controller ใน lobby: ตัวเลือกหายหลังออกจากเกม, ปุ่มตัวเลขใส่เลขห้อง, กรอบ highlight + ลดเสียงคลิก/ธง (2026-10-26)

**ที่มา:** เจ้าของโปรเจกต์แจ้ง
- Controller ใส่เลขห้องไม่ได้ (อยากได้คีย์บอร์ด popup)
- ออกจากเกมรอบแรกด้วย Controller แล้วสร้างห้อง → กดลงจากปุ่มกากบาทแล้วตัวเลือกหาย
- ลดเสียงคลิกและเสียงธง 50% (เสียงติกปุ่มในด่านแยกออก คงความดังเดิม)
- ขอให้ highlight ของ Controller เป็นกรอบขาวแบบ highlight ผู้เล่น/ไอเทม

ผู้ใช้เลือก: ปุ่มตัวเลข popup (เลขห้องเป็นตัวเลข 6 หลัก), กรอบขาวมุมมน

**Branch:** `fix/controller-ui` (แตกจาก `test/all2`) → fast-forward เข้า `test/all2`
- Unity compile 0 error
- ฉาก/prefab แก้ผ่าน Unity MCP
- เทสอัตโนมัติผ่าน MCP ด้วยจอยเสมือน (`InputSystem.QueueStateEvent`) ให้ทุกการกดผ่าน `UIKeyboardNavigator` จริง

### 23.1 ต้นเหตุและการแก้
| รหัส | ต้นเหตุ | การแก้ | แก้แล้ว |
|---|---|---|---|
| NV-1 | `GameManager.ReturnToSession` ลบ UI ในเกมด้วยชื่อ `PlayerInterfaceCanvas` แต่ `CenterHost` สร้าง `Canvas.prefab` ชื่อ `Canvas` → UI ในเกมทั้งชุดค้างใน DontDestroyOnLoad เข้า lobby; ออกผ่านเมนู Quit ปุ่ม Resume/Reset/Setting/Quit/Tutorial ยัง active + กดได้ ซ่อนอยู่หลัง lobby; navigation อัตโนมัติของ Unity หาจาก Selectable ทุกตัว → กดทิศแล้วตัวเลือก (และกรอบ) ไปอยู่ที่ปุ่มที่มองไม่เห็น | `ReturnToSession` ลบ root ชื่อ `Canvas` เฉพาะใน DontDestroyOnLoad (เกมถัดไป `CenterHost` สร้างใหม่เหมือนเกมแรก); `UIKeyboardNavigator` กันซ้ำ: ตัวเลือกใหม่ต้องอยู่ root canvas เดิม/มองเห็น/กดได้ ไม่งั้นหาปุ่มในทิศเดียวกันบนหน้าเดิม; ตัวที่เลือกยังอยู่บนจอแต่ใช้ไม่ได้ → กดทิศแล้วเลือกปุ่มบนสุดของหน้านั้น; ไม่มีอะไรถูกเลือก/หน้าปิดไปแล้ว (กำลังเล่น) ไม่แตะ | แก้แล้ว: `fix/controller-ui` `fb75c6c1` |
| KP-1 | กด A ที่ช่องใส่เลข → ช่องเข้าโหมดพิมพ์ แล้ว `UIKeyboardNavigator` หยุดรับปุ่ม → Controller ติดในช่อง; ช่องยังเข้าโหมดพิมพ์ทันทีที่ถูกเลือก; navigation อัตโนมัติหาช่องไม่เจอระหว่าง DOTween pop-in (scale 0) | `RoomCodeKeypad` (0–9, DEL, OK, ตารางวนในตัว, สูงสุด 6 หลัก, OK = กด Join ให้, B ปิด, X ลบ, Start = OK); กด A ที่ช่อง → เปิด popup; ช่องไม่เข้าโหมดพิมพ์ตอนถูกเลือก (คลิกเมาส์/Enter ยังพิมพ์ได้); หน้า Join เริ่มที่ช่องใส่เลข; ช่อง/Join/ปุ่มกลับ ใช้ navigation แบบ Explicit; UI popup สร้างใน `SessionScene` (แก้หน้าตาต่อใน Editor ได้) | แก้แล้ว: `fix/controller-ui` `8b78238c` |
| HL-1 | highlight เป็นกล่องเหลืองโปร่งแสงทับปุ่ม | กรอบขาวมุมมนรอบปุ่ม (texture สร้างในโค้ด, 9-slice, กลางโปร่ง) + ขอบเข้มบาง ๆ ให้เห็นบนปุ่มขาว/ฟ้าอ่อน; ยังแสดงเฉพาะตอนมี gamepad | แก้แล้ว: `fix/controller-ui` `0912d6ba` |
| SFX-2 | คลิกในเกม (`AudioManager.prefab` `uiSource`), คลิก lobby (`AudioSession.uiSource`), ธง (`Spawnpoint_WayMark.prefab`) volume 1 | ทั้งสามเป็น 0.5; ติกปุ่มในด่าน: entry `SwitchTick` ของตัวเองใน `sfxSoundList` (ไฟล์เดิมไปก่อน เปลี่ยนใน Inspector ได้) + `switchTickVolume` = 1 (เล่นผ่าน SFX pool ไม่โดนการลดคลิก) | แก้แล้ว: `fix/controller-ui` `99cd7cc9` (คลิก lobby อยู่ใน `8b78238c`) |

### 23.2 ผลเทสอัตโนมัติ (MCP, Host คนเดียว, จอยเสมือน)

**NV-1**
- ก่อนแก้ (ออกผ่านเมนู pause → Quit ด้วยจอย):
  - DontDestroyOnLoad ยังมี `Canvas` ของเกม
  - ในนั้นมี `PauseMenu/Resume`, `Reset`, `Setting`, `Quit`, `Tutorial` active + interactable
  - กรอบ highlight ค้างอยู่บน canvas นั้น (ขนาดปุ่ม Quit 372×122)
- หลังแก้:
  - DontDestroyOnLoad ไม่มี `Canvas`
  - Selectable นอก `SessionScene` = 0
  - กดลง/ขวา/ขึ้นจากกากบาท อยู่ใน `Session_Create` ตลอด
  - กรอบอยู่ที่ปุ่ม lobby (90×90)
- เกมที่ 2 หลังออก:
  - สร้าง `Canvas` ใหม่ 1 ชุด; HUD/หลอดเลือด, dialogue (`Canvas/Dialogue`), ปุ่ม Skip คัตซีนใช้ได้
  - เมนู pause เลื่อน/กดได้
- ระหว่างเล่น:
  - ไม่มีปุ่มที่ active+interactable บน HUD
  - กด D-pad แล้วตัวเลือกไม่เปลี่ยน

**KP-1**
- หน้า Join เริ่มที่ช่องใส่เลข (ไม่เข้าโหมดพิมพ์); ขวา → Join, ลง → ปุ่มกลับ, ขึ้น → ช่อง
- กด A → popup เปิดที่ปุ่ม 1
- กด 1 2 3 6 5 4 → `123654`; กดหลักที่ 7 ไม่เพิ่ม; X → `12365`
- ซ้ายวนในแถว; B ปิดกลับช่อง (ไม่เข้าโหมดพิมพ์); A เปิดใหม่ → ไล่ไป 0 → `123650`
- Start → ปิดแล้วกด Join

**HL-1**
- ภาพจอ: กรอบขาวมุมมนรอบ PLAY (เมนูหลัก), รอบ RESUME (เมนู pause ในเกม)
- บนปุ่มขาวของ popup เห็นขอบเข้ม

**SFX-2** (SFX 100, Music 0, ฟังด้วย `AudioListener.GetOutputData`)

| เสียง | ก่อน | หลัง |
|---|---|---|
| คลิก | 0.4603 | 0.2302 |
| ธง (`Spawnpoint_WayMark (1)`) | 0.0203 | 0.0097 (pool vol 0.50) |
| ติก | — | 0.751 (pool vol 1.00 ไม่ลด) |

### 23.3 ข้อสังเกต (ไม่ได้แก้)
- ใส่เลขห้องที่ไม่มีอยู่จริง: Fusion ตอบ `GameNotFound` → `OnShutdown` สาย lobby พากลับเมนูหลัก (ไม่อยู่หน้า Join พร้อมข้อความ "Connection failed") — พฤติกรรมเดิม
- เล่นใน Editor แล้ว `LoadingScreenManager.prefab` ถูกเขียน `m_IsActive: 0` (โค้ดหน้าโหลด SetActive ตัว prefab asset)
  - เป็นที่มาของไฟล์นี้ที่โผล่ใน working tree บ่อย ๆ
  - รอบนี้คืนค่าด้วย git ไม่ commit
- ข้อความ debug "Join Session Fail: …" ใช้ฟอนต์ที่ตกไป `LiberationSans SDF - Fallback` (dynamic) → ไฟล์ฟอนต์นี้เปลี่ยนหลังเทส join (คืนค่าแล้ว)
- กรอบ highlight ปรับขนาดได้ในโค้ด (`frameThickness` 4, `cornerRadius` 12, `padding` 6 ใน `SelectionHighlightFollower`) — component ถูกเพิ่มตอนรัน จึงไม่ขึ้นใน Inspector

### 23.4 Checklist เทส — `test/all2` (MPPM หรือ build, Host + Client, จอยจริง)
| ขั้น | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|
| 1 | เล่นเกม → เปิดเมนู pause ด้วยจอย → Quit → สร้างห้อง → กดลง/ซ้าย/ขวา/ขึ้น | ตัวเลือกและกรอบอยู่บนปุ่มของห้องตลอด ไม่หาย | |
| 2 | ข้อ 1 แล้วเริ่มเกมรอบ 2 | HUD, dialogue, คัตซีน, เมนู pause ใช้ได้เหมือนรอบแรก | |
| 3 | Client: Join → กด A ที่ช่องเลข → กดเลขห้องของ Host ด้วย popup → OK | เข้าห้องได้; B ปิด popup, X ลบ ใช้ได้ | |
| 4 | คีย์บอร์ด/เมาส์: คลิกช่องเลขแล้วพิมพ์ / กด Enter ที่ช่องแล้วพิมพ์ | พิมพ์ได้เหมือนเดิม | |
| 5 | ทุกเมนู (lobby, ตั้งค่า, pause, dialogue, tutorial) ด้วยจอย | กรอบขาวมุมมนรอบปุ่ม เห็นชัดทั้งบนปุ่มสีเข้มและสีขาว | |
| 6 | คลิกปุ่มใน lobby/ในเกม, เข้าธง, เหยียบปุ่มในด่าน | คลิกและธงเบาลงครึ่งหนึ่ง, ติกปุ่มดังเท่าเดิม, ทั้งหมดยังตามค่า SFX | |

## 24. Dynamic Input Prompt — รูปปุ่มเปลี่ยนตามอุปกรณ์ที่ใช้ล่าสุด + template ทีม UI (2026-10-26)

**ที่มา:** เจ้าของโปรเจกต์ขอให้ปุ่มบน UI เปลี่ยนเป็นปุ่ม Controller เมื่อเล่นด้วยจอย (Seamless Glyph Switching) และทำเป็น template ส่งทีม UI

ผู้ใช้เลือก:
- รองรับ Xbox + PlayStation (จอยอื่น = Xbox)
- ทำระบบ + template + ต่อเข้าจุดที่มีอยู่

**Branch:** `feature/input-prompts` (แตกจาก `test/all2`) → fast-forward เข้า `test/all2`
- Unity compile 0 error
- prefab/ฉากแก้ผ่าน Unity MCP
- คู่มือทีม UI: `Assets/Art-Source/UI/InputGlyphs/README.md`

### 24.1 สิ่งที่ทำ
| รหัส | อะไร | แก้แล้ว |
|---|---|---|
| IP-1 | ระบบใน `Assets/Scripts/U_interface/InputPrompt/`:<br>• `InputDeviceWatcher` — อุปกรณ์ที่กดล่าสุด: ปุ่มทุกชนิด / ก้าน > 0.5 / เมาส์ขยับต่อเนื่อง > 20 px; ถอดจอยหมด → คีย์บอร์ด; PS4/PS5 = `DualShockGamepad`<br>• `InputPromptAction` (12 การกระทำ)<br>• `InputGlyphSet` (Resources, รูป/รูปตอนกด/ชื่อ ต่ออุปกรณ์)<br>• `InputGlyph` (Image/SpriteRenderer)<br>• `InputPromptText` (ข้อความ `{Action}`) | แก้แล้ว: `feature/input-prompts` `3cfee371` |
| IP-2 | ต่อจุดเดิม:<br>• ไอคอนสกิล 4 ตัว (`SkillGUI.keyAction`)<br>• ป้ายกด E เหนือของ (`Canvas.prefab` `InteractPrompt/Square`)<br>• หน้าสอนเล่น (`TutorialData.tutorialSpriteController`, เปลี่ยนสดตอนเปิดอยู่)<br>• ข้อความ popup เลขห้อง (ซ่อนตอนใช้คีย์บอร์ด) | แก้แล้ว: `feature/input-prompts` `f1b2c8fb` |
| IP-3 | Template:<br>• รูป placeholder 50 ไฟล์ (Xbox/PS/Keyboard + `_pressed`, 300×300 และ ×2 1133×300, PPU 300)<br>• `Resources/InputGlyphSet.asset` ต่อครบ 12 การกระทำ<br>• `Template/InputPrompt_UI.prefab`, `InputPrompt_World.prefab`<br>• README ภาษาไทย | แก้แล้ว: `feature/input-prompts` `2726f645` |
| HL-2 | กรอบเลือกปุ่มขึ้นเฉพาะตอนกำลังใช้จอย (เดิม: เมื่อมีจอยเสียบ แม้ใช้เมาส์อยู่) | แก้แล้ว: `feature/input-prompts` `62229b36` |

### 24.2 ผลกระทบที่ตรวจก่อนแก้ (สรุป)
- ไม่แตะการอ่านปุ่มของเกม (`INetworkStructure`, `UIKeyboardNavigator`, `RoomCodeKeypad`) — ระบบใหม่แค่ฟัง
- ไม่มี network: แต่ละเครื่องเปลี่ยนตามอุปกรณ์ตัวเอง
- `SkillGUI.SetPressed` ถูกเรียกทุกเฟรม → เลือกคู่รูปใหม่เฉพาะตอนอุปกรณ์เปลี่ยน (วัดแล้วไม่จอง memory)
- `InteractPrompt` ไม่มี Animator — ปรับ scale ให้ขนาดเท่าเดิมได้
- หน้าสอนเล่น: field ใหม่ว่าง = รูปเดิม; เทส `TutorialPanelKeyboardNavTests` ไม่พัง
- พฤติกรรมที่เปลี่ยน: จอยเสียบอยู่แต่ใช้เมาส์ → กรอบเลือกปุ่มหาย (ตั้งใจ)
- ข้อจำกัด:
  - จอย PS ผ่าน Steam Input / DS4Windows จะเห็นเป็น Xbox
  - ข้อความใช้ชื่อปุ่มภาษาอังกฤษ (ฟอนต์ไม่มี ✕○□△)

### 24.3 ผลเทสอัตโนมัติ (MCP, Host คนเดียว, อุปกรณ์เสมือน Xbox + DualShock4 + คีย์บอร์ด/เมาส์จริง)

**การสลับอุปกรณ์**
- สลับ คีย์บอร์ด → Xbox → PS → คีย์บอร์ด ถูกทุกครั้ง (`psIsDualShock=True`)
- ก้าน 0.3 + เมาส์ 3 px → ไม่สลับ (เปลี่ยน 0 ครั้ง); เมาส์ 36 px → คีย์บอร์ด; ก้าน 0.8 → Xbox

**รูปที่ได้**

| จุด | คีย์บอร์ด | Xbox | PlayStation |
|---|---|---|---|
| สกิลเป็ด | `F_0` / `E_0` | `xbox_rb` / `xbox_x` | `ps_r1` / `ps_square` |
| สกิลนก | `spacebar x 2` / `q_0` | `xbox_a_x2` / `xbox_rb` | — |
| ป้ายกด E | `E_Button_0` scale 100 | `xbox_x` scale 77 (สูง 0.77 เท่ารูปเดิม) | — |

- รูปตอนกด: `ps_r1_pressed`, `xbox_a_x2_pressed`
- `SetPressed` ×2000 = จอง memory **0 byte**
- หน้าสอนเล่น: รูปคีย์บอร์ด → รูปจอย → รูปคีย์บอร์ด ตามอุปกรณ์ขณะเปิดอยู่
- ข้อความ popup เลขห้อง: ซ่อนตอนคีย์บอร์ด
- กรอบเลือกปุ่ม: ขึ้นตอน Xbox/PS, หายตอนคีย์บอร์ด
- ภาพจอ HUD: ไอคอนสกิลแสดงปุ่ม RB/X, R1/□, A×2/RB ตามอุปกรณ์

### 24.4 Checklist เทส — `test/all2` (MPPM หรือ build, Host + Client, จอยจริง)
| ขั้น | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|
| 1 | เล่นด้วยจอย Xbox: เดินไปใกล้ของที่กด E ได้, ดูไอคอนสกิล | ป้าย X เหนือของ (ขนาดเท่าป้าย E เดิม), สกิลเป็ด RB/X, สกิลนก A×2/RB | |
| 2 | แตะคีย์บอร์ดหรือขยับเมาส์ | กลับเป็น E/F/Q/Space×2 ทันที, กรอบเลือกปุ่มในเมนูหาย | |
| 3 | จอย PS4/PS5 (ไม่ผ่าน Steam/DS4Windows) | □ / R1 / ✕×2 | |
| 4 | Host ใช้จอย, Client ใช้คีย์บอร์ด | แต่ละจอแสดงตามอุปกรณ์ของตัวเอง | |
| 5 | ใส่รูปสอนเล่นเวอร์ชันจอยใน `LevelData` สักหน้า แล้วเปิดด้วยจอย/คีย์บอร์ด | รูปเปลี่ยนตามอุปกรณ์ | |
| 6 | ทีม UI วาดทับไฟล์ใน `InputGlyphs/` ชื่อเดิม | รูปใหม่ขึ้นทุกจุดโดยไม่ต้องต่อใหม่ | |

## 25. เป็ดกด E เลือกสิ่งที่ใกล้สุด, จอยเลือกปุ่ม = หน้าตา Hover, ชื่อ asset ไทย → อังกฤษ (2026-10-27)

**ที่มา:** เจ้าของโปรเจกต์แจ้ง
- เป็ดกด X/E ตอนนกกับหินอยู่ใกล้ทั้งคู่ → ให้เช็คสิ่งที่ใกล้ที่สุด ถ้าเป็นหินก็ทุบหิน ไม่ยกนก
- กรอบขาว (HL-1) ให้เหลือแค่หลอดปรับค่า (Sound / Music) ส่วนปุ่มใช้หน้าตาตอนเมาส์ชี้ (Hover)
- เปลี่ยนชื่อ asset ภาษาไทยเป็นอังกฤษ (ชี้ = hover, กด = pressed, กรอบ = frame, ใส่ = insert, ย้อน = back, กดไม่ได้ = can't press)

ผู้ใช้เลือก:
- หิน = หินใหญ่ที่เป็ดทุบ (`BreakAbleRock`); แบกนกอยู่แล้วกด E = โยนนกอย่างเดียว
- ปุ่มที่ไม่มีรูป/สี Hover ให้เห็น และช่องใส่เลขห้อง ใช้กรอบขาว
- `__` / `___` ในชื่อไฟล์ (เดิมคือ `??` ที่ Windows ใส่ในชื่อไฟล์ไม่ได้) → `empty`; ชื่อรูปย่อยเปลี่ยนตามชื่อไฟล์ใหม่; ลบ `โครงการใหม่.png` / `โครงการใหม่ (1).png`
- "R1 = นกปาหิน" ตรงกับโค้ดอยู่แล้ว (`INetworkStructure.cs` `rightShoulder` → Throw) ไม่ต้องแก้

**Branch:** `fix/feedback-1027` (แตกจาก `test/all2`) → fast-forward เข้า `test/all2`
- Unity compile 0 error
- asset เปลี่ยนชื่อผ่าน Unity MCP (`AssetDatabase.RenameAsset`, Sprite Editor data provider) — ไม่แก้ YAML ของฉาก/prefab

### 25.1 ต้นเหตุและการแก้
| รหัส | ต้นเหตุ | การแก้ | แก้แล้ว |
|---|---|---|---|
| IN-1 | กด E ครั้งเดียว host รันสองตัวจัดการใน tick เดียวกันโดยไม่รู้กัน: `MovementCharacter.HandleInteraction` ใช้ของ/ทุบหิน (เลือก **ตัวแรก** ที่ overlap คืนมา ไม่ใช่ตัวใกล้สุด) แล้ว `Duck_Moveset.HandleDuckInteraction` ยกนกต่อ → ทุบหิน + ยกนกพร้อมกัน; แบกนกอยู่ = ทุบหิน + โยนนกพร้อมกัน; ป้ายกดชี้ตัวใกล้สุดแต่กดแล้วได้ตัวแรก | `FindClosestInteractable` เลือกของที่ใกล้สุด (วัดแบบเดียวกับป้ายกด); hook `FriendInteractWins` — เป็ดชนะเมื่อแบกอยู่ (โยนนกอย่างเดียว) หรือนกที่ยกได้ใกล้กว่าของ; กดที่ใช้กับของไปแล้ว (`InteractUsedOnWorldThisTick`) เป็ดไม่ยกนก; ป้ายกดซ่อนเมื่อกดแล้วจะไปยก/โยนนก; ตัดสินที่ host ทั้งหมด ไม่มี RPC ใหม่ | แก้แล้ว: `fix/feedback-1027` `4e8f481f` |
| HL-3 | uGUI โชว์สถานะ Selected ก่อน Highlighted เสมอ — ปุ่มที่จอยเลือกจึงเป็นหน้าปกติ (ไม่มี `selectedSprite`) + กรอบ | `SelectionHighlightFollower`: ปุ่มที่มี Hover ให้เห็น (SpriteSwap มี `highlightedSprite` / ColorTint สี Hover ต่างจากสีปกติ ≥ 0.08) → ยืมรูป/สี Hover เป็นหน้าตาตอนถูกเลือกชั่วคราว คืนค่าเดิมเมื่อเลือกตัวอื่น/เลิกใช้จอย; กรอบสำหรับหลอด, ช่องพิมพ์, ปุ่มที่ไม่มี Hover และปุ่มที่กดไม่ได้ (Unity โชว์หน้า Disabled ทับ) — เลือกใหม่เมื่อปุ่มเปลี่ยนกดได้/ไม่ได้ระหว่างถูกเลือก; ยังทำเฉพาะตอนใช้จอย (HL-2) | แก้แล้ว: `fix/feedback-1027` `b19eeaa3`, `1de98890` |
| AS-1 | ชื่อไฟล์/โฟลเดอร์ไทย 123 path + ชื่อรูปย่อยไทยใน 40 `.meta` | เปลี่ยนชื่อผ่าน Unity (GUID + internal ID เดิม): รูป UI lobby, เสียงทั้งหมดใน `Sound/`, โฟลเดอร์ `TestJsonData/`, `Bell/Switch/Hold`·`NoHold`, ต้นไม้/ประตู, `Untitled design.mat`; รูปย่อย = `<ชื่อไฟล์ใหม่>_N` (`ReadyButton.png` `Yes จบ_0` → `ReadyButton_0`); ลบลูกศร `โครงการใหม่` 2 ไฟล์ | แก้แล้ว: `fix/feedback-1027` `8318deff` |

### 25.2 ผลเทสอัตโนมัติ

**AS-1** (Unity MCP)
- reference ไปยัง asset ที่เปลี่ยนชื่อ (ฉาก, prefab, anim, mat) 70 รายการ — ก่อน 70/70 resolve, หลัง 68/70; 2 ที่หายคือลูกศรที่ลบ ซึ่งมีแค่ฉาก `_Recovery` อ้าง
- `.meta` 111 ไฟล์เทียบกับ HEAD: เปลี่ยนแค่ชื่อรูปย่อย + `spriteID` ที่ Unity เติม; pivot เปลี่ยนเฉพาะรูปที่ alignment ไม่ใช่ Custom (ไม่มีผลต่อภาพ)
- path ที่มีอักษรไทยใน `Assets/` = 0, `.meta` ที่มีชื่อไทย = 0
- git เห็นเป็น rename 231 ไฟล์ + ลบ 4 + แก้ 1 (`ReadyButton.png.meta`)
- ไม่แตะ `Sound/悲しみのトランシーバー.mp3` (ญี่ปุ่น)

**HL-3** (Play Mode จาก `SessionScene`, จอยเสมือน + คีย์บอร์ดเสมือน) — 40/40 ผ่าน
- Hover (รูป Hover ขึ้น, ไม่มีกรอบ, เลิกเลือกแล้วคืนค่าเดิม): PLAY / SETTING / EXIT, Create / Join, X (หน้าเลือกห้องและในห้อง), Join, ปุ่มกลับหน้า Join, ปุ่มตัวเลข keypad (สีฟ้า Hover)
- กรอบ: ช่องเลขห้อง, ลูกศรเปลี่ยนตัวละคร ×2, Ready/Not, Roomcode, START ตอนกดไม่ได้ (รออีกคน), Sound / Music slider
- กดคีย์บอร์ด → PLAY กลับหน้าตาเดิม ไม่มีกรอบ; แตะจอยอีกครั้ง → Hover กลับมา
- ปุ่ม X ที่ถูกเลือกโดน CanvasGroup ปิด → เปลี่ยนเป็นกรอบ; เปิดกลับ → Hover
- ภาพจอ: PLAY หน้าตา Hover, ลูกศร `<` ในห้องมีกรอบ, หลอด SOUND มีกรอบ; รูป `???` / เงาตัวละครที่เปลี่ยนชื่อแล้วขึ้นปกติ

**IN-1** — เล่นคนเดียวไม่มีนกให้เทส → ✅ โค้ด (ไม่ได้รัน); รอ MPPM ข้อ 1–5 ด้านล่าง

### 25.3 ข้อสังเกต
- ปุ่มเมนูในเกม Prev / Next / Skip / Tutorial ใช้สี Hover ค่าเริ่มต้นของ Unity (เข้มขึ้น 4%) → ใช้กรอบ; ถ้าทีม UI ใส่รูป/สี Hover ภายหลัง จะเปลี่ยนเป็น Hover เอง
- คีย์บอร์ดเลื่อนเมนูยังไม่มีอะไรบอกตัวที่เลือก (ตามที่ตัดสินใน HL-2: กรอบ/Hover เฉพาะตอนใช้จอย)
- เล่นใน Editor แล้ว `LoadingScreenManager.prefab` ถูกเขียน `m_IsActive: 0` อีก — คืนค่าด้วย git ไม่ commit

### 25.4 Checklist เทส — `test/all2` (MPPM, Host + Client; สลับให้เป็ดเป็นทั้ง Host และ Client)
| ขั้น | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|
| 1 | (ปลดสกิลทุบหินแล้ว) เป็ดยืนชิดนก มีหินใหญ่ในระยะแต่ไกลกว่า กด E / X | ยกนก หินไม่แตก; ป้ายกดไม่ขึ้นเหนือหิน | |
| 2 | เป็ดยืนชิดหิน นกอยู่ใกล้แต่ไกลกว่าหิน กด E / X | ทุบหิน นกไม่ถูกยก; ป้ายกดอยู่เหนือหิน | |
| 3 | เป็ดแบกนกยืนข้างหิน กด E | โยนนกอย่างเดียว หินไม่แตก; ป้ายกดไม่ขึ้นตอนแบก | |
| 4 | เป็ดยืนระหว่างของสองชิ้น (เช่นโน้ต + กุญแจ) กด E | ใช้ชิ้นที่ป้ายกดชี้ (ใกล้กว่า) | |
| 5 | นกหยิบหิน / ใช้ของ ตามปกติ | เหมือนเดิม | |
| 6 | lobby + เมนู pause + หน้าตั้งค่า ด้วยจอย | ปุ่มที่มีรูป Hover ขึ้นหน้า Hover ไม่มีกรอบ; หลอดเสียง, ช่องเลขห้อง, ลูกศรตัวละคร, Ready, Roomcode, START ที่กดไม่ได้ มีกรอบ | |
| 7 | Host อยู่ที่ START ด้วยจอย แล้ว Client กด Ready | START เปลี่ยนจากกรอบเป็นหน้า Hover | |
| 8 | ใช้เมาส์คลิกปุ่มแล้วขยับออก | ปุ่มไม่ค้างหน้า Hover | |
| 9 | เปิดฉาก S1–S3 / lobby หลังเปลี่ยนชื่อ asset | รูป เสียง บทพูด ครบเหมือนเดิม ไม่มี Missing | |

## 26. UI ชุดใหม่ของทีม UI (PlayStation) จาก `Resources/UI_Assets/NewUI` (2026-10-27)

**ที่มา:** ทีม UI ส่งรูป 21 ไฟล์ — ปุ่มจอย PS, ป้าย "○ TO CLOSE", ปุ่มลูกศร back และปุ่ม ? (how to play) พร้อมสถานะ `hold` (ชี้/เลือก) / `pass` (กด) / `dis` (กดไม่ได้)

ผู้ใช้เลือก:
- ผูกไฟล์ใน NewUI ตรง ๆ (ไม่คัดลอกทับ placeholder)
- back ใช้ที่ปุ่มกลับใน lobby + Prev/Next บทสนทนาและหน้าสอนเล่น; ? ใช้ที่ปุ่ม Tutorial ในเมนู pause
- ป้าย TO CLOSE ของ Xbox ทำ placeholder ให้

**Branch:** `feature/ps-ui` (แตกจาก `test/all2`) → fast-forward เข้า `test/all2`
- Unity compile 0 error
- import / asset / prefab / ฉาก แก้ผ่าน Unity MCP

### 26.1 สิ่งที่ทำ
| รหัส | อะไร | แก้แล้ว |
|---|---|---|
| IP-4 | `InputGlyphSet` ช่อง PS ชี้ art จริง (L Stick, L1, R1, ✕, ✕×2, □, OPTION); import แบบเดียวกับปุ่ม E/F/Q (sprite เดียวตัดขอบ, รูปตอนกดใช้กรอบเดียวกัน) ส่วน ✕×2 ใช้ทั้งภาพเหมือน spacebar ×2 (`join Fly` เดิมถูกตัดเป็น 2 ชิ้น); `InputGlyph.FitWidthToSprite` — ช่องปุ่มสูงเท่าเดิม กว้างตามรูป (`SkillGUI` ใช้ด้วย) เดิมรูปกว้างจะถูกบีบ/ยืดเข้าช่องสี่เหลี่ยม | แก้แล้ว: `feature/ps-ui` `4f20e066` |
| IP-5 | การกระทำใหม่ `CloseHint` (ต่อท้าย enum) สำหรับป้าย "PRESS Tab TO CLOSE" ในหน้าโน้ตและหน้าสอนเล่น: คีย์บอร์ด = รูปเดิม, PS = `join close`, Xbox = placeholder `Xbox/xbox_close_hint.png` (สร้างจากป้าย PS แล้วใส่ปุ่ม B); `InputGlyph` รองรับ RawImage (ป้ายนี้เป็น RawImage) | แก้แล้ว: `feature/ps-ui` `4f20e066`, `20dd0bb1` |
| UI-1 | SpriteSwap ด้วย art ใหม่: ปุ่มกลับหน้า Join (`SessionScene`), Prev/Next `Dialogue.prefab`, Prev/Next หน้าสอนเล่น (เดิม ColorTint, Next กลับด้านด้วย scale −1 แบบบทสนทนา), ปุ่ม Tutorial ในเมนู pause (เดิม ColorTint) → จอยเลือกแล้วเห็นรูป `hold` แทนกรอบ (HL-3) | แก้แล้ว: `feature/ps-ui` `20dd0bb1` |

### 26.2 ผลเทสอัตโนมัติ (Play Mode จาก `SessionScene`, Host คนเดียว, DualShock4 + Xbox + คีย์บอร์ดเสมือน) — 23/23 ผ่าน

| จุด | คีย์บอร์ด | PS | Xbox |
|---|---|---|---|
| สกิลเป็ด ดำน้ำ | `F_0` 51.6×51.6 | `R1_0` 74.0×51.6 (กด = `R1 pass_0`) | `xbox_rb` 55.7×51.6 |
| สกิลเป็ด ทุบ | `E_0` 51.6×51.6 | `jpin interact_0` 51.6×51.6 | — |
| สกิลนก บิน | `spacebar x 2` 170.5×45.2 | `join Fly` 76.0×45.2 | `xbox_a_x2` 170.5×45.2 |
| สกิลนก ปาหิน | — | `R1_0` 74.0×51.6 | — |
| ป้ายกดเหนือของ | `E_Button_0` สูง 0.297 | `jpin interact_0` สูง 0.297 | — |
| ป้ายปิดหน้าโน้ต | `Tab close` กว้าง 659.9 | `join close` 440.7 | `xbox_close_hint` 440.7 |

- ใช้คีย์บอร์ดแล้วช่องกลับขนาดเดิมพอดี
- ปุ่มกลับหน้า Join: ปกติ `back_0`, จอยเลือก `back hold_0` ไม่มีกรอบ
- บทสนทนา: Prev/Next = `back_0` (Next scale −1), hover `back hold_0`, disabled `back dis_0`
- เมนู pause: Tutorial ปกติ `how to play icon_0`, จอยเลือก `how to play icon hold_0` ไม่มีกรอบ
- หน้าสอนเล่นหน้าแรก: Prev = `back dis_0` (กดไม่ได้), Next กลับด้าน, ป้ายปิด PS = `join close`
- ภาพจอ: HUD เป็ดปุ่ม R1/□, หน้าสอนเล่นลูกศรเทา/ฟ้า, ปุ่ม ? มุมขวาล่างเมนู pause

### 26.3 ข้อสังเกต
- ยังไม่มีรูปจริง: ○ (CloseUI/Back ใช้ placeholder), รูปตอนกดของ ✕ / OPTION / △; Xbox ทั้งชุดยังเป็น placeholder
- placeholder Xbox (ทั้ง canvas 300×300) ในช่องสกิลกว้าง 55.7 (เดิม 51.6) เพราะช่องใช้อัตรายืดเดียวกับรูป E/F/Q — จะเท่ากันเองเมื่อทีม UI ส่ง art Xbox แบบเดียวกับ PS
- ชื่อไฟล์ NewUI ยังเป็นของทีม UI (`join …`, `jpin interact` สะกดผิด) — ไม่ได้เปลี่ยน เพื่อให้ทีมวาดทับชื่อเดิมได้
- `back.png`, `how to play icon.png`, `Next.png`, `Back_Not_available.png`, `Next_Not_available.png` เดิมใน `Resources/Assets/Ingame/Tutorial/` ไม่มี prefab/ฉากใดใช้แล้ว (ไม่ได้ลบ)

### 26.4 Checklist เทส — `test/all2` (MPPM หรือ build, Host + Client, จอย PS จริง)
| ขั้น | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|
| 1 | เล่นด้วยจอย PS: ดูไอคอนสกิลเป็ด/นก, เดินไปใกล้ของ | R1 / □ / ✕×2 รูปใหม่ ไม่บีบ ไม่ยืด; ป้าย □ เหนือของขนาดเท่าป้าย E | |
| 2 | เปิดโน้ต / หน้าสอนเล่น ด้วยจอย PS → จอย Xbox → คีย์บอร์ด | ป้าย "○ TO CLOSE" → "B TO CLOSE" → "PRESS Tab TO CLOSE" | |
| 3 | หน้าสอนเล่น เลื่อนหน้าด้วยจอย | ลูกศรเทาตอนกดไม่ได้, ลูกศรที่เลือกเป็นรูป hold | |
| 4 | บทสนทนา Prev/Next, ปุ่มกลับหน้า Join ด้วยเมาส์และจอย | ลูกศรใหม่, ชี้/เลือก = hold, กด = pass | |
| 5 | เมนู pause → ปุ่ม ? | รูป hold ตอนเลือก, กดแล้วเปิดหน้าสอนเล่น | |
| 6 | Host ใช้จอย PS, Client ใช้คีย์บอร์ด | แต่ละจอแสดงตามอุปกรณ์ของตัวเอง | |

## 27. Controller: B = ย้อนกลับ + แถบบอกปุ่ม A/B ขวาล่าง (2026-10-28)

**ที่มา:** เจ้าของโปรเจกต์ขอ QOL ตอนใช้จอยในเมนู — ขวาล่างบอกว่า A = ยืนยัน, B = กลับ และกด B = ย้อนไปหน้าก่อน (เหมือนกดปุ่ม Back) โดยปุ่ม Back บนจอยังเลื่อนไปกดเองได้

ผู้ใช้เลือก:
- ทำทั้ง lobby และเมนู pause ในเกม
- ในห้อง (B = ออกจากห้อง) ต้องกด B ค้าง ~0.6 วิ — Host ออกแล้วห้องปิด เพื่อนหลุด
- หน้าที่ไม่มีหน้าก่อน (เมนูหลัก, เมนู pause หน้าแรก) B ไม่ทำอะไร แถบโชว์แค่ A (Resume เป็นการโหวตสองคน ไม่ผูกกับ B)

**Branch:** `feature/back-shortcut` (แตกจาก `test/all2`) → fast-forward เข้า `test/all2`
- Unity compile 0 error
- prefab/ฉากแก้ผ่าน Unity MCP

### 27.1 สิ่งที่ทำ
| รหัส | อะไร | แก้แล้ว |
|---|---|---|
| BK-1 | `UIBackButton` ติดบนปุ่ม Back ของแต่ละหน้า (ตัวที่เปิดล่าสุดและใช้ได้จริง = ปุ่มย้อนของหน้าปัจจุบัน); `UIKeyboardNavigator` อ่าน B ใน `LateUpdate` แล้วกดปุ่มนั้นผ่าน submit เหมือนปุ่ม A; ปุ่มที่มี `holdSeconds` ต้องกดค้าง (ปล่อยก่อน/เปลี่ยนหน้า = ยกเลิก); popup ปุ่มตัวเลขและหน้าสอนเล่นที่ปิดด้วย B เอง "กิน" ปุ่มไป หน้าข้างหลังไม่ย้อนต่อ | แก้แล้ว: `feature/back-shortcut` `b1e52417` |
| BK-2 | `ControllerHintBar` (prefab `Resources/Prefabs/Interface/ControllerHintBar.prefab`): ขวาล่าง "[A] Confirm  [B] Back" รูปปุ่มตาม Xbox/PS; ขึ้นเฉพาะตอนใช้จอย; ส่วน B ขึ้นเมื่อมีปุ่มย้อนและใช้คำของปุ่มนั้น; วงรอบ B เต็มตอนกดค้าง; วางใน `SessionScene` และ `Canvas.prefab` (ในเกมขึ้นเฉพาะตอนเมนู pause/ตั้งค่าเปิด); ติด `UIBackButton`: X หน้าเลือกห้อง, ปุ่มกลับหน้า Join, X ตั้งค่า lobby, X ในห้อง (ค้าง 0.6 วิ "Hold: Leave"), X ตั้งค่าในเกม | แก้แล้ว: `feature/back-shortcut` `0399b275` |

### 27.2 ผลเทสอัตโนมัติ (Play Mode จาก `SessionScene`, Host คนเดียว, จอย Xbox + คีย์บอร์ดเสมือน) — 20/20 ผ่าน
- เมนูหลัก: แถบมีแค่ A; กด B อยู่หน้าเดิม
- หน้าเลือกห้อง → B → เมนูหลัก; ตั้งค่า → B → เมนูหลัก
- หน้า Join เปิด popup ตัวเลข: B ครั้งแรกปิด popup (ยังอยู่หน้า Join), B ครั้งที่สอง → หน้าเลือกห้อง
- ในห้อง (Host): แถบ "Hold: Leave"; แตะ B ยังอยู่; ค้าง 0.3 วิแล้วปล่อย ยังอยู่ (วง 0.43 แล้วกลับ 0); ค้าง 0.9 วิ → ออกจากห้อง
- กดคีย์บอร์ด → แถบหาย
- ในเกม: ระหว่างเล่นไม่มีแถบ; เมนู pause → แถบมีแค่ A, B ไม่ปิดเมนู; ตั้งค่า → แถบมี B, กด B → กลับเมนู pause; ปิดเมนู → แถบหาย
- ภาพจอ: แถบมุมขวาล่างทั้ง lobby / ห้อง / ตั้งค่าในเกม

### 27.3 ข้อสังเกต
- คีย์บอร์ดไม่ได้เพิ่มปุ่มย้อน (Esc ในเกมเปิดเมนู pause อยู่แล้ว) — ทำได้ถ้าต้องการ
- รูป ○ ของ PS บนแถบยังเป็น placeholder (หัวข้อ 26)
- ค่าขนาด/ตำแหน่งที่ layout คำนวณถูกบันทึกเป็น override ของแถบใน `SessionScene` — ระบบคำนวณใหม่ตอนรัน ไม่มีผล
- เพิ่มปุ่มย้อนในหน้าใหม่: ติด `UIBackButton` บนปุ่ม Back ของหน้านั้น (ตั้ง `Hold Seconds` ถ้ากดพลาดแล้วเสียหาย)

### 27.4 Checklist เทส — `test/all2` (MPPM หรือ build, Host + Client, จอยจริง)
| ขั้น | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|
| 1 | lobby ทุกหน้าด้วยจอย กด B | ย้อนกลับทีละหน้า; เมนูหลักไม่ทำอะไร; แถบขวาล่างตรงกับหน้า | |
| 2 | หน้า Join เปิด popup ตัวเลข แล้วกด B สองครั้ง | ครั้งแรกปิด popup, ครั้งสองกลับหน้าเลือกห้อง | |
| 3 | Client ในห้อง: แตะ B / กด B ค้าง | แตะไม่ออก; ค้างแล้ววงเต็มก่อนออก กลับหน้าเลือกห้อง | |
| 4 | Host ในห้อง: กด B ค้างจนออก | Client หลุดกลับ lobby ตามปกติ (เหมือนกด X) | |
| 5 | ในเกม: เมนู pause → ตั้งค่า → B; เมนู pause → B | ตั้งค่ากลับเมนู pause; หน้าแรกไม่ปิดเมนู | |
| 6 | ใช้คีย์บอร์ด/เมาส์ | แถบไม่ขึ้น, ปุ่ม Back บนจอกดได้เหมือนเดิม | |

## 28. UI ชุดใหม่ใน UI_Assets: Xbox จริง, ปุ่มตัวเลข, How to play, รูป hover + ล้าง NewUI (2026-10-28)

**ที่มา:** ทีม UI ส่งไฟล์ใหม่ใน `Assets/Resources/UI_Assets/` — `xbox/` (ปุ่ม Xbox ครบชุด + ป้าย B TO CLOSE), `playstation/` และ `back*`/`how to play*` (ซ้ำกับ `NewUI`), `How to play/` (รูปสอนเล่น 5 รูปที่มีปุ่มคีย์บอร์ด + PS + Xbox), `number room join/` (ปุ่มตัวเลขใส่เลขห้อง), `change proflie L/R` (hover ลูกศรเปลี่ยนตัวละคร) พร้อมภาพแบบ popup ตัวเลข

ผู้ใช้เลือก: รูป How to play แทนรูปเดิมทุกอุปกรณ์; `change proflie` = hover ของลูกศร; popup ตัวเลขตามแบบทีม UI (3×4, DEL-0-OK); ลบไฟล์ซ้ำใน NewUI ได้

**Branch:** `feature/ui-assets-1028` (แตกจาก `test/all2`) → fast-forward เข้า `test/all2` — Unity compile 0 error, ไฟล์/ฉากแก้ผ่าน Unity MCP

### 28.1 สิ่งที่ทำ
| รหัส | อะไร | แก้แล้ว |
|---|---|---|
| AS-2 | ไฟล์ใน `NewUI` (21 ไฟล์ พิกเซลเหมือนชุดใหม่) ย้ายไปแทนไฟล์ซ้ำใน `playstation/` และ `UI_Assets/` ด้วย `AssetDatabase.MoveAsset` — GUID/การตั้งค่า import เดิม reference ไม่ต้องแก้; ลบ `NewUI` | แก้แล้ว: `feature/ui-assets-1028` `682846a1` |
| UI-3 | รูปสอนเล่น 5 รูปเดิม (GUID เดิม) ย้ายไป `UI_Assets/How to play/` แล้วเขียนพิกเซลรูปใหม่ทับ — S1–S3, Canvas.prefab และหน้าดูย้อนหลังเปลี่ยนเอง; กรอบรูปย่อยเดิมใช้ได้ (เนื้อรูปตำแหน่งเดียวกัน) | แก้แล้ว: `feature/ui-assets-1028` `682846a1` |
| IP-6 | `InputGlyphSet` ช่อง Xbox ใช้ art จริงจาก `UI_Assets/xbox/` ครบ 13 การกระทำ (รวม B ของแถบบอกปุ่ม และป้าย B TO CLOSE แทน placeholder) import แบบเดียวกับชุด PS | แก้แล้ว: `feature/ui-assets-1028` `7e45610b` |
| UI-2 | popup ตัวเลข: พื้น `bg number`, ช่อง ENTER CODE, ปุ่ม 0–9/DEL/OK เป็น SpriteSwap (hold/pass), ซ่อนตัวหนังสือบนปุ่ม, DEL/OK ใหญ่ขึ้นเล็กน้อยตามแบบ; ช่องว่างแสดง "ENTER CODE" สีจาง; ข้อความวิธีกดย้ายไปใต้กรอบ | แก้แล้ว: `feature/ui-assets-1028` `d025b155` |
| UI-4 | hover แทนกรอบขาว: ลูกศรเปลี่ยนตัวละคร ×4 (`change proflie L/R`), ช่องใส่เลขห้องหน้า Join (`code  number hold`); ช่องพิมพ์ที่มีรูป hover ใช้ hover ได้ (HL-3) | แก้แล้ว: `feature/ui-assets-1028` `d025b155` |

### 28.2 ผลเทสอัตโนมัติ (Play Mode จาก `SessionScene`, Host คนเดียว, จอยเสมือน)
- reference ไปยังไฟล์ที่ย้าย 25 รายการ (InputGlyphSet, Canvas/Dialogue prefab, SessionScene, S1–S3) resolve ก่อน 25/25 หลัง 25/25; ไม่มีโฟลเดอร์ NewUI
- popup ตัวเลข: เปิดแล้วแสดง ENTER CODE; ปุ่มที่จอยเลือกเป็นรูป hold (ไม่มีกรอบ); พิมพ์ 1, 2 → `1 2 _ _ _ _`; DEL / X / B ใช้ได้; DEL ↔ OK วนซ้าย-ขวาได้; ภาพจอใกล้แบบทีม UI
- หน้า Join: จอยเลือกช่อง → `code  number hold` ไม่มีกรอบ
- ห้อง: จอยเลือกลูกศรของตัวเอง → `change proflie L/R` ไม่มีกรอบ (ลูกศรฝั่งเพื่อนกดไม่ได้ → กรอบ ตามเดิม)
- ในเกม (Xbox): ดำน้ำ `RB` 74.0 × 51.6, ทุบ `X` 51.6, บิน `A×2` 73.9 × 45.2, ปาหิน `RB`; ป้ายเหนือของ X; ป้ายปิด `howwto xbox close_` กว้าง 440.7; หน้าสอนเล่นเป็นรูปใหม่; แถบบอกปุ่มใช้ A/B ของจริง
- รวม 20/20 ผ่าน (รอบแรก 2 ข้อ fail เพราะ probe เลือกลูกศรฝั่งเพื่อน แก้ probe แล้วผ่าน)

### 28.3 ข้อสังเกต
- PS ยังไม่มีรูป ○ (ปิด/ย้อน) → ยังใช้ placeholder
- placeholder Xbox/PS ใน `Art-Source/UI/InputGlyphs` ไม่ได้ใช้แล้ว (ยกเว้น `ps_circle`) — ยังไม่ลบ
- รูปเก่าใน `Assets/Ingame/Tutorial/` (`back`, `how to play icon`, `Next`, `*_Not_available`) ไม่มีใครใช้ — ยังไม่ลบ
- `number room join/number.png`, `number hold.png`, `number pass.png` (ปุ่มเลขทั่วไป) ไม่ได้ใช้
- แถบบอกปุ่มยังทับปุ่ม ? ในเมนู pause (หัวข้อ 27) — รอทีม UI เลือกตำแหน่ง

### 28.4 Checklist เทส — `test/all2` (MPPM หรือ build, Host + Client, จอยจริง Xbox และ PS)
| ขั้น | ทำอะไร | ควรเห็น | ผล |
|---|---|---|---|
| 1 | Client: หน้า Join เลือกช่องด้วยจอย กด A ใส่เลขห้อง Host ด้วย popup → OK | popup ตามแบบทีม UI; ปุ่มที่เลือกเป็นรูป hold; เข้าห้องได้ | |
| 2 | เมาส์คลิกปุ่มตัวเลขใน popup | กดได้ รูป pass ตอนกด | |
| 3 | ในห้องเลื่อนไปที่ลูกศรเปลี่ยนตัวละครด้วยจอย | ลูกศรวงฟ้า ไม่มีกรอบ | |
| 4 | เล่นด้วยจอย Xbox จริง: HUD, ป้ายเหนือของ, เปิดโน้ต, แถบบอกปุ่มในเมนู pause | ปุ่ม Xbox ของจริงทุกจุด | |
| 5 | เปิดหน้าสอนเล่น (ครั้งแรก และดูย้อนหลังจากเมนู pause) | รูปใหม่ที่มีปุ่มทุกอุปกรณ์ | |
| 6 | ปุ่ม back / ? / ปุ่มจอย PS | เหมือนเดิม (ย้ายไฟล์แล้วไม่หลุด) | |
