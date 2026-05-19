using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;

public class CameraCharacter : NetworkBehaviour
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

    [Header("Boundary System")]
    [SerializeField, Tooltip("Layer กั้นแบบเด็ดขาด ห้ามกล้องทะลุ")]
    private LayerMask outOfBoundLayer;

    [SerializeField, Tooltip("Layer กั้นแบบยอมให้ทะลุขอบได้นิดหน่อย")]
    private LayerMask littleBitOfBoundLayer;

    [SerializeField, Tooltip("ระยะที่ยอมให้ทะลุได้สำหรับ LittleBitOfBound (ยิ่งมากยิ่งทะลุได้เยอะ)")]
    private float littleBitOffset = 2f;

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            LocalCamera = GetComponentInChildren<Camera>();

            oldPosition = transform.position.x;
            offset = transform.localPosition;

            target = transform.parent;

            SceneManager.activeSceneChanged += OnSceneChanged;

            if (ParallaxBackground.Instance != null)
            {
                ParallaxBackground.Instance.SetCamera(this);
            }

            if (ParallaxBackground.Instance != null)
            {
                ParallaxBackground.Instance.SetCamera(this);
            }
        }
        else
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) cam.enabled = false;

            AudioListener listener = GetComponentInChildren<AudioListener>();
            if (listener != null) listener.enabled = false;
        }
    }

    private void OnSceneChanged(Scene current, Scene next)
    {
        onCameraTranslate = null;
        Debug.Log("[Camera] Scene changed, cleared Parallax delegate.");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority && gameObject != null)
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            Destroy(gameObject);
        }
    }

    private void LateUpdate()
    {
        if (!HasInputAuthority || target == null || !target.gameObject.activeInHierarchy) return;

        Vector3 desiredPosition = target.position + offset;

        if (LocalCamera != null)
        {
            float camHeight = LocalCamera.orthographicSize;
            float camWidth = camHeight * LocalCamera.aspect;

            desiredPosition = ApplyBoundary(desiredPosition, Vector2.left, camWidth, camHeight, littleBitOfBoundLayer, littleBitOffset);
            desiredPosition = ApplyBoundary(desiredPosition, Vector2.right, camWidth, camHeight, littleBitOfBoundLayer, littleBitOffset);
            desiredPosition = ApplyBoundary(desiredPosition, Vector2.up, camWidth, camHeight, littleBitOfBoundLayer, littleBitOffset);
            desiredPosition = ApplyBoundary(desiredPosition, Vector2.down, camWidth, camHeight, littleBitOfBoundLayer, littleBitOffset);

            desiredPosition = ApplyBoundary(desiredPosition, Vector2.left, camWidth, camHeight, outOfBoundLayer, 0f);
            desiredPosition = ApplyBoundary(desiredPosition, Vector2.right, camWidth, camHeight, outOfBoundLayer, 0f);
            desiredPosition = ApplyBoundary(desiredPosition, Vector2.up, camWidth, camHeight, outOfBoundLayer, 0f);
            desiredPosition = ApplyBoundary(desiredPosition, Vector2.down, camWidth, camHeight, outOfBoundLayer, 0f);
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);

        if (transform.position.x != oldPosition)
        {
            if (onCameraTranslate != null && ParallaxBackground.Instance != null)
            {
                float delta = oldPosition - transform.position.x;
                onCameraTranslate(delta);
            }

            oldPosition = transform.position.x;
        }
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
                // ถ้ากล้องล้ำเส้นที่กำหนด ให้คำนวณระยะแล้วดันถอยหลังกลับ
                float pushBackDistance = requiredDistance - distanceToWall;
                pos -= (Vector3)(direction * pushBackDistance);
            }
        }
        return pos;
    }
}