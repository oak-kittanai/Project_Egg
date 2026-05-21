using UnityEngine;

public class AudioSession : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameSettingsSO gameSettings;

    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource uiSource;

    private float currentBgmBaseVolume = 1f;
    public static AudioSession Instance;

    void Awake()
    {
        Instance = this;

        if (gameSettings == null)
            gameSettings = Resources.Load<GameSettingsSO>("GameSettings");

        if (gameSettings != null)
            gameSettings.LoadSettings();

        if (bgmSource != null) currentBgmBaseVolume = bgmSource.volume;

        UpdateBGMVolumeRealtime();
    }

    public void PlayClickSound(AudioClip clip)
    {
        if (uiSource == null || clip == null || gameSettings == null) return;

        float volumeMultiplier = gameSettings.sfxVolume / 100f;
        uiSource.PlayOneShot(clip, volumeMultiplier);
    }

    public void UpdateBGMVolumeRealtime()
    {
        if (bgmSource != null && gameSettings != null)
        {
            bgmSource.volume = currentBgmBaseVolume * (gameSettings.musicVolume / 100f);
        }
    }
}