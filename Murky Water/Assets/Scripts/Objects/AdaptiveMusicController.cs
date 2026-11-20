using UnityEngine;
using UnityEngine.Audio;

/// Level 2. Layered adaptive music:
///  - One looping base track per pollution zone, crossfaded when the zone changes.
///  - One "tension" layer whose volume rises as the predator gets closer.
/// All clips must share the same tempo and length so the layers stay in sync.
/// Assign the Music mixer group (Jose's AudioMixer) so the player's music volume applies.
/// It creates its own AudioSources, so it does not need AudioManager. If Jose's AudioManager
/// also plays zone music, disable one of them to avoid duplicated music.
public class AdaptiveMusicController : MonoBehaviour
{
    [System.Serializable]
    public class ZoneTrack
    {
        [Min(1)] public int phaseNumber = 1;
        public AudioClip clip;
    }

    [SerializeField] private GameBalanceConfig balanceConfig;
    [SerializeField] private ZoneTrack[] zoneTracks = new ZoneTrack[4];
    [SerializeField] private AudioClip tensionClip;
    [SerializeField] private AudioMixerGroup musicGroup;

    [Header("Mix")]
    [SerializeField, Min(0.1f)] private float zoneBlendSeconds = 5f;
    [SerializeField, Range(0f, 1f)] private float maxZoneVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float maxTensionVolume = 0.8f;
    [Tooltip("How fast the tension layer fades in/out (volume units per second).")]
    [SerializeField, Min(0.1f)] private float tensionFadeSpeed = 1.5f;
    [SerializeField, Min(0.1f)] private float masterFadeSeconds = 1.5f;

    private AudioSource[] zoneSources;
    private AudioSource tensionSource;
    private float[] zoneWeights;
    private int targetZone = -1;
    private float tension;
    private float tensionTarget;
    private float master;
    private float masterTarget;
    private bool started;

    private void Awake()
    {
        int count = zoneTracks != null ? zoneTracks.Length : 0;
        zoneSources = new AudioSource[count];
        zoneWeights = new float[count];

        for (int i = 0; i < count; i++)
            zoneSources[i] = CreateSource(zoneTracks[i] != null ? zoneTracks[i].clip : null);

        tensionSource = CreateSource(tensionClip);

        if (balanceConfig == null)
            Debug.LogWarning("AdaptiveMusicController: assign GameBalanceConfig.", this);
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
    }

    private void Update()
    {
        if (!started) return;

        // Unscaled time so fades still finish when the game ends or is paused.
        float dt = Time.unscaledDeltaTime;
        master = Mathf.MoveTowards(master, masterTarget, dt / masterFadeSeconds);
        tension = Mathf.MoveTowards(tension, tensionTarget, tensionFadeSpeed * dt);

        for (int i = 0; i < zoneSources.Length; i++)
        {
            float goal = i == targetZone ? 1f : 0f;
            zoneWeights[i] = Mathf.MoveTowards(zoneWeights[i], goal, dt / zoneBlendSeconds);
            if (zoneSources[i] != null)
                zoneSources[i].volume = zoneWeights[i] * maxZoneVolume * master;
        }

        if (tensionSource != null)
            tensionSource.volume = tension * maxTensionVolume * master;
    }

    private AudioSource CreateSource(AudioClip clip)
    {
        if (clip == null) return null;
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.outputAudioMixerGroup = musicGroup;
        return source;
    }

    private void StartAllLayers()
    {
        if (started) return;
        started = true;

        // Start every layer in the same frame so they stay aligned.
        double startTime = AudioSettings.dspTime + 0.1;
        foreach (AudioSource source in zoneSources)
            if (source != null) source.PlayScheduled(startTime);
        if (tensionSource != null) tensionSource.PlayScheduled(startTime);
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

        bool first = targetZone < 0;
        targetZone = index;

        if (first)
        {
            // First zone starts at full weight instead of fading from silence.
            for (int i = 0; i < zoneWeights.Length; i++) zoneWeights[i] = i == index ? 1f : 0f;
        }
    }

    private void HandleGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Countdown:
            case GameState.Playing:
            case GameState.Paused:
                StartAllLayers();
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
