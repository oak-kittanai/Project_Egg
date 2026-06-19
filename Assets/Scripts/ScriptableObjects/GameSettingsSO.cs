using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Settings/Game Settings")]
public class GameSettingsSO : ScriptableObject
{
    [Range(0f, 100f)] public float musicVolume = 100f;
    [Range(0f, 100f)] public float sfxVolume = 100f;

    public void LoadSettings()
    {
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 100f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 100f);
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
    }
}