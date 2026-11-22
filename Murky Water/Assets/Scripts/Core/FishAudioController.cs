using UnityEngine;

/// Sounds of the fish: swimming, eating, hits, low energy, death and victory.
/// Only listens to GameEvents; it never modifies gameplay.
/// Place it on the player (or an empty object that is always active).
///
/// Audio sources (all assigned by hand, nothing is created automatically):
///  - Fish Source: the fish's own AudioSource. Fish sounds (eating, hurt, low energy warning,
///    invulnerability end, deaths) play here, so its Volume and mixer group apply.
///  - Shared Sfx Source: the SFX AudioSource of AudioManager. Sounds that are not the fish's own
///    (obstacle impacts, vision blur, victory) play here.
///  - Each loop (swim, low energy) needs its OWN AudioSource in its "Source" field.
public class FishAudioController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameBalanceConfig balanceConfig;
    [Tooltip("The fish's own AudioSource (one-shot sounds). Set its Volume and Output (SFX mixer group) in the Inspector.")]
    [SerializeField] private AudioSource fishSource;
    [Tooltip("AudioManager's SFX AudioSource, for sounds that are not the fish's own.")]
    [SerializeField] private AudioSource sharedSfxSource;

    [Header("One-shot effects")]
    [SerializeField] private SfxEntry eatFood;
    [Tooltip("Obstacle that removes life (danger obstacle). Plays on the shared SFX source.")]
    [SerializeField] private SfxEntry hitDanger;
    [Tooltip("Obstacle that removes energy and blurs vision (puddle / energy obstacle). Plays on the shared SFX source.")]
    [SerializeField] private SfxEntry hitEnergy;
    [Tooltip("Lethal obstacle. Plays on the shared SFX source.")]
    [SerializeField] private SfxEntry hitLethal;
    [Tooltip("Fish reaction to a predator bite (the bite itself sounds in PredatorAudioController). Optional.")]
    [SerializeField] private SfxEntry hurtByPredator;
    [Tooltip("Plays on the shared SFX source.")]
    [SerializeField] private SfxEntry visionObstructed;
    [SerializeField] private SfxEntry invulnerabilityEnded;
    [SerializeField] private SfxEntry lowEnergyWarning;

    [Header("Run end (disable if AudioManager already plays these)")]
    [SerializeField] private bool handleRunEndSounds = true;
    [SerializeField] private SfxEntry deathHealth;
    [SerializeField] private SfxEntry deathEnergy;
    [SerializeField] private SfxEntry deathLethal;
    [SerializeField] private SfxEntry deathPredator;
    [Tooltip("Plays on the shared SFX source.")]
    [SerializeField] private SfxEntry victory;

    [Header("Loops (each one needs its own AudioSource)")]
    [Tooltip("Swimming sound while playing. Pitch/volume follow energy.")]
    [SerializeField] private LoopChannel swimLoop = new LoopChannel { volume = 0.35f, pitchAtMin = 0.85f, pitchAtMax = 1f };
    [Tooltip("Tired breathing / heartbeat while energy is below the low-energy threshold.")]
    [SerializeField] private LoopChannel lowEnergyLoop = new LoopChannel { volume = 0.6f, pitchAtMin = 0.9f, pitchAtMax = 1.2f };
    [SerializeField, Range(0f, 1f)] private float minSwimIntensity = 0.4f;

    private bool isPlaying;
    private bool paused;
    private bool lowEnergy;
    private float energy = 1f;

    private void Awake()
    {
        if (fishSource == null)
            Debug.LogError("FishAudioController: assign the fish AudioSource.", this);
        if (sharedSfxSource == null)
            Debug.LogWarning("FishAudioController: assign the shared SFX AudioSource. Until then those sounds play on the fish's own source.", this);
        if (balanceConfig == null)
            Debug.LogWarning("FishAudioController: assign GameBalanceConfig.", this);

        swimLoop.Init(this);
        lowEnergyLoop.Init(this);

        bool loopUsesFishSource = fishSource != null &&
            (swimLoop.Source == fishSource || lowEnergyLoop.Source == fishSource);
        bool loopsShareSource = swimLoop.Source != null && swimLoop.Source == lowEnergyLoop.Source;
        if (loopUsesFishSource || loopsShareSource)
            Debug.LogWarning("FishAudioController: every loop needs its own AudioSource, different from the fish's one-shot source.", this);
    }

    private void OnEnable()
    {
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        GameEvents.OnPlayerHit += HandlePlayerHit;
        GameEvents.OnFoodConsumed += HandleFoodConsumed;
        GameEvents.OnEnergyChanged += HandleEnergyChanged;
        GameEvents.OnInvulnerabilityChanged += HandleInvulnerabilityChanged;
        GameEvents.OnVisionObstructed += HandleVisionObstructed;
        GameEvents.OnPlayerDied += HandlePlayerDied;
        GameEvents.OnRunCompleted += HandleRunCompleted;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        GameEvents.OnPlayerHit -= HandlePlayerHit;
        GameEvents.OnFoodConsumed -= HandleFoodConsumed;
        GameEvents.OnEnergyChanged -= HandleEnergyChanged;
        GameEvents.OnInvulnerabilityChanged -= HandleInvulnerabilityChanged;
        GameEvents.OnVisionObstructed -= HandleVisionObstructed;
        GameEvents.OnPlayerDied -= HandlePlayerDied;
        GameEvents.OnRunCompleted -= HandleRunCompleted;
    }

    private void Update()
    {
        bool lowActive = isPlaying && lowEnergy && energy > 0f;
        swimLoop.SetIntensity(isPlaying ? Mathf.Lerp(minSwimIntensity, 1f, energy) : 0f);
        lowEnergyLoop.SetIntensity(lowActive ? Mathf.Lerp(0.35f, 1f, 1f - energy / LowThreshold) : 0f);

        float dt = Time.unscaledDeltaTime;
        swimLoop.Tick(dt);
        lowEnergyLoop.Tick(dt);
    }

    private float LowThreshold
    {
        get
        {
            float percent = balanceConfig != null ? balanceConfig.lowEnergyThreshold : 25f;
            return Mathf.Max(0.01f, percent / 100f);
        }
    }

    // Plays on the fish's own AudioSource (random pitch is applied to it).
    private void PlayOwn(SfxEntry entry, float volumeScale = 1f)
    {
        if (entry == null || paused) return;
        entry.PlayOn(fishSource, true, volumeScale);
    }

    // Plays on the shared SFX AudioSource (its pitch is left alone). Falls back to the fish source if not assigned.
    private void PlayShared(SfxEntry entry, float volumeScale = 1f)
    {
        if (entry == null || paused) return;

        if (sharedSfxSource != null) entry.PlayOn(sharedSfxSource, false, volumeScale);
        else entry.PlayOn(fishSource, true, volumeScale);
    }

    private void HandleGameStateChanged(GameState state)
    {
        isPlaying = state == GameState.Playing;

        switch (state)
        {
            case GameState.Paused:
                paused = true;
                if (fishSource != null) fishSource.Pause(); // only the fish's source; the shared one is not touched
                swimLoop.Pause();
                lowEnergyLoop.Pause();
                break;
            case GameState.Playing:
                paused = false;
                if (fishSource != null) fishSource.UnPause();
                swimLoop.Resume();
                lowEnergyLoop.Resume();
                break;
            case GameState.Countdown:
            case GameState.Menu:
                paused = false;
                if (fishSource != null) fishSource.Stop();
                swimLoop.StopImmediate();
                lowEnergyLoop.StopImmediate();
                lowEnergy = false;
                break;
            // Won / Lost: loops fade out on their own (isPlaying = false); one-shots finish normally.
        }
    }

    private void HandlePlayerHit(HitData hit)
    {
        if (hit.source == HitSource.Predator)
        {
            PlayOwn(hurtByPredator);
            return;
        }

        if (hit.isLethal) PlayShared(hitLethal);
        else if (hit.lifeDamagePercent > 0f) PlayShared(hitDanger);
        else if (hit.energyDamagePercent > 0f) PlayShared(hitEnergy);
    }

    private void HandleFoodConsumed(float recoveredPercent) => PlayOwn(eatFood);

    private void HandleVisionObstructed(float duration, float intensity) => PlayShared(visionObstructed, Mathf.Lerp(0.6f, 1f, intensity));

    private void HandleInvulnerabilityChanged(bool active)
    {
        if (!active && isPlaying) PlayOwn(invulnerabilityEnded);
    }

    private void HandleEnergyChanged(float normalized)
    {
        energy = normalized;
        bool nowLow = normalized < LowThreshold;
        if (nowLow && !lowEnergy && isPlaying && normalized > 0f) PlayOwn(lowEnergyWarning);
        lowEnergy = nowLow;
    }

    private void HandlePlayerDied(DeathCause cause, float progress)
    {
        if (!handleRunEndSounds) return;
        switch (cause)
        {
            case DeathCause.Health: PlayOwn(deathHealth); break;
            case DeathCause.Energy: PlayOwn(deathEnergy); break;
            case DeathCause.Lethal: PlayOwn(deathLethal); break;
            case DeathCause.Predator: PlayOwn(deathPredator); break;
        }
    }

    private void HandleRunCompleted()
    {
        if (handleRunEndSounds) PlayShared(victory);
    }
}
