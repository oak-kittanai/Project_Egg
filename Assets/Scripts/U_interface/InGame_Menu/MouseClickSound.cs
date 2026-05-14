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
            if (clickAudioSource != null && clickClip != null)
            {
                clickAudioSource.PlayOneShot(clickClip);
            }
        }
    }
}