
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Mixer")]
    public AudioMixer mixer;
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup sfxGroup;
    public AudioMixerGroup ambientGroup;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource ambientSource;

    [Header("Background Music")]
    public AudioClip backgroundMusic;

    [Header("Ambient Sound")]
    public AudioClip ambientSound;

    private void Awake()
    {
        // Prevent duplicate AudioManager instances.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Routes each AudioSource to its corresponding Audio Mixer Group.
        musicSource.outputAudioMixerGroup = musicGroup;
        sfxSource.outputAudioMixerGroup = sfxGroup;
        ambientSource.outputAudioMixerGroup = ambientGroup;
    }

    private void Start()
    {
        PlayMusic(backgroundMusic);
        PlayAmbient(ambientSound);
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

    public void PlayAmbient(AudioClip clip)
    {
        if (clip == null) return;

        ambientSource.clip = clip;
        ambientSource.loop = true;
        ambientSource.Play();
    }

    public void StopAmbient()
    {
        ambientSource.Stop();
    }

    // Converts a linear volume value into decibels.
    public void SetMusicVolume(float value)
    {
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

    public void SetAmbientVolume(float value)
    {
        mixer.SetFloat(
            "AmbientVolume",
            Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f
        );
    }
}