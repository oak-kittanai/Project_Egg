using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

public class UIKeyboardNavigator : MonoBehaviour
{
    [SerializeField] private float repeatDelay = 0.4f;
    [SerializeField] private float repeatRate = 0.1f;

    private float nextMoveTime;

    // ตัวที่เลือกล่าสุดที่ยังใช้ได้ — ไว้หาหน้าจอเดิมตอนตัวเลือกหายไป (NV-1)
    private GameObject lastSeenSelected;
    private GameObject lastUsableSelected;
    private static Selectable[] selectableBuffer = new Selectable[64];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LockCursorOnBoot()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Awake()
    {
        var inputModule = GetComponent<InputSystemUIInputModule>();
        if (inputModule != null)
        {
            inputModule.move = null;
            inputModule.submit = null;
            inputModule.cancel = null;
            inputModule.deselectOnBackgroundClick = false;
        }

        if (GetComponent<SelectionHighlightFollower>() == null)
            gameObject.AddComponent<SelectionHighlightFollower>();
    }


    private void Update()
    {
        if (EventSystem.current == null) return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;

        // ถ้ากำลังพิมพ์อยู่ในกล่อง input (เช่นช่อง Enter Room Code) ห้าม intercept WASD
        // ปล่อยให้ field จัดการคีย์บอร์ดเองทั้งหมด (พิมพ์ตัวอักษร w/a/s/d ได้ปกติ, arrow เลื่อน caret)
        if (selected != null && selected.TryGetComponent<TMP_InputField>(out var inputField) && inputField.isFocused)
            return;

        if (selected != lastSeenSelected)
        {
            lastSeenSelected = selected;
            if (IsUsable(selected)) lastUsableSelected = selected;
        }

        HandleSubmit(selected);
        HandleMove(selected);
    }

    private void HandleSubmit(GameObject selected)
    {
        bool kbSubmit = Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame);
        bool gpSubmit = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
        if (!kbSubmit && !gpSubmit) return;

        if (selected != null)
            ExecuteEvents.Execute(selected, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
    }

    private void HandleMove(GameObject selected)
    {
        Vector2 moveDir = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveDir.y = 1;
            else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveDir.y = -1;
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveDir.x = 1;
            else if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveDir.x = -1;
        }

        if (moveDir == Vector2.zero && Gamepad.current != null)
        {
            var gp = Gamepad.current;
            if (gp.leftStick.up.isPressed || gp.dpad.up.isPressed) moveDir.y = 1;
            else if (gp.leftStick.down.isPressed || gp.dpad.down.isPressed) moveDir.y = -1;
            else if (gp.leftStick.right.isPressed || gp.dpad.right.isPressed) moveDir.x = 1;
            else if (gp.leftStick.left.isPressed || gp.dpad.left.isPressed) moveDir.x = -1;
        }

        if (moveDir == Vector2.zero) { nextMoveTime = 0f; return; }
        if (Time.unscaledTime < nextMoveTime) return;
        nextMoveTime = Time.unscaledTime + (nextMoveTime <= 0.0001f ? repeatDelay : repeatRate);

        // ไม่มีอะไรถูกเลือก หรือหน้าที่เลือกไว้ปิดไปแล้ว (เช่นปิดเมนู pause/dialogue แล้วกำลังเล่น) — ไม่แตะ
        // ไม่ให้คันโยกตอนเดินไปเลือกปุ่มบน HUD
        if (selected == null || !selected.activeInHierarchy) return;

        // ตัวที่เลือกยังอยู่บนจอแต่ใช้ไม่ได้ (กดไม่ได้/ถูกซ่อนด้วย alpha/canvas ปิด): กดทิศแล้วเลือกปุ่มบนหน้าจอเดียวกันใหม่ (NV-1)
        if (!IsUsable(selected))
        {
            Selectable fallback = FirstUsable(RootCanvasOf(selected) ?? RootCanvasOf(lastUsableSelected));
            if (fallback != null) EventSystem.current.SetSelectedGameObject(fallback.gameObject);
            return;
        }

        MoveDirection dir = moveDir.y > 0 ? MoveDirection.Up
                          : moveDir.y < 0 ? MoveDirection.Down
                          : moveDir.x > 0 ? MoveDirection.Right
                          : MoveDirection.Left;

        Canvas root = RootCanvasOf(selected);
        ExecuteEvents.Execute(selected, new AxisEventData(EventSystem.current) { moveDir = dir }, ExecuteEvents.moveHandler);

