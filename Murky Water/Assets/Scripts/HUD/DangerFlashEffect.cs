using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Triggers a screen/icon warning flash when the predator is within danger distance,
/// strictly respecting ScreenFlashLimiter accessibility limits.
/// </summary>
public class DangerFlashEffect : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("UI Image (e.g., red border or warning icon) that flashes during danger")]
    [SerializeField] private Image dangerVisual;

    [Header("Flash Timing")]
    [SerializeField] private float flashVisibleDuration = 0.15f;
    [SerializeField] private float checkInterval = 0.4f;

    private bool isInDanger = false;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        if (dangerVisual != null)
        {
            dangerVisual.enabled = false;
        }
    }

    private void OnEnable()
    {
        GameEvents.OnPredatorDangerChanged += HandleDangerStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnPredatorDangerChanged -= HandleDangerStateChanged;
        StopFlashing();
    }

    private void HandleDangerStateChanged(bool dangerActive)
    {
        isInDanger = dangerActive;

        if (isInDanger && flashCoroutine == null)
        {
            flashCoroutine = StartCoroutine(DangerFlashRoutine());
        }
        else if (!isInDanger)
        {
            StopFlashing();
        }
    }

    private void StopFlashing()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        if (dangerVisual != null)
        {
            dangerVisual.enabled = false;
        }
    }

    private IEnumerator DangerFlashRoutine()
    {
        while (isInDanger)
        {
            bool canFlash = ScreenFlashLimiter.Instance == null || ScreenFlashLimiter.Instance.RequestFlash();

            if (canFlash && dangerVisual != null)
            {
                dangerVisual.enabled = true;
                yield return new WaitForSeconds(flashVisibleDuration);
                if (dangerVisual != null) dangerVisual.enabled = false;
            }

            yield return new WaitForSeconds(checkInterval);
        }
    }
}