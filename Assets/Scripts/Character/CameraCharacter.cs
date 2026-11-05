using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraCharacter : MonoBehaviour
{
    public static Camera LocalCamera { get; private set; }

    public delegate void ParallaxCameraDelegate(float deltaMovement);
    public ParallaxCameraDelegate onCameraTranslate;

    private float oldPosition;

    [Header("Camera Smoothing"), Tooltip("(0.05|low value = fast follow, 0.1|high value = slow follower/Smooth)")]
    [SerializeField] private float smoothTime = 0.08f;
    private Vector3 velocity = Vector3.zero;

    private Transform target;
    private Vector3 offset;
    private bool isLocalCamera = false;

    // AudioListener ของผู้เล่น local — ตามกล้อง เพราะ SFX เป็นเสียง 3D (AudioManager: ได้ยินไม่เกิน 20 หน่วย)
    // listener ที่วางนิ่งในด่าน (UI_Camera) อยู่ไกลจากผู้เล่นเกือบทั้งด่าน ทำให้เกมเงียบ
    private AudioListener localListener;

    [Header("Boundary System")]
    [SerializeField] private LayerMask outOfBoundLayer;

    [SerializeField] private LayerMask littleBitOfBoundLayer;

    [SerializeField] private float littleBitOffset = 2f;

    // CM-1: มองไปข้างหน้าตามทิศที่ตัวละครเคลื่อน (คิดจากตำแหน่ง target ทุก frame — ใช้ได้ทั้ง Duo และ Solo หลังสลับตัว)
    [Header("Look Ahead (CM-1)")]
    [SerializeField] private bool useLookAhead = true;
    [Tooltip("ระยะที่กล้องเลื่อนไปข้างหน้า (หน่วยโลก) — ครึ่งความกว้างจอที่ size 4 คือ ~7 หน่วย")]
    [SerializeField] private float lookAheadDistance = 1f;
    [Tooltip("ความเร็วแนวนอนขั้นต่ำ (หน่วย/วิ) ที่นับว่ากำลังเคลื่อนไปทางนั้น")]
    [SerializeField] private float lookAheadMinSpeed = 1f;
    [SerializeField] private float lookAheadSmoothTime = 0.5f;

    // CM-2: กล้องสั่นตอนผู้เล่น local เสียเลือด — สั่นเฉพาะภาพ ไม่กระทบ SmoothDamp, ขอบด่าน และ parallax
    [Header("Shake (CM-2)")]
    [SerializeField] private float damageShakeAmplitude = 0.15f;
    [SerializeField] private float damageShakeDuration = 0.25f;
    [SerializeField] private float shakeFrequency = 25f;

    private static CameraCharacter localRig;

    private float lookAheadX;
    private float lookAheadVelocity;
    private float lookAheadDir;
    private Vector3 lastTargetPosition;
    private Transform lastTarget;

    private float shakeAmplitude;
    private float shakeDuration;
    private float shakeTimeLeft;
    private float shakeSeed;
    private Vector3 appliedShake;

    public void InitializeAsLocal(Transform followTarget)
    {
        if (isLocalCamera) return;

        if (LocalCamera != null && LocalCamera.gameObject != gameObject)
        {
            Debug.LogWarning("[Camera] Destroying old leftover camera");
            Destroy(LocalCamera.gameObject);
        }

        isLocalCamera = true;
        localRig = this;
        LocalCamera = GetComponentInChildren<Camera>();

        offset = transform.localPosition;
        target = followTarget;
        oldPosition = transform.position.x;

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        SceneManager.activeSceneChanged += OnSceneChanged;

        if (ParallaxBackground.Instance != null)
            ParallaxBackground.Instance.SetCamera(this);

        ClaimAudioListener();
    }

    private void ClaimAudioListener()
    {
        // ปิด listener ตัวอื่นก่อน (กัน warning ว่ามี AudioListener 2 ตัว)
        foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (listener != localListener) listener.enabled = false;
        }

        if (localListener == null)
        {
            GameObject listenerObj = new GameObject("LocalAudioListener");
            listenerObj.transform.SetParent(transform, false);
            localListener = listenerObj.AddComponent<AudioListener>();
        }

        localListener.enabled = true;
    }

    // โหมด Solo: กล้องตามตัวที่คุมอยู่
    public void SetFollowTarget(Transform followTarget)
    {
        target = followTarget;
    }

    public void DisableForRemote()
    {
        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null) cam.enabled = false;

        AudioListener listener = GetComponentInChildren<AudioListener>();
        if (listener != null) listener.enabled = false;

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (isLocalCamera)
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            if (LocalCamera != null && LocalCamera.gameObject == this.gameObject)
                LocalCamera = null;
            if (localRig == this) localRig = null;
        }
    }

    private void OnSceneChanged(Scene current, Scene next)
    {
        onCameraTranslate = null;

        // ด่านใหม่มี AudioListener ของตัวเองติดมาด้วย — ปิดแล้วใช้ตัวของกล้อง local ต่อ
        ClaimAudioListener();
    }

    private void LateUpdate()
    {
        if (!isLocalCamera || target == null || !target.gameObject.activeInHierarchy) return;

        // เอาการสั่นของ frame ก่อนออกก่อน — ตำแหน่งฐานต้องไม่มีการสั่นปน (ถ้าโค้ดอื่นย้ายกล้อง ค่าที่ย้ายยังอยู่ครบ)
        Vector3 basePosition = transform.position - appliedShake;
        appliedShake = Vector3.zero;

        Vector3 desiredPosition = target.position + offset;
        desiredPosition.x += UpdateLookAhead();

        Vector3 smoothed = Vector3.SmoothDamp(basePosition, desiredPosition, ref velocity, smoothTime);

        if (LocalCamera != null)
        {
            float camHeight = LocalCamera.orthographicSize;
            float camWidth = camHeight * LocalCamera.aspect;

            smoothed = ApplyBoundary(smoothed, Vector2.left, camWidth, camHeight, littleBitOfBoundLayer, littleBitOffset);
            smoothed = ApplyBoundary(smoothed, Vector2.right, camWidth, camHeight, littleBitOfBoundLayer, littleBitOffset);
            smoothed = ApplyBoundary(smoothed, Vector2.up, camWidth, camHeight, littleBitOfBoundLayer, littleBitOffset);
            smoothed = ApplyBoundary(smoothed, Vector2.down, camWidth, camHeight, littleBitOfBoundLayer, littleBitOffset);

            smoothed = ApplyBoundary(smoothed, Vector2.left, camWidth, camHeight, outOfBoundLayer, 0f);
            smoothed = ApplyBoundary(smoothed, Vector2.right, camWidth, camHeight, outOfBoundLayer, 0f);
            smoothed = ApplyBoundary(smoothed, Vector2.up, camWidth, camHeight, outOfBoundLayer, 0f);
            smoothed = ApplyBoundary(smoothed, Vector2.down, camWidth, camHeight, outOfBoundLayer, 0f);
        }

        transform.position = smoothed;

        // วาง listener ที่ระนาบ z ของตัวละคร (กล้องอยู่ห่างออกมาในแกน z ทำให้เสียงเบาลงโดยไม่จำเป็น)
        if (localListener != null)
            localListener.transform.position = new Vector3(smoothed.x, smoothed.y, target.position.z);

        if (transform.position.x != oldPosition)
        {
            if (onCameraTranslate != null && ParallaxBackground.Instance != null)
            {
                float delta = oldPosition - transform.position.x;
                onCameraTranslate(delta);
            }
            oldPosition = transform.position.x;
        }

        // สั่นหลังคิด parallax และ listener แล้ว — parallax ไม่สั่นตาม และ delta ไม่เพี้ยน
        appliedShake = UpdateShake();
        if (appliedShake != Vector3.zero) transform.position += appliedShake;
    }

    // คืนระยะ look-ahead ตามแกน x; ทิศคงค้างไว้เมื่อหยุด กล้องจึงไม่ดีดกลับทุกครั้งที่ปล่อยปุ่ม
    private float UpdateLookAhead()
    {
        if (!useLookAhead || lookAheadDistance <= 0f)
        {
            lookAheadX = 0f;
            lookAheadVelocity = 0f;
            return 0f;
        }

        Vector3 targetPosition = target.position;

        // target ใหม่ (Solo สลับตัว) หรือกระโดดไกลในเฟรมเดียว (เกิดใหม่/วาร์ป) ไม่นับเป็นความเร็ว
        if (target != lastTarget)
        {
            lastTarget = target;
            lastTargetPosition = targetPosition;
        }

        float dt = Time.deltaTime;
        if (dt > 0f)
        {
            float speedX = (targetPosition.x - lastTargetPosition.x) / dt;
            bool teleported = Mathf.Abs(targetPosition.x - lastTargetPosition.x) > lookAheadDistance * 4f + 1f;
            if (!teleported && Mathf.Abs(speedX) >= lookAheadMinSpeed)
                lookAheadDir = Mathf.Sign(speedX);
        }
        lastTargetPosition = targetPosition;

        lookAheadX = Mathf.SmoothDamp(lookAheadX, lookAheadDir * lookAheadDistance, ref lookAheadVelocity, lookAheadSmoothTime);
        return lookAheadX;
    }

    private Vector3 UpdateShake()
    {
        if (shakeTimeLeft <= 0f) return Vector3.zero;

        shakeTimeLeft -= Time.deltaTime;
        if (shakeTimeLeft <= 0f) return Vector3.zero;

        float strength = shakeAmplitude * (shakeTimeLeft / shakeDuration);
        float t = Time.time * shakeFrequency;
        float x = Mathf.PerlinNoise(shakeSeed, t) * 2f - 1f;
        float y = Mathf.PerlinNoise(shakeSeed + 37.1f, t) * 2f - 1f;
        return new Vector3(x, y, 0f) * strength;
    }

    // สั่นกล้องของเครื่องนี้ — เรียกจากโค้ดที่ gate ด้วย HasInputAuthority แล้วเท่านั้น (ไม่ใช่จาก FixedUpdateNetwork)
    public static void Shake(float amplitude, float duration)
    {
        if (localRig == null || amplitude <= 0f || duration <= 0f) return;

        // ซ้อนกันได้: เอาค่าที่แรงกว่า ไม่บวกสะสม
        if (localRig.shakeTimeLeft > 0f)
        {
            float current = localRig.shakeAmplitude * (localRig.shakeTimeLeft / localRig.shakeDuration);
            if (current >= amplitude) return;
        }

        localRig.shakeAmplitude = amplitude;
        localRig.shakeDuration = duration;
        localRig.shakeTimeLeft = duration;
        localRig.shakeSeed = Random.Range(0f, 100f);
    }

    public static void ShakeOnDamage()
    {
        if (localRig == null) return;
        Shake(localRig.damageShakeAmplitude, localRig.damageShakeDuration);
    }

    private Vector3 ApplyBoundary(Vector3 pos, Vector2 direction, float halfWidth, float halfHeight, LayerMask layerMask, float allowedOffset)
    {
        if (layerMask == 0) return pos;

        float checkDistance = (direction.x != 0) ? halfWidth : halfHeight;
        float lookAhead = 100f;

        RaycastHit2D hit = Physics2D.Raycast(pos, direction, checkDistance + lookAhead, layerMask);

        if (hit.collider != null)
        {
            float distanceToWall = hit.distance;
            float requiredDistance = checkDistance - allowedOffset;

            if (distanceToWall < requiredDistance)
            {
                float pushBackDistance = requiredDistance - distanceToWall;
                pos -= (Vector3)(direction * pushBackDistance);
            }
        }
        return pos;
    }
}