using UnityEngine;
using System;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Configs")]
    [SerializeField] private SoundConfig[] soundList;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void PlaySoundAtPosition(string name, Vector3 position)
    {
        SoundConfig s = Array.Find(soundList, sound => sound.soundName == name);

        if (s == null)
        {
            Debug.LogWarning($"can't find '{name}' in configs");
            return;
        }

        GameObject tempAudioObj = new GameObject("TempAudio_" + name);
        tempAudioObj.transform.position = position;

        AudioSource source = tempAudioObj.AddComponent<AudioSource>();
        source.clip = s.clip;
        source.volume = s.volume;
        source.spatialBlend = 1f;

        source.Play();
        Destroy(tempAudioObj, s.clip.length);
    }
}

[System.Serializable]
public class SoundConfig
{
    public string soundName;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
}