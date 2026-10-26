using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Utilities;

public enum InputScheme
{
    KeyboardMouse,
    Xbox,
    PlayStation,
}

// รู้ว่าผู้เล่นกำลังใช้อุปกรณ์ไหน (กดล่าสุด) เพื่อเปลี่ยนรูปปุ่มบน UI ทันที (IP-1)
// ฟังอย่างเดียว ไม่กิน input — เกมยังอ่านปุ่มเองเหมือนเดิม (INetworkStructure / UIKeyboardNavigator)
// แต่ละเครื่องเปลี่ยนตามอุปกรณ์ของตัวเอง (ไม่เกี่ยวกับ network)
public class InputDeviceWatcher : MonoBehaviour
{
    public static InputScheme Current { get; private set; } = InputScheme.KeyboardMouse;
    public static bool UsingGamepad => Current != InputScheme.KeyboardMouse;
    public static event Action<InputScheme> Changed;

    // ก้านจอยต้องเกินครึ่ง และเมาส์ต้องขยับต่อเนื่องเกินระยะนี้ — กันก้าน drift / เมาส์สั่นนิดเดียวทำให้รูปกระพริบ
    private const float StickThreshold = 0.5f;
    private const float MouseMoveThreshold = 20f;

    private IDisposable buttonListener;
    private float mouseTravel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Current = InputScheme.KeyboardMouse;
        Changed = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        var go = new GameObject("[InputDeviceWatcher]");
        DontDestroyOnLoad(go);
        go.AddComponent<InputDeviceWatcher>();
    }

    private void OnEnable()
    {
        // เริ่มเกมมีจอยเสียบอยู่ = เริ่มด้วยรูปจอย
        if (Gamepad.current != null) Set(SchemeOf(Gamepad.current));

        buttonListener = InputSystem.onAnyButtonPress.Call(OnButtonPressed);
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable()
    {
        buttonListener?.Dispose();
        buttonListener = null;
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void Update()
    {
        // ก้านจอยไม่ใช่ปุ่ม จึงไม่ผ่าน onAnyButtonPress
        var pads = Gamepad.all;
        for (int i = 0; i < pads.Count; i++)
        {
            Gamepad pad = pads[i];
            if (pad.leftStick.ReadValue().sqrMagnitude > StickThreshold * StickThreshold
                || pad.rightStick.ReadValue().sqrMagnitude > StickThreshold * StickThreshold)
            {
                Set(SchemeOf(pad));
                break;
            }
        }

        // เมาส์: นับเฉพาะการขยับต่อเนื่อง (เฟรมไหนไม่ขยับ เริ่มนับใหม่)
        if (UsingGamepad && Mouse.current != null)
        {
            float moved = Mouse.current.delta.ReadValue().magnitude;
            mouseTravel = moved > 0f ? mouseTravel + moved : 0f;
            if (mouseTravel > MouseMoveThreshold) Set(InputScheme.KeyboardMouse);
        }
        else
        {
            mouseTravel = 0f;
        }
    }

    private void OnButtonPressed(InputControl control)
    {
        InputDevice device = control.device;
        if (device is Gamepad pad) Set(SchemeOf(pad));
        else if (device is Keyboard || device is Mouse) Set(InputScheme.KeyboardMouse);
    }

    // ถอดจอยออกจนไม่เหลือ -> กลับเป็นรูปคีย์บอร์ด
    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Removed && device is Gamepad && Gamepad.all.Count == 0)
            Set(InputScheme.KeyboardMouse);
    }

    // PS4/PS5 = DualShockGamepad (DualSense สืบมาจากตัวนี้); จอยอื่น/ไม่รู้จัก = Xbox
    // หมายเหตุ: เล่นผ่าน Steam Input / DS4Windows จอย PS จะถูกมองเป็นจอย Xbox
    private static InputScheme SchemeOf(Gamepad pad) => pad is DualShockGamepad ? InputScheme.PlayStation : InputScheme.Xbox;

    private static void Set(InputScheme scheme)
    {
        if (scheme == Current) return;
        Current = scheme;
        Changed?.Invoke(scheme);
    }
}
