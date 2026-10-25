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
        HoldBGMUntilLevelShown();
        PlayBGM("FirstBGM");
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindAudioComponents(scene);
        HoldBGMUntilLevelShown();

        // เริ่ม BGM เมื่อเข้าซีนที่มี BGM_Player (เช่น Scene 1) — เดิม PlayBGM ถูกเรียกแค่ใน Start()
        // ตอนอยู่ใน core scene ที่ยังไม่มี BGM_Player เลยไม่เคยเล่นในเกมจริง (PlayBGM idempotent อยู่แล้ว)
        if (bgmSource != null) PlayBGM("FirstBGM");
    }

    private void FindAudioComponents(Scene scene)
    {
        GameObject bgmObj = GameObject.Find("BGM_Player");
        if (bgmObj != null)
        {
            bgmSource = bgmObj.GetComponent<AudioSource>();
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
    // volumeScale: ความดังเฉพาะของเสียงนั้น (เช่น volume เดิมของ AudioSource บนกับดัก) คูณกับระดับ SFX ที่ผู้เล่นตั้ง
    public void PlayClipAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSFXSource();
        source.transform.position = position;
        source.clip = clip;
        source.volume = GetGlobalSFXVolume() * volumeScale;
        source.spatialBlend = 1f;
        source.Play();
    }

    // เสียงของวัตถุในด่าน (กับดัก, checkpoint, ตาชั่ง): เล่นผ่าน SFX pool เหมือนเสียงตัวละคร — ตามระดับ SFX และเบาลงตามระยะ (SFX-1)
    // volume ของ AudioSource เดิมบนวัตถุใช้เป็นความดังเฉพาะของเสียงนั้น
    public static void PlayObjectSfx(AudioSource localSource, AudioClip clip, Vector3 position)
    {
        if (clip == null) return;

        float baseVolume = localSource != null ? localSource.volume : 1f;
        if (Instance != null) Instance.PlayClipAtPosition(clip, position, baseVolume);
        else if (localSource != null) localSource.PlayOneShot(clip);
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

    // เสียง "ติก" ตอนปุ่ม/แผ่นกดในด่านถูกกด (SW-2): ใช้เสียงชื่อ "SwitchTick" ถ้าใส่ไว้ใน sfxSoundList ไม่งั้นใช้เสียงคลิก UI
    public void PlaySwitchTick(Vector3 position)
    {
        SoundConfig s = Array.Find(sfxSoundList, sound => sound.soundName == "SwitchTick")
                     ?? Array.Find(sfxSoundList, sound => sound.soundName == "Click");
        if (s != null) PlayClipAtPosition(s.clip, position);
    }

    public float GetGlobalSFXVolume()
    {
        return gameSettings != null ? (gameSettings.sfxVolume / 100f) : 1f;
    }

    #endregion

    #region BGM

    // เพลงด่านรอจนหน้าโหลดปิด (AU-2): ช่วงต้นด่านมีวิดีโอที่มีเสียงเล่นอยู่ (คัตซีน S1, วิดีโอหน้าโหลดทุกด่าน)
    // เดิมเพลงด่านเริ่มตั้งแต่โหลดฉากจึงดังซ้อน — ระหว่างรอ PlayBGM แค่จำชื่อเพลง, GameManager ปล่อยตอนปิดหน้าโหลด
    private bool _bgmHeld;
    private string _heldBgmName;

    // ฉากด่าน (มี BGM_Player) ที่หน้าโหลดยังไม่ปิด; ถ้าเกมพร้อมแล้ว (เช่น manager ถูกสร้างช้า) ไม่รอ — ไม่มีใครปล่อยให้
    private void HoldBGMUntilLevelShown()
    {
        if (bgmSource == null) return;

        var gm = GameManager.Instance;
        bool levelShown = gm != null && gm.Object != null && gm.Object.IsValid && gm.IsGameReady;
        if (!levelShown) HoldBGM();
    }

    public void HoldBGM()
    {
        _bgmHeld = true;
        if (bgmSource != null && bgmSource.isPlaying) bgmSource.Stop(); // BGM_Player บางฉากตั้ง Play On Awake ไว้
    }

    public void ReleaseBGM()
    {
        if (!_bgmHeld) return;
        _bgmHeld = false;

        if (!string.IsNullOrEmpty(_heldBgmName)) PlayBGM(_heldBgmName);
        _heldBgmName = null;
    }

    public void PlayBGM(string name)
    {
        if (_bgmHeld)
        {
            _heldBgmName = name;
            return;
        }

        if (bgmSource == null) return;

        SoundConfig s = Array.Find(soundBGMList, sound => sound.soundName == name);
        if (s == null) return;

        if (bgmSource.clip == s.clip && bgmSource.isPlaying) return;

        bgmSource.clip = s.clip;
        currentBgmBaseVolume = 1f;
        bgmSource.loop = true;

        UpdateBGMVolumeRealtime();
        bgmSource.Play();
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