        // ปุ่มถัดไปของ Unity มาจาก Selectable ทุกตัวในเกม — ถ้าหลุดไป canvas อื่นหรือของที่ซ่อนอยู่
        // ให้หาปุ่มในทิศเดียวกันบนหน้าจอเดิมแทน ไม่เจอก็อยู่ที่เดิม (NV-1); สไลเดอร์ซ้าย/ขวายังปรับค่าเหมือนเดิม
        GameObject after = EventSystem.current.currentSelectedGameObject;
        if (after == selected || (IsUsable(after) && RootCanvasOf(after) == root)) return;

        Selectable next = FindInDirection(selected.GetComponent<Selectable>(), root, dir);
        EventSystem.current.SetSelectedGameObject(next != null ? next.gameObject : selected);
    }

    private static Canvas RootCanvasOf(GameObject go)
    {
        if (go == null) return null;
        Canvas canvas = go.GetComponentInParent<Canvas>(true);
        return canvas != null ? canvas.rootCanvas : null;
    }

    // เลือกได้จริงและมองเห็น: active, กดได้, canvas เปิดอยู่, ไม่อยู่ใต้ CanvasGroup ที่ alpha 0
    private static bool IsUsable(GameObject go)
    {
        if (go == null || !go.activeInHierarchy) return false;
        if (!go.TryGetComponent<Selectable>(out var selectable) || !selectable.IsInteractable()) return false;

        Canvas root = RootCanvasOf(go);
        if (root == null || !root.isActiveAndEnabled) return false;

        for (Transform t = go.transform; t != null; t = t.parent)
        {
            if (!t.TryGetComponent<CanvasGroup>(out var group)) continue;
            if (group.alpha <= 0.01f) return false;
            if (group.ignoreParentGroups) break;
        }
        return true;
    }

    private static int CollectSelectables()
    {
        if (selectableBuffer.Length < Selectable.allSelectableCount)
            selectableBuffer = new Selectable[Selectable.allSelectableCount * 2];
        return Selectable.AllSelectablesNoAlloc(selectableBuffer);
    }

    private static Vector3 CenterOf(Selectable selectable)
    {
        var rect = (RectTransform)selectable.transform;
        return rect.TransformPoint(rect.rect.center);
    }

    // ปุ่มที่ใกล้และตรงทิศที่สุดบน canvas เดียวกัน (คะแนนแบบ Unity: ระยะตามทิศ / ระยะยกกำลังสอง)
    private static Selectable FindInDirection(Selectable from, Canvas root, MoveDirection dir)
    {
        if (from == null || root == null) return null;

        Vector3 localDir = dir == MoveDirection.Up ? Vector3.up
                         : dir == MoveDirection.Down ? Vector3.down
                         : dir == MoveDirection.Right ? Vector3.right
                         : Vector3.left;
        Vector3 worldDir = root.transform.rotation * localDir;
        Vector3 origin = CenterOf(from);

        Selectable best = null;
        float bestScore = float.NegativeInfinity;
        int count = CollectSelectables();
        for (int i = 0; i < count; i++)
        {
            Selectable candidate = selectableBuffer[i];
            if (candidate == from || candidate.navigation.mode == Navigation.Mode.None) continue;
            if (!IsUsable(candidate.gameObject) || RootCanvasOf(candidate.gameObject) != root) continue;

            Vector3 offset = CenterOf(candidate) - origin;
            float along = Vector3.Dot(worldDir, offset);
            if (along <= 0f) continue;

            float score = along / offset.sqrMagnitude;
            if (score > bestScore) { bestScore = score; best = candidate; }
        }
        return best;
    }

    // ปุ่มบนสุดของหน้าจอ (canvas เดิม ถ้ารู้) — ไว้เลือกใหม่ตอนตัวเลือกหาย
    private static Selectable FirstUsable(Canvas root)
    {
        Selectable best = null;
        float bestY = float.NegativeInfinity;
        int count = CollectSelectables();
        for (int i = 0; i < count; i++)
        {
            Selectable candidate = selectableBuffer[i];
            if (!IsUsable(candidate.gameObject)) continue;
            if (root != null && RootCanvasOf(candidate.gameObject) != root) continue;

            float y = CenterOf(candidate).y;
            if (y > bestY) { bestY = y; best = candidate; }
        }
        return best;
    }
}
