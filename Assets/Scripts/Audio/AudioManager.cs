using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Configs")]
    [SerializeField] private SoundConfig[] soundBGMList;
    [SerializeField] private SoundConfig[] playerSoundList;
    [SerializeField] private SoundConfig[] sfxSoundList;
    [SerializeField] private SoundConfig[] assetsSoundList;

    [Header("Global Settings")]
    [SerializeField] private GameSettingsSO gameSettings;

    [Header("BGM Settings")]
    [SerializeField] private AudioSource uiSource;
    [SerializeField] private AudioSource bgmSource;
    private float currentBgmBaseVolume = 1f;

    [Header("SFX Pool Settings")]
    [SerializeField] private int sfxPoolSize = 15;
    private List<AudioSource> sfxPool = new List<AudioSource>();

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (uiSource == null) uiSource = GetComponent<AudioSource>();

        if (gameSettings == null)
        {
            gameSettings = Resources.Load<GameSettingsSO>("GameSettings");
            if (gameSettings == null)
                Debug.LogWarning("[AudioManager] GameSettings not found");
        }

        if (gameSettings != null) gameSettings.LoadSettings();

        InitializeSFXPool();
    }

    private void Start()
    {
        FindAudioComponents(SceneManager.GetActiveScene());
        PlayBGM("FirstBGM");
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

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
            UpdateBGMVolumeRealtime();
        }
        else
        {
            bgmSource = null;
        }
    }

    #region SFX Pool System (ป้องกันเกมกระตุก)

    private void InitializeSFXPool()
    {
        GameObject poolContainer = new GameObject("SFX_Pool");
        poolContainer.transform.SetParent(this.transform);

        for (int i = 0; i < sfxPoolSize; i++)
        {
            GameObject obj = new GameObject($"SFX_Source_{i}");
            obj.transform.SetParent(poolContainer.transform);
            AudioSource source = obj.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 5f;
            source.maxDistance = 20f;

            sfxPool.Add(source);
        }
    }

    private AudioSource GetAvailableSFXSource()
    {
        foreach (var source in sfxPool)
        {
            if (!source.isPlaying) return source;
        }
        return sfxPool[0];
    }

    #endregion

    #region Play SFX (รวม Player, SFX, Asset)
    public void PlayClipAtPosition(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSFXSource();
        source.transform.position = position;
        source.clip = clip;
        source.volume = GetGlobalSFXVolume();
        source.spatialBlend = 1f;
        source.Play();
    }

    private SoundConfig FindSoundInAllLists(string name)
    {
        SoundConfig s = Array.Find(playerSoundList, sound => sound.soundName == name);
        if (s != null) return s;

        s = Array.Find(sfxSoundList, sound => sound.soundName == name);
        if (s != null) return s;

        s = Array.Find(assetsSoundList, sound => sound.soundName == name);
        return s;
    }

    public void PlaySoundAtPosition(string name, Vector3 position)
    {
        SoundConfig s = FindSoundInAllLists(name);

        if (s == null)
        {
            Debug.LogWarning($"[AudioManager] can't find '{name}' Config");
            return;
        }

        AudioSource source = GetAvailableSFXSource();
        source.transform.position = position;
        source.clip = s.clip;

        float globalSfx = gameSettings != null ? (gameSettings.sfxVolume / 100f) : 1f;
        source.volume = globalSfx;
        source.spatialBlend = 1f;

        source.Play();
    }

    public void PlayDirectSound(string name)
    {
        SoundConfig s = FindSoundInAllLists(name);

        if (s == null)
        {
            Debug.LogWarning($"[AudioManager] can't find '{name}' Config for Direct Sound");
            return;
        }

        if (uiSource != null && s.clip != null)
        {
            float globalSfx = gameSettings != null ? (gameSettings.sfxVolume / 100f) : 1f;
            uiSource.PlayOneShot(s.clip, globalSfx);
        }
    }

    public float GetGlobalSFXVolume()
    {
        return gameSettings != null ? (gameSettings.sfxVolume / 100f) : 1f;
    }

    #endregion

    #region BGM

    public void PlayBGM(string name)
    {
        if (bgmSource == null) return;

        SoundConfig s = Array.Find(soundBGMList, sound => sound.soundName == name);
        if (s == null) return;

        if (bgmSource.clip == s.clip && bgmSource.isPlaying) return;

        bgmSource.clip = s.clip;
        currentBgmBaseVolume = 1f;
        bgmSource.loop = true;

        UpdateBGMVolumeRealtime();
        bgmSource.Play();
        Debug.Log($"[AudioManager] Playing BGM: {name}");
    }

    public void UpdateBGMVolumeRealtime()
    {
        if (bgmSource != null && gameSettings != null)
        {
            bgmSource.volume = currentBgmBaseVolume * (gameSettings.musicVolume / 100f);
        }
    }

    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.Stop();
    }
    #endregion
}

[System.Serializable]
public class SoundConfig
{
    public string soundName;
    public AudioClip clip;
}