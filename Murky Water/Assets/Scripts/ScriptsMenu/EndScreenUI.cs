using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Shows the defeat or victory panel when the run ends, and wires their Restart / Menu buttons.
/// It only listens to events: GameManager keeps deciding the state (Lost / Won).
///  - Defeat: waits defeatDelaySeconds so the death animation can be seen, then shows the panel.
///  - Victory: waits victoryDelaySeconds, then shows the panel.
///  - Both panels are hidden at start and when a new run begins.
/// Place this on an object that is ALWAYS active (e.g., the Canvas or a Managers object), and keep
/// the panels outside the HUD container: HUDController hides the HUD as soon as the run ends.
public class EndScreenUI : MonoBehaviour
{
    [Header("Panels (scene objects, outside the HUD group)")]
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private GameObject victoryPanel;

    [Header("Timing")]
    [Tooltip("Seconds to let the death animation play before showing the defeat panel.")]
    [SerializeField, Min(0f)] private float defeatDelaySeconds = 1.5f;
    [SerializeField, Min(0f)] private float victoryDelaySeconds = 0.5f;

    [Header("Buttons (optional: they are hooked by code, do not also set OnClick)")]
    [SerializeField] private Button[] restartButtons;
    [SerializeField] private Button[] menuButtons;

    [Header("Defeat texts (optional)")]
    [Tooltip("Shows the route progress reached. {0} is replaced by the percentage.")]
    [SerializeField] private TextMeshProUGUI defeatProgressText;
    [SerializeField] private string progressFormat = "Progreso: {0}%";

    private Coroutine showRoutine;

    private void Awake()
    {
        HideAll();

        foreach (Button button in restartButtons)
            if (button != null) button.onClick.AddListener(Restart);
        foreach (Button button in menuButtons)
            if (button != null) button.onClick.AddListener(GoToMenu);

        if (defeatPanel == null) Debug.LogWarning("EndScreenUI: assign the defeat panel.", this);
        if (victoryPanel == null) Debug.LogWarning("EndScreenUI: assign the victory panel.", this);
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerDied += HandlePlayerDied;
        GameEvents.OnRunCompleted += HandleRunCompleted;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerDied -= HandlePlayerDied;
        GameEvents.OnRunCompleted -= HandleRunCompleted;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        StopShowRoutine();
    }

    private void OnDestroy()
    {
        foreach (Button button in restartButtons)
            if (button != null) button.onClick.RemoveListener(Restart);
        foreach (Button button in menuButtons)
            if (button != null) button.onClick.RemoveListener(GoToMenu);
    }

    // Public so a Button's OnClick can also call them if you prefer to hook them by hand.
    public void Restart()
    {
        if (GameManager.Instance != null) GameManager.Instance.RestartRun();
        else Debug.LogWarning("EndScreenUI: there is no GameManager in the scene.", this);
    }

    public void GoToMenu()
    {
        if (GameManager.Instance != null) GameManager.Instance.GoToMenu();
        else Debug.LogWarning("EndScreenUI: there is no GameManager in the scene.", this);
    }

    private void HandlePlayerDied(DeathCause cause, float progress)
    {
        StopShowRoutine();

        if (defeatProgressText != null)
            defeatProgressText.text = string.Format(progressFormat, Mathf.RoundToInt(Mathf.Clamp01(progress) * 100f));

        showRoutine = StartCoroutine(ShowAfterDelay(defeatPanel, defeatDelaySeconds));
    }

    private void HandleRunCompleted()
    {
        StopShowRoutine();
        showRoutine = StartCoroutine(ShowAfterDelay(victoryPanel, victoryDelaySeconds));
    }

    // A new run (Countdown) or the menu: make sure no end panel stays on screen.
    private void HandleGameStateChanged(GameState state)
    {
        if (state == GameState.Countdown || state == GameState.Playing || state == GameState.Menu)
        {
            StopShowRoutine();
            HideAll();
        }
    }

    // Realtime, so the delay works even if timeScale is ever changed.
    private IEnumerator ShowAfterDelay(GameObject panel, float seconds)
    {
        if (seconds > 0f) yield return new WaitForSecondsRealtime(seconds);

        HideAll();
        if (panel != null) panel.SetActive(true);
        showRoutine = null;
    }

    private void StopShowRoutine()
    {
        if (showRoutine == null) return;
        StopCoroutine(showRoutine);
        showRoutine = null;
    }

    private void HideAll()
    {
        if (defeatPanel != null) defeatPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }
}
