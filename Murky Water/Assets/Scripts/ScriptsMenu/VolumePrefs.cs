using UnityEngine;
using UnityEngine.Audio;

/// Saves the player's volume settings (PlayerPrefs) and applies them to the AudioMixer.
/// Values are linear (0-1) and converted to decibels for the mixer.
/// The parameter names must be EXPOSED in the AudioMixer (right click a group's Volume > Expose).
public static class VolumePrefs
{
    public const string MusicParam = "MusicVolume";
    public const string SfxParam = "SFXVolume";
    public const string AmbientParam = "AmbientVolume";

    private const string KeyPrefix = "volume_";

    public static float Get(string parameterName, float defaultValue = 1f)
    {
        return PlayerPrefs.GetFloat(KeyPrefix + parameterName, defaultValue);
    }

    public static void Set(string parameterName, float value)
    {
        PlayerPrefs.SetFloat(KeyPrefix + parameterName, Mathf.Clamp01(value));
    }

    /// Converts a linear value to decibels and sets it on the mixer.
    public static void Apply(AudioMixer mixer, string parameterName, float value)
    {
        if (mixer == null) return;
        mixer.SetFloat(parameterName, Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
    }

    /// Applies every saved volume. Call it when a scene starts (in Start, not Awake).
    public static void ApplyAll(AudioMixer mixer)
    {
        if (mixer == null) return;
        Apply(mixer, MusicParam, Get(MusicParam));
        Apply(mixer, SfxParam, Get(SfxParam));
        Apply(mixer, AmbientParam, Get(AmbientParam));
    }
}
