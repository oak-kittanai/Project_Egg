using UnityEngine;

public class MouseClickSound : MonoBehaviour
{
    [Header("Audio Setting")]
    public AudioSource clickAudioSource;
    public AudioClip clickClip;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (AudioSession.Instance != null && clickClip != null)
            {
                AudioSession.Instance.PlayClickSound(clickClip);
            }
        }
    }
}