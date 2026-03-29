using UnityEngine;
using Fusion;

public class CameraCharacter : NetworkBehaviour
{
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
            oldPosition = transform.position.x;
            offset = transform.localPosition;

            target = transform.parent;

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

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority && gameObject != null)
        {
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
            if (onCameraTranslate != null)
            {
                float delta = oldPosition - transform.position.x;
                onCameraTranslate(delta);
            }

            oldPosition = transform.position.x;
        }
    }
}