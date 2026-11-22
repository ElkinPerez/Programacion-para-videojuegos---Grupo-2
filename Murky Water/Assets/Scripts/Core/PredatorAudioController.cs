using UnityEngine;

/// Sounds of the predator: chase ambience, danger alert, bites and retreat.
/// Only listens to GameEvents; it never moves or changes the predator.
/// Place it on the predator, or on an empty object that is always active
/// (if the predator gets disabled, this script would stop listening and its loops would cut off).
///
/// Audio sources (all assigned by hand, nothing is created automatically):
///  - Predator Source: the predator's own AudioSource. Bites, retreat and the danger alert play here,
///    so its Volume and mixer group apply.
///  - Shared Sfx Source: AudioManager's SFX AudioSource. Optional: only used by effects that are not
///    the predator's own (currently none, it is ready in case you route one to it).
///  - Each loop (chase, danger) needs its OWN AudioSource in its "Source" field.
public class PredatorAudioController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameBalanceConfig balanceConfig;
    [Tooltip("The predator's own AudioSource (one-shot sounds). Set its Volume and Output (SFX mixer group) in the Inspector.")]
    [SerializeField] private AudioSource predatorSource;
    [Tooltip("AudioManager's SFX AudioSource. Optional, for sounds that are not the predator's own.")]
    [SerializeField] private AudioSource sharedSfxSource;

    [Header("One-shot effects")]
    [Tooltip("First bite (OnPredatorBite with number 1).")]
    [SerializeField] private SfxEntry bite1;
    [Tooltip("Second bite or later (the one that kills).")]
    [SerializeField] private SfxEntry bite2;
    [SerializeField] private SfxEntry retreat;
    [Tooltip("Roar/alert played once when the predator enters the danger distance.")]
    [SerializeField] private SfxEntry dangerEntered;

    [Header("Loops (each one needs its own AudioSource)")]
    [Tooltip("Chase ambience while playing: louder and lower-pitched as the predator gets closer.")]
    [SerializeField] private LoopChannel chaseLoop = new LoopChannel { volume = 0.3f, pitchAtMin = 0.9f, pitchAtMax = 0.75f };
    [Tooltip("Heartbeat / tension drone while inside the danger distance.")]
    [SerializeField] private LoopChannel dangerLoop = new LoopChannel { volume = 0.6f, fadeInSeconds = 0.2f, pitchAtMin = 0.9f, pitchAtMax = 1.3f };
    [SerializeField, Range(0f, 1f)] private float minChaseIntensity = 0.25f;

    private bool isPlaying;
    private bool paused;
    private bool danger;
    private float distance = float.MaxValue;

    private void Awake()
    {
        if (predatorSource == null)
            Debug.LogError("PredatorAudioController: assign the predator AudioSource.", this);
        if (balanceConfig == null)
            Debug.LogWarning("PredatorAudioController: assign GameBalanceConfig.", this);

        chaseLoop.Init(this);
        dangerLoop.Init(this);

        bool loopUsesPredatorSource = predatorSource != null &&
            (chaseLoop.Source == predatorSource || dangerLoop.Source == predatorSource);
        bool loopsShareSource = chaseLoop.Source != null && chaseLoop.Source == dangerLoop.Source;
        if (loopUsesPredatorSource || loopsShareSource)
            Debug.LogWarning("PredatorAudioController: every loop needs its own AudioSource, different from the predator's one-shot source.", this);
    }

    private void OnEnable()
    {
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        GameEvents.OnPredatorBite += HandlePredatorBite;
        GameEvents.OnPredatorRetreat += HandlePredatorRetreat;
        GameEvents.OnPredatorDistanceChanged += HandleDistanceChanged;
        GameEvents.OnPredatorDangerChanged += HandleDangerChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        GameEvents.OnPredatorBite -= HandlePredatorBite;
        GameEvents.OnPredatorRetreat -= HandlePredatorRetreat;
        GameEvents.OnPredatorDistanceChanged -= HandleDistanceChanged;
        GameEvents.OnPredatorDangerChanged -= HandleDangerChanged;
    }

    private void Update()
    {
        float initialDistance = Mathf.Max(0.01f, balanceConfig != null ? balanceConfig.predatorInitialDistance : 12f);
        float dangerDistance = Mathf.Max(0.01f, balanceConfig != null ? balanceConfig.predatorDangerDistance : 5f);

        float chaseProximity = 1f - Mathf.Clamp01(distance / initialDistance);
        float dangerProximity = 1f - Mathf.Clamp01(distance / dangerDistance);

        chaseLoop.SetIntensity(isPlaying ? Mathf.Lerp(minChaseIntensity, 1f, chaseProximity) : 0f);
        dangerLoop.SetIntensity(isPlaying && danger ? Mathf.Max(0.3f, dangerProximity) : 0f);

        float dt = Time.unscaledDeltaTime;
        chaseLoop.Tick(dt);
        dangerLoop.Tick(dt);
    }

    // Plays on the predator's own AudioSource (random pitch is applied to it).
    private void PlayOwn(SfxEntry entry, float volumeScale = 1f)
    {
        if (entry == null || paused) return;
        entry.PlayOn(predatorSource, true, volumeScale);
    }

    // Plays on the shared SFX AudioSource (its pitch is left alone). Falls back to the predator source if not assigned.
    // Not used by default: swap PlayOwn for PlayShared on any effect you want to route there.
    private void PlayShared(SfxEntry entry, float volumeScale = 1f)
    {
        if (entry == null || paused) return;

        if (sharedSfxSource != null) entry.PlayOn(sharedSfxSource, false, volumeScale);
        else entry.PlayOn(predatorSource, true, volumeScale);
    }

    private void HandleGameStateChanged(GameState state)
    {
        isPlaying = state == GameState.Playing;

        switch (state)
        {
            case GameState.Paused:
                paused = true;
                if (predatorSource != null) predatorSource.Pause(); // only the predator's source; the shared one is not touched
                chaseLoop.Pause();
                dangerLoop.Pause();
                break;
            case GameState.Playing:
                paused = false;
                if (predatorSource != null) predatorSource.UnPause();
                chaseLoop.Resume();
                dangerLoop.Resume();
                break;
            case GameState.Countdown:
            case GameState.Menu:
                paused = false;
                if (predatorSource != null) predatorSource.Stop();
                chaseLoop.StopImmediate();
                dangerLoop.StopImmediate();
                danger = false;
                distance = float.MaxValue;
                break;
            // Won / Lost: loops fade out on their own (isPlaying = false).
        }
    }

    private void HandlePredatorBite(int biteNumber) => PlayOwn(biteNumber <= 1 ? bite1 : bite2);

    private void HandlePredatorRetreat() => PlayOwn(retreat);

    private void HandleDistanceChanged(float newDistance) => distance = newDistance;

    private void HandleDangerChanged(bool inDanger)
    {
        if (inDanger && !danger && isPlaying) PlayOwn(dangerEntered);
        danger = inDanger;
    }
}
