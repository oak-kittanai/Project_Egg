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

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            LocalCamera = GetComponentInChildren<Camera>();

            oldPosition = transform.position.x;
            offset = transform.localPosition;

            target = transform.parent;

            DontDestroyOnLoad(gameObject);

            SceneManager.activeSceneChanged += OnSceneChanged;

            if (ParallaxBackground.Instance != null)
            {
                ParallaxBackground.Instance.SetCamera(this);
            }

            transform.SetParent(null);

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
        if (!HasInputAuthority || target == null) return;

        Vector3 desiredPosition = target.position + offset;
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
}