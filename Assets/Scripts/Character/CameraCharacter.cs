using UnityEngine;
using Fusion;

public class CameraCharacter : NetworkBehaviour
{
    public delegate void ParallaxCameraDelegate(float deltaMovement);
    public ParallaxCameraDelegate onCameraTranslate;

    private float oldPosition;

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            oldPosition = transform.position.x;

            if (ParallaxBackground.Instance != null)
            {
                ParallaxBackground.Instance.SetCamera(this);
            }

            gameObject.tag = "Camera";
        }
        else
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) cam.enabled = false;

            AudioListener listener = GetComponentInChildren<AudioListener>();
            if (listener != null) listener.enabled = false;
        }
    }
    void LateUpdate()
    {
        if (!Object.HasInputAuthority) return;

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