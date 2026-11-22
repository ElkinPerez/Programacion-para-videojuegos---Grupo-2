using UnityEngine;

/// Level 2. Adaptive music on ONE shared AudioSource (the project's music source):
///  - One looping base track per pollution zone. When the zone changes, the current track fades out,
///    the next one is loaded and fades in (the whole change lasts zoneBlendSeconds).
///  - The predator's proximity adds tension by raising the pitch slightly (a single AudioSource
///    cannot play a second "tension" layer at the same time).
/// The zone logic is unchanged: it listens to OnPhaseChanged and matches each track by PhaseConfig.phaseNumber.
/// Clips should share the same tempo and length: the playback position is kept when switching, so the beat stays in sync.
/// Drag the music AudioSource here (the one AudioManager uses). Its Audio Mixer Group is NOT changed,
/// so the player's music volume keeps applying. Leave AudioManager's "Background Music" empty so it
/// does not start its own track on that source.
public class AdaptiveMusicController : MonoBehaviour
{
    [System.Serializable]
    public class ZoneTrack
    {
        [Min(1)] public int phaseNumber = 1;
        public AudioClip clip;
    }

    [SerializeField] private GameBalanceConfig balanceConfig;
    [Tooltip("The music AudioSource. Empty = AudioManager's music source is used when it exists.")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private ZoneTrack[] zoneTracks = new ZoneTrack[4];

    [Header("Mix")]
    [Tooltip("Total duration of a zone change: half fading out, half fading in.")]
    [SerializeField, Min(0.1f)] private float zoneBlendSeconds = 5f;
    [SerializeField, Range(0f, 1f)] private float maxZoneVolume = 1f;
    [Tooltip("Extra pitch when the predator is about to bite (0.06 = +6%). 0 = no tension effect.")]
    [SerializeField, Range(0f, 0.3f)] private float maxTensionPitchBoost = 0.06f;
    [Tooltip("How fast the tension effect fades in/out (units per second).")]
    [SerializeField, Min(0.1f)] private float tensionFadeSpeed = 1.5f;
    [SerializeField, Min(0.1f)] private float masterFadeSeconds = 1.5f;
    [Tooltip("Start the new track at the same position as the old one. Can cause a hitch with compressed clips.")]
    [SerializeField] private bool keepPlaybackPosition = false;

    private int targetZone = -1;
    private int playingZone = -1;
    private float zoneFade;
    private float tension;
    private float tensionTarget;
    private float master;
    private float masterTarget;
    private bool started;

    private void Awake()
    {
        if (balanceConfig == null)
            Debug.LogWarning("AdaptiveMusicController: assign GameBalanceConfig.", this);

        PreloadClips();
    }

    // Loads every zone clip into memory now, so switching tracks later never has to load audio mid-game.
    private void PreloadClips()
    {
        if (zoneTracks == null) return;

        foreach (ZoneTrack track in zoneTracks)
        {
            if (track == null || track.clip == null) continue;
            if (track.clip.loadState == AudioDataLoadState.Unloaded)
                track.clip.LoadAudioData();
        }
    }

    private void Start()
    {
        if (musicSource == null && AudioManager.Instance != null)
            musicSource = AudioManager.Instance.musicSource;

        if (musicSource == null)
            Debug.LogError("AdaptiveMusicController: assign the music AudioSource.", this);
    }

    private void OnEnable()
    {
        GameEvents.OnPhaseChanged += HandlePhaseChanged;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        GameEvents.OnPredatorDistanceChanged += HandlePredatorDistance;
    }

    private void OnDisable()
    {
        GameEvents.OnPhaseChanged -= HandlePhaseChanged;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        GameEvents.OnPredatorDistanceChanged -= HandlePredatorDistance;

        if (musicSource != null) musicSource.pitch = 1f;
    }

    private void Update()
    {
        if (!started || musicSource == null) return;

        // Unscaled time so fades still finish when the game ends or is paused.
        float dt = Time.unscaledDeltaTime;
        master = Mathf.MoveTowards(master, masterTarget, dt / masterFadeSeconds);
        tension = Mathf.MoveTowards(tension, tensionTarget, tensionFadeSpeed * dt);

        UpdateZoneFade(dt);

        musicSource.volume = zoneFade * maxZoneVolume * master;
        musicSource.pitch = 1f + tension * maxTensionPitchBoost;
    }

    // Fades the current track out, swaps the clip, then fades the new one in.
    private void UpdateZoneFade(float dt)
    {
        if (targetZone < 0) return;

        float half = zoneBlendSeconds * 0.5f;

        if (playingZone != targetZone)
        {
            if (playingZone >= 0 && zoneFade > 0f)
            {
                zoneFade = Mathf.MoveTowards(zoneFade, 0f, dt / half);
                if (zoneFade > 0f) return; // still fading out
            }
            SwapTo(targetZone);
        }

        zoneFade = Mathf.MoveTowards(zoneFade, 1f, dt / half);
    }

    private void SwapTo(int zoneIndex)
    {
        bool first = playingZone < 0;
        AudioClip clip = zoneTracks[zoneIndex] != null ? zoneTracks[zoneIndex].clip : null;
        AudioClip previousClip = musicSource.clip;
        // Reading time with no clip assigned logs a warning, so only read it when it is needed and valid.
        float previousTime = keepPlaybackPosition && previousClip != null ? musicSource.time : 0f;

        playingZone = zoneIndex;

        if (clip == null)
        {
            musicSource.Stop();
            musicSource.clip = null;
        }
        else
        {
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.Play();

            // Optional: keep the position so the beat does not jump. Off by default because
            // seeking inside a compressed clip can cause a frame hitch.
            bool sameLength = previousClip != null && Mathf.Abs(previousClip.length - clip.length) < 0.05f;
            if (keepPlaybackPosition && !first && sameLength)
                musicSource.time = Mathf.Min(previousTime, clip.length - 0.01f);
        }

        // First zone starts at full weight instead of fading from silence.
        if (first) zoneFade = 1f;
    }

    private void HandlePhaseChanged(PhaseConfig phase)
    {
        if (phase == null || zoneTracks == null) return;

        int index = -1;
        for (int i = 0; i < zoneTracks.Length; i++)
        {
            if (zoneTracks[i] != null && zoneTracks[i].phaseNumber == phase.phaseNumber)
            {
                index = i;
                break;
            }
        }
        if (index < 0) return;

        targetZone = index;
    }

    private void HandleGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Countdown:
            case GameState.Playing:
            case GameState.Paused:
                started = true;
                masterTarget = 1f;
                break;
            default: // Menu, Won, Lost
                masterTarget = 0f;
                tensionTarget = 0f;
                break;
        }
    }

    private void HandlePredatorDistance(float distance)
    {
        float far = balanceConfig != null ? balanceConfig.predatorInitialDistance : 12f;
        float near = balanceConfig != null ? balanceConfig.predatorBiteDistance : 1f;
        if (far <= near) far = near + 1f;

        // 0 when the predator is at its starting distance or farther, 1 when it is about to bite.
        tensionTarget = 1f - Mathf.InverseLerp(near, far, distance);
    }
}