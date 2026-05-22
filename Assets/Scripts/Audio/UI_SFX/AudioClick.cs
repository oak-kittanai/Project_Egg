using UnityEngine;

public class AudioClick : MonoBehaviour
{
    [Header("Audio Setting")]
    public string clickSoundName = "Click";

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayDirectSound(clickSoundName);
            }
        }
    }
}