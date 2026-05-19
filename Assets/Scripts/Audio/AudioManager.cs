using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Configs")]
    [SerializeField] private SoundConfig[] soundList;

    [Header("BGM Settings")]
    [SerializeField] private AudioSource bgmSource;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        FindAudioComponents(SceneManager.GetActiveScene());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindAudioComponents(scene);
    }

    private void FindAudioComponents(Scene scene)
    {
        GameObject bgmObj = GameObject.Find("BGM_Player");

        if (bgmObj != null)
        {
            bgmSource = bgmObj.GetComponent<AudioSource>();
            Debug.Log($"[AudioManager] found BGM_Player in: {scene.name}");
        }
        else
        {
            Debug.Log($"[AudioManager] can't found BGM_Player in: {scene.name}");
            bgmSource = null;
        }
    }

    public void PlaySoundAtPosition(string name, Vector3 position)
    {
        SoundConfig s = Array.Find(soundList, sound => sound.soundName == name);

        if (s == null)
        {
            Debug.LogWarning($"[AudioManager] can't find '{name}'Config");
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

    public void PlayBGM(string name)
    {
        if (bgmSource == null)
        {
            Debug.LogWarning($"[AudioManager] bgmSource not ready can't play BGM '{name}'");
            return;
        }

        SoundConfig s = Array.Find(soundList, sound => sound.soundName == name);

        if (s == null)
        {
            Debug.LogWarning($"[AudioManager] BGM can't find '{name}'");
            return;
        }

        if (bgmSource.clip == s.clip && bgmSource.isPlaying) return;

        bgmSource.clip = s.clip;
        bgmSource.volume = s.volume;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.Stop();
    }
}

[System.Serializable]
public class SoundConfig
{
    public string soundName;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
}