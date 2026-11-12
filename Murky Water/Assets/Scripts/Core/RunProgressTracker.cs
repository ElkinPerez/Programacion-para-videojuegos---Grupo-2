using UnityEngine;

/// Converts the distance traveled by the fish into normalized progress from 0 to 1.
public class RunProgressTracker : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private GameBalanceConfig balanceConfig;

    private float runStartX;
    private bool tracking;
    private bool completed;
    private float lastProgress = -1f;

    public float Progress { get; private set; }

    private void Awake()
    {
        if (player == null)
        {
            PlayerController controller = FindAnyObjectByType<PlayerController>();
            if (controller != null) player = controller.transform;
        }

        if (balanceConfig == null)
            Debug.LogWarning("RunProgressTracker: assign GameBalanceConfig.", this);
    }

    private void OnEnable()
    {
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        GameEvents.OnCountdownFinished += HandleCountdownFinished;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        GameEvents.OnCountdownFinished -= HandleCountdownFinished;
    }

    private void Update()
    {
        if (!tracking || completed || player == null) return;

        float totalDistance = balanceConfig != null ? balanceConfig.totalRunDistance : 900f;
        Progress = Mathf.Clamp01((player.position.x - runStartX) / Mathf.Max(1f, totalDistance));

        if (lastProgress < 0f || Mathf.Abs(Progress - lastProgress) >= 0.001f)
        {
            lastProgress = Progress;
            GameEvents.RaiseProgressChanged(Progress);
            if (GameManager.Instance != null) GameManager.Instance.SetProgress(Progress);
        }

        if (Progress >= 1f)
        {
            completed = true;
            tracking = false;
            if (GameManager.Instance != null) GameManager.Instance.CompleteRun();
        }
    }

    public void ResetProgress()
    {
        runStartX = player != null ? player.position.x : 0f;
        Progress = 0f;
        lastProgress = -1f;
        completed = false;
        tracking = false;
        GameEvents.RaiseProgressChanged(0f);
        if (GameManager.Instance != null) GameManager.Instance.SetProgress(0f);
    }

    private void BeginTracking()
    {
        if (player == null) return;
        runStartX = player.position.x;
        Progress = 0f;
        lastProgress = -1f;
        completed = false;
        tracking = true;
        GameEvents.RaiseProgressChanged(0f);
        if (GameManager.Instance != null) GameManager.Instance.SetProgress(0f);
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (state == GameState.Countdown)
        {
            ResetProgress();
            BeginTracking();
            tracking = false;
        }
        else if (state == GameState.Playing)
        {
            if (lastProgress < 0f) BeginTracking();
            tracking = true;
        }
        else
        {
            tracking = false;
        }
    }

    private void HandleCountdownFinished()
    {
        if (lastProgress < 0f) BeginTracking();
        tracking = true;
    }
}
