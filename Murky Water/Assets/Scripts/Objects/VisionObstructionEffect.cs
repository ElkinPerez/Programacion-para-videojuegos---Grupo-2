using System.Collections;
using UnityEngine;

/// Darkens the screen (vignette overlay) for the duration and intensity received
/// through GameEvents.OnVisionObstructed. Assign a full-screen UI Image with a
/// dark vignette sprite, wrapped in a CanvasGroup (alpha 0 at start).
public class VisionObstructionEffect : MonoBehaviour
{
    [SerializeField] private CanvasGroup overlay;
    [SerializeField, Min(0f)] private float fadeInSeconds = 0.2f;
    [SerializeField, Min(0f)] private float fadeOutSeconds = 0.6f;
    [Tooltip("Overlay alpha when intensity = 1.")]
    [SerializeField, Range(0f, 1f)] private float maxOverlayAlpha = 0.95f;

    private Coroutine routine;
    private float currentTarget;

    private void Awake()
    {
        if (overlay == null)
        {
            Debug.LogWarning("VisionObstructionEffect: assign the overlay CanvasGroup.", this);
            return;
        }
        overlay.alpha = 0f;
        overlay.interactable = false;
        overlay.blocksRaycasts = false;
    }

    private void OnEnable()
    {
        GameEvents.OnVisionObstructed += HandleVisionObstructed;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnVisionObstructed -= HandleVisionObstructed;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        Hide();
    }

    private void HandleVisionObstructed(float duration, float intensity)
    {
        if (overlay == null || duration <= 0f || intensity <= 0f) return;

        float target = intensity * maxOverlayAlpha;
        if (routine != null)
        {
            // A new hit while still blind: restart the timer and keep the strongest intensity.
            StopCoroutine(routine);
            target = Mathf.Max(target, currentTarget);
        }
        currentTarget = target;
        routine = StartCoroutine(PlayEffect(duration, target));
    }

    private IEnumerator PlayEffect(float duration, float target)
    {
        float start = overlay.alpha;
        float t = 0f;
        while (t < fadeInSeconds)
        {
            t += Time.deltaTime;
            overlay.alpha = Mathf.Lerp(start, target, fadeInSeconds > 0f ? t / fadeInSeconds : 1f);
            yield return null;
        }
        overlay.alpha = target;

        float hold = Mathf.Max(0f, duration - fadeInSeconds - fadeOutSeconds);
        float elapsed = 0f;
        while (elapsed < hold)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        t = 0f;
        while (t < fadeOutSeconds)
        {
            t += Time.deltaTime;
            overlay.alpha = Mathf.Lerp(target, 0f, fadeOutSeconds > 0f ? t / fadeOutSeconds : 1f);
            yield return null;
        }

        overlay.alpha = 0f;
        routine = null;
        currentTarget = 0f;
    }

    private void HandleGameStateChanged(GameState state)
    {
        // The run is not active (menu, countdown, win, loss): clear the effect immediately.
        if (state != GameState.Playing && state != GameState.Paused) Hide();
    }

    private void Hide()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        currentTarget = 0f;
        if (overlay != null) overlay.alpha = 0f;
    }
}
