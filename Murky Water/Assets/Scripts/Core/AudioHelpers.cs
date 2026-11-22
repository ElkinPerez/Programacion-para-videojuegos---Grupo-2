using System;
using UnityEngine;

/// One sound effect: several clip variations, volume and random pitch range.
/// Leave "clips" empty to silence that effect (nothing plays, no errors).
/// It plays through an AudioSource that YOU assign in the owner script (it never creates one),
/// so the AudioSource's own Volume and Output (mixer group) keep applying.
[Serializable]
public class SfxEntry
{
    public AudioClip[] clips;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 2f)] public float minPitch = 0.95f;
    [Range(0.5f, 2f)] public float maxPitch = 1.05f;

    [NonSerialized] private int lastIndex = -1;

    public bool HasClips => clips != null && clips.Length > 0;

    /// Picks a random clip, avoiding repeating the previous one when there are several.
    public AudioClip PickClip()
    {
        if (!HasClips) return null;
        if (clips.Length == 1) return clips[0];

        int index;
        do { index = UnityEngine.Random.Range(0, clips.Length); }
        while (index == lastIndex);

        lastIndex = index;
        return clips[index];
    }

    public float RandomPitch() => UnityEngine.Random.Range(Mathf.Min(minPitch, maxPitch), Mathf.Max(minPitch, maxPitch));

    /// Plays this effect once on the given AudioSource. The final volume is the AudioSource's Volume
    /// multiplied by this entry's volume and volumeScale.
    /// applyPitch = true also sets the random pitch ON THE SOURCE (use it only for a source that is
    /// dedicated to one character; a shared source keeps its own pitch).
    public void PlayOn(AudioSource source, bool applyPitch, float volumeScale = 1f)
    {
        if (source == null) return;

        AudioClip clip = PickClip();
        if (clip == null) return;

        if (applyPitch) source.pitch = RandomPitch();
        source.PlayOneShot(clip, Mathf.Clamp01(volume * volumeScale));
    }
}

/// Looping sound with fade in/out. The intensity (0-1) controls volume and pitch.
/// It uses the AudioSource you assign in "source" (one AudioSource per loop, never shared with
/// another loop or with the one-shot source). The source's Volume works as the master level of this loop.
/// The owner script must call Init() once and Tick() every frame.
[Serializable]
public class LoopChannel
{
    public AudioClip clip;
    [Tooltip("Dedicated AudioSource for this loop. Its Volume is the master level of the loop (1 = use 'volume' below as is).")]
    [SerializeField] private AudioSource source;
    [Range(0f, 1f)] public float volume = 0.6f;
    [Min(0f)] public float fadeInSeconds = 0.3f;
    [Min(0f)] public float fadeOutSeconds = 0.5f;
    [Tooltip("Pitch when intensity is 0.")]
    [Range(0.5f, 2f)] public float pitchAtMin = 1f;
    [Tooltip("Pitch when intensity is 1.")]
    [Range(0.5f, 2f)] public float pitchAtMax = 1f;

    private float baseVolume = 1f;
    private float target;
    private float current;
    private bool paused;
    private bool ready;

    public AudioSource Source => source;

    public void Init(UnityEngine.Object context)
    {
        if (clip == null) return;

        if (source == null)
        {
            Debug.LogWarning($"LoopChannel: assign an AudioSource for the loop '{clip.name}'.", context);
            return;
        }

        // Remember the volume you set on the AudioSource and use it as the loop's master level.
        baseVolume = source.volume;
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
        ready = true;
    }

    /// 0 = fade out and stop, 1 = full volume.
    public void SetIntensity(float intensity) => target = Mathf.Clamp01(intensity);

    public void Tick(float unscaledDeltaTime)
    {
        if (!ready || paused) return;

        float seconds = target > current ? fadeInSeconds : fadeOutSeconds;
        float step = seconds > 0f ? unscaledDeltaTime / seconds : 1f;
        current = Mathf.MoveTowards(current, target, step);

        source.volume = current * volume * baseVolume;
        source.pitch = Mathf.Lerp(pitchAtMin, pitchAtMax, current);

        if (current > 0f && !source.isPlaying) source.Play();
        else if (current <= 0f && source.isPlaying) source.Stop();
    }

    public void Pause()
    {
        paused = true;
        if (ready) source.Pause();
    }

    public void Resume()
    {
        paused = false;
        if (ready) source.UnPause();
    }

    public void StopImmediate()
    {
        target = 0f;
        current = 0f;
        paused = false;
        if (!ready) return;
        source.volume = 0f;
        source.Stop();
    }
}
