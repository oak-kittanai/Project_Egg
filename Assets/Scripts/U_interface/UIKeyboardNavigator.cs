using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using TMPro;

public class UIKeyboardNavigator : MonoBehaviour
{
    [SerializeField] private float repeatDelay = 0.4f;
    [SerializeField] private float repeatRate = 0.1f;

    private float nextMoveTime;

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
            // ปิด keyboard/gamepad action ในตัวของ Unity เอง เพราะ UIKeyboardNavigator คุม WASD/Enter เองทั้งหมดแล้ว
            // ถ้าไม่ปิด จะมี 2 ระบบฟัง Enter ซ้อนกัน ทำให้กด 1 ครั้งเท่ากับ Submit 2 รอบ (บั๊ก "กดทะลุปุ่มถัดไป")
            // หมายเหตุ: ถ้าจะเพิ่ม Controller ในอนาคต ให้พิจารณาลบโค้ด poll คีย์บอร์ดในไฟล์นี้ทิ้ง
            // แล้วผูก Navigate/Submit/Cancel ผ่าน Input Actions asset ของ InputSystemUIInputModule แทน
            inputModule.move = null;
            inputModule.submit = null;
            inputModule.cancel = null;
            inputModule.deselectOnBackgroundClick = false; // กันคลิกพื้นที่ว่าง (เช่นคลิกเข้าจอเกมเพื่อโฟกัส) แล้ว selection หายไปเลย
        }

        if (GetComponent<SelectionHighlightFollower>() == null)
            gameObject.AddComponent<SelectionHighlightFollower>();
    }

    private GameObject lastLoggedSelection;

    private void Update()
    {
        if (EventSystem.current == null || Keyboard.current == null) return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (selected != lastLoggedSelection)
        {
            lastLoggedSelection = selected;
            Debug.Log($"[UIKeyboardNavigator] ตอนนี้เลือกอยู่ที่: {(selected != null ? selected.name : "ไม่มี (null)")}");
        }

        // ถ้ากำลังพิมพ์อยู่ในกล่อง input (เช่นช่อง Enter Room Code) ห้าม intercept WASD
        // ปล่อยให้ field จัดการคีย์บอร์ดเองทั้งหมด (พิมพ์ตัวอักษร w/a/s/d ได้ปกติ, arrow เลื่อน caret)
        if (selected != null && selected.TryGetComponent<TMP_InputField>(out var inputField) && inputField.isFocused)
            return;

        HandleSubmit(selected);
        HandleMove(selected);
    }

    private void HandleSubmit(GameObject selected)
    {
        if (!Keyboard.current.enterKey.wasPressedThisFrame && !Keyboard.current.spaceKey.wasPressedThisFrame) return;

        if (selected != null)
            ExecuteEvents.Execute(selected, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
    }

    private void HandleMove(GameObject selected)
    {
        Vector2 moveDir = Vector2.zero;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveDir.y = 1;
        else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveDir.y = -1;
        else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveDir.x = 1;
        else if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveDir.x = -1;

        if (moveDir == Vector2.zero) { nextMoveTime = 0f; return; }
        if (Time.unscaledTime < nextMoveTime) return;
        nextMoveTime = Time.unscaledTime + (nextMoveTime <= 0.0001f ? repeatDelay : repeatRate);

        if (selected == null) return;

        MoveDirection dir = moveDir.y > 0 ? MoveDirection.Up
                          : moveDir.y < 0 ? MoveDirection.Down
                          : moveDir.x > 0 ? MoveDirection.Right
                          : MoveDirection.Left;

        ExecuteEvents.Execute(selected, new AxisEventData(EventSystem.current) { moveDir = dir }, ExecuteEvents.moveHandler);
    }
}
