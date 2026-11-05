---
name: unity-mcp-editor-ops
description: How to change scenes, prefabs, animator controllers and assets in Easter Bond (Project_Egg) through the Unity MCP (mcp__unity-mcp__*) instead of editing YAML, how to run automated Play Mode probes, check compile errors, and what to do when MCP is unavailable. Use whenever a task touches .unity / .prefab / .controller / .asset files, needs Console/compile results, or needs an in-game test.
---

# Unity MCP — แก้ Editor และเทสตอนรัน

**กฎ CLAUDE.md:** ห้ามแก้ `.unity` / `.prefab` / `.controller` ด้วยการ edit YAML ตรง ๆ — ใช้ Unity MCP ถ้าต่อได้ ถ้าไม่ได้ให้เขียนขั้นตอนให้ผู้ใช้ทำใน Editor

## 1. ก่อนเริ่ม
- MCP มาจาก package `com.unity.ai.assistant` 2.20.0-pre.2 (เคย "Connection revoked" ก่อน 2.20) — โหลด tool ด้วย ToolSearch `unity-mcp`
- Unity 6000.2.6f2, URP 2D Renderer
- **เช็คฉากที่เปิดและสถานะ dirty ก่อนทุกครั้ง** ฉากที่ Editor เปิดอาจเปลี่ยนกลางงาน; ฉาก dirty ของผู้ใช้ (SessionScene, Stage1-S1) **ห้ามเซฟแทน**
- ฉาก `.unity` เป็น Git LFS: ดู diff จริงด้วย `git show HEAD:<path> | git lfs smudge`
- เซฟฉากทำให้ Unity สร้าง Mesh ใหม่ในฉาก — ถ้าจะบอกว่า "เปลี่ยนแค่ค่านี้" ให้กรอง class 43 ออกก่อนเทียบ
- ห้ามแก้ Build List เอง (ผู้ใช้สั่งไว้) — `EditorBuildSettings.asset` ต้องให้ผู้ใช้เซฟ

## 2. ข้อจำกัดของ `Unity_RunCommand`
| ทำไม่ได้ | ทางแทน |
|---|---|
| อ้าง type ที่สืบจาก Fusion (assembly ของ MCP ไม่ reference `Fusion.Runtime`) | `root.GetComponentInChildren(System.Type.GetType("SessionHub, MainGame.Core"), true)` + `SerializedObject` |
| `System.Reflection`, `System.Text.RegularExpressions` | `SerializedObject` / `SerializedProperty`; string ธรรมดา |
| `AssetDatabase.DeleteAsset` ("User interactions are not supported") | ลบไฟล์ + `.meta` ผ่าน shell แล้ว `AssetDatabase.Refresh()` |
| ชื่อ `Image` ชน | alias `UnityEngine.UI.Image` |
| `CompilationPipeline` ชน namespace | เรียกเต็ม `UnityEditor.Compilation.CompilationPipeline` |

ทำได้: `AssetDatabase.RenameAsset` / `MoveAsset` (คง GUID), `UnityEditor.U2D.Sprites` (ISpriteEditorDataProvider), แก้ importer ผ่าน `SerializedObject(importer)`

