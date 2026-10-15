
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Mixer")]
    public AudioMixer mixer;
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup sfxGroup;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    [Header("Background Music")]
    public AudioClip backgroundMusic;

    private void Start()
    {
        PlayMusic(backgroundMusic);
    }
    private void Awake()
    {
        // Prevents duplicate AudioManager instances.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Routes each AudioSource to its corresponding Audio Mixer Group.
        musicSource.outputAudioMixerGroup = musicGroup;
        sfxSource.outputAudioMixerGroup = sfxGroup;
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        sfxSource.PlayOneShot(clip);
    }

    public void SetMusicVolume(float value)
    {
        // Converts a linear volume value into decibels.
        mixer.SetFloat(
            "MusicVolume",
            Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f
        );
    }

    public void SetSFXVolume(float value)
    {
        mixer.SetFloat(
            "SFXVolume",
            Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f
        );
    }
}
