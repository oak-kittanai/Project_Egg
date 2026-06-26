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

    [Header("Boundary System")]
    [SerializeField] private LayerMask outOfBoundLayer;

    [SerializeField] private LayerMask littleBitOfBoundLayer;

    [SerializeField] private float littleBitOffset = 2f;

    public void InitializeAsLocal(Transform followTarget)
    {
        if (isLocalCamera) return;

        if (LocalCamera != null && LocalCamera.gameObject != gameObject)
        {
            Debug.LogWarning("[Camera] Destroying old leftover camera");
            Destroy(LocalCamera.gameObject);
        }

        isLocalCamera = true;
        LocalCamera = GetComponentInChildren<Camera>();

        offset = transform.localPosition;
        target = followTarget;
        oldPosition = transform.position.x;

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        SceneManager.activeSceneChanged += OnSceneChanged;

        if (ParallaxBackground.Instance != null)
            ParallaxBackground.Instance.SetCamera(this);
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
        }
    }

    private void OnSceneChanged(Scene current, Scene next)
    {
        onCameraTranslate = null;
    }

    private void LateUpdate()
    {
        if (!isLocalCamera || target == null || !target.gameObject.activeInHierarchy) return;

        Vector3 desiredPosition = target.position + offset;

        Vector3 smoothed = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);

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
                float pushBackDistance = requiredDistance - distanceToWall;
                pos -= (Vector3)(direction * pushBackDistance);
            }
        }
        return pos;
    }
}