## 3. เทสตอนรันด้วย probe (Host คนเดียว)
1. เขียน probe MonoBehaviour ชั่วคราว (เช่น `Assets/Scripts/_Verify/XxxProbe.cs`) ใช้ `[RuntimeInitializeOnLoadMethod]` สร้างตัวเอง; ป้อน input ผ่าน `INetworkRunnerCallbacks.OnInput` (`input.TryGet`/`Set` ทับทีละ tick)
2. เริ่มจาก lobby โดยไม่เปลี่ยนฉากที่ผู้ใช้เปิด: `EditorSceneManager.playModeStartScene = <SessionScene asset>` แล้วตั้งกลับเป็น `null` ตอนจบ
3. `EditorApplication.isPlaying = true` → สั่งเริ่มด้วย `GameObject.Find("<Probe>").SendMessage("Begin")`
4. Duo Host คนเดียว: `SessionManager.GenerateCode()` → `StartGame()` → `PlayerInterface.Instance.skipButton.onClick.Invoke()` → เรียก `GameManager.MapFinishedLoading()` / `PlayerFinishedLoading()` แทนผู้เล่นคนที่สอง; Solo: เรียก `StartSoloGame`
5. อ่านผล: `grep -a "\[PROBE\]" "$LOCALAPPDATA/Unity/Editor/Editor.log"` (ไฟล์อาจมี NUL ใช้ `-a`)
6. Error Pause ทำ Photon หลุด → probe สมัคร `EditorApplication.pauseStateChanged` ให้ unpause เอง
7. Input เสมือน: คีย์บอร์ดเสมือนไม่เข้าเกมถ้า Game view ไม่ focus → ตั้งชั่วคราว `InputSystem.settings.editorInputBehaviorInPlayMode = AllDeviceInputAlwaysGoesToGameView` + `backgroundBehavior = IgnoreFocus`; จอย: `InputSystem.AddDevice<Gamepad>()` + `QueueStateEvent` ค้าง 2 frame; ทุกอุปกรณ์รวม DS4: `StateEvent.From` + `control.WriteValueIntoEvent` (ห้ามเขียน dpad DS4) — **ลบอุปกรณ์เสมือนเมื่อจบ** (`InputSystem.RemoveDevice`) เพราะเครื่องผู้ใช้มีจอยจริง
8. ภาพจอ: `ScreenCapture.CaptureScreenshot(path, 3)` (`Unity_Camera_Capture` ใช้กับกล้องหลายตัวของเกมไม่ได้); ใช้ `CameraCharacter.LocalCamera` ไม่ใช่ `Camera.main`; ใน `Level1_solo` ถ่ายที่จุดเกิด + 5 หน่วย (ต้นสนบัง)
9. ทุกเคส "ไม่ควรเกิดผล" ต้องมีตัวคุมในตำแหน่งเดียวกัน; เกณฑ์ระยะตายตัวพลาดเมื่อของถึงก้นบ่อ ให้เช็คจุดหยุดแทน
10. จบแล้ว: ลบ probe ออกจากโปรเจกต์ (เก็บสำรองไว้ใน scratchpad), คืนไฟล์ที่ Play Mode เขียนทับด้วย git (ดู skill `mppm-2player-test` ข้อ 5)

probe เทสได้แค่ฝั่ง Host — ยังต้องมี checklist 2 คนเสมอ ผู้ใช้ไม่ต้องการเทส UI ด้วย probe แล้ว เทสเฉพาะในเกม (Player/Trap/pause)

## 4. Compile check
- MCP: `Unity_GetConsoleLogs` หลัง Unity compile เสร็จ — ต้อง 0 error
- ถ้า MCP ไม่ได้: อ่าน `%LOCALAPPDATA%\Unity\Editor\Editor.log` หา `error CS` / `Tundra build` และเทียบเวลา `Library/ScriptAssemblies/MainGame.Core.dll` กับไฟล์ที่แก้
- Unity ไม่ได้เปิด: Roslyn — สร้าง response file จาก `MainGame.Core.csproj` (HintPath + `Library/ScriptAssemblies/*.dll` + `DefineConstants`) รวม `Assets/Scripts/**/*.cs` แล้ว `dotnet "C:/Program Files/dotnet/sdk/<ver>/Roslyn/bincore/csc.dll" @rsp -nostdlib+ -unsafe+ -langversion:9.0 -target:library` (ไม่ผ่าน Fusion IL weaving)

## 5. ถ้า MCP ใช้ไม่ได้ — เขียนขั้นตอนให้ผู้ใช้
รูปแบบ: ฉาก/prefab → path ใน Hierarchy → component → field → ค่าเดิม → ค่าใหม่ แล้วให้ผู้ใช้กด Save เอง เช่น
```
1. เปิด Assets/Scenes/Level1_solo.unity
2. Hierarchy: ===== Solo Zones ===== / <ชื่อ object> → SoloZoneTrigger → Hint Seconds: 5 → 3
3. Ctrl+S
```
