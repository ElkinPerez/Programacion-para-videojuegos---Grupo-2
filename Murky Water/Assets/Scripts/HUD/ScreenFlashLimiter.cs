using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Accessibility manager that enforces WCAG 2.2 photosensitivity limits:
/// Maximum 3 flashes per second globally (or 1 per second if Reduce Flashes is enabled).
/// </summary>
public class ScreenFlashLimiter : MonoBehaviour
{
    public static ScreenFlashLimiter Instance { get; private set; }

    [Header("Accessibility Fallback")]
    [Tooltip("Default state if AccessibilitySettings is not yet present in the scene")]
    [SerializeField] private bool reduceFlashesFallback = false;

    private readonly Queue<float> flashTimestamps = new Queue<float>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Requests permission to trigger a visual flash. Returns true if within safe limits.
    /// </summary>
    public bool RequestFlash()
    {
        float now = Time.unscaledTime;

        // Remove timestamps older than 1 second
        while (flashTimestamps.Count > 0 && now - flashTimestamps.Peek() >= 1f)
        {
            flashTimestamps.Dequeue();
        }

        int maxFlashesPerSecond = IsReduceFlashesEnabled() ? 1 : 3;

        if (flashTimestamps.Count < maxFlashesPerSecond)
        {
            flashTimestamps.Enqueue(now);
            return true;
        }

        return false;
    }

    private bool IsReduceFlashesEnabled()
    {
        // Note: Connect to AccessibilitySettings.Instance.ReduceFlashes once integrated
        return reduceFlashesFallback;
    }
}