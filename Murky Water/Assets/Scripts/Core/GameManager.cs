using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Coordinates game states, countdown, defeat, victory, pause, and restart.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player References")]
    [SerializeField] private HealthSystem healthSystem;
    [SerializeField] private EnergySystem energySystem;
    [SerializeField] private PlayerController playerController;
    [Header("Flow")]
    [SerializeField, Min(0f)] private float countdownSeconds = 3f;
    [SerializeField] private string menuSceneName = "";

    private Coroutine countdownRoutine;
    private GameState currentState = GameState.Menu;
    private float progress;
    private bool endingRun;

    public GameState CurrentState => currentState;
    public float CurrentProgress => progress;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("There is more than one active GameManager in the scene.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        GameEvents.OnHealthChanged += HandleHealthChanged;
        GameEvents.OnEnergyChanged += HandleEnergyChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnHealthChanged -= HandleHealthChanged;
        GameEvents.OnEnergyChanged -= HandleEnergyChanged;
    }

    private void Start()
    {
        if (healthSystem == null && playerController != null) healthSystem = playerController.GetComponent<HealthSystem>();
        if (energySystem == null && playerController != null) energySystem = playerController.GetComponent<EnergySystem>();
        //SetState(GameState.Menu);
        StartRun();
    }

    public void StartRun()
    {
        if (currentState == GameState.Playing || currentState == GameState.Countdown) return;
        Time.timeScale = 1f;
        endingRun = false;
        progress = 0f;
        if (healthSystem != null) healthSystem.ResetHealth();
        if (energySystem != null) energySystem.ResetEnergy();
        SetState(GameState.Countdown);
        if (countdownRoutine != null) StopCoroutine(countdownRoutine);
        countdownRoutine = StartCoroutine(CountdownRoutine());
    }

    public void PauseRun()
    {
        if (currentState != GameState.Playing) return;
        Time.timeScale = 0f;
        SetState(GameState.Paused);
    }

    public void ResumeRun()
    {
        if (currentState != GameState.Paused) return;
        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    public void RestartRun()
    {
        Time.timeScale = 1f;
        if (countdownRoutine != null) StopCoroutine(countdownRoutine);
        countdownRoutine = null;
        Scene active = SceneManager.GetActiveScene();
        if (active.buildIndex >= 0)
            SceneManager.LoadScene(active.buildIndex);
        else
            Debug.LogError("RestartRun: add the current scene to Build Profiles/Scenes In Build.", this);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrWhiteSpace(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
            return;
        }
        SetState(GameState.Menu);
    }

    public void RegisterDeath(DeathCause cause)
    {
        if (endingRun || currentState == GameState.Won || currentState == GameState.Lost) return;
        endingRun = true;
        Time.timeScale = 1f;
        if (countdownRoutine != null) StopCoroutine(countdownRoutine);
        SetState(GameState.Lost);
        GameEvents.RaisePlayerDied(cause, progress);
    }

    public void CompleteRun()
    {
        if (endingRun || currentState == GameState.Lost) return;
        endingRun = true;
        progress = 1f;
        GameEvents.RaiseProgressChanged(1f);
        SetState(GameState.Won);
        GameEvents.RaiseRunCompleted();
    }

    /// Allows the tracker to update normalized progress.
    public void SetProgress(float normalizedProgress)
    {
        progress = Mathf.Clamp01(normalizedProgress);
    }

    private IEnumerator CountdownRoutine()
    {
        if (countdownSeconds > 0f) yield return new WaitForSecondsRealtime(countdownSeconds);
        if (currentState != GameState.Countdown) yield break;
        countdownRoutine = null;
        SetState(GameState.Playing);
        GameEvents.RaiseCountdownFinished();
    }

    private void HandleHealthChanged(float normalized)
    {
        if (normalized <= 0f) RegisterDeath(DeathCause.Health);
    }

    private void HandleEnergyChanged(float normalized)
    {
        if (normalized <= 0f) RegisterDeath(DeathCause.Energy);
    }

    private void SetState(GameState state)
    {
        currentState = state;
        GameEvents.RaiseGameStateChanged(state);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }
}
