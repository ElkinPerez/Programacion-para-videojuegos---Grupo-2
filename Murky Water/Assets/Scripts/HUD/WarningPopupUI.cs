using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays temporary 2-second warning popups in Spanish for low energy, predator proximity, and reduced vision.
/// </summary>
public class WarningPopupUI : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private GameBalanceConfig balanceConfig;
    [SerializeField] private float displayDurationSeconds = 2f;

    [Header("UI References")]
    [SerializeField] private GameObject popupContainer;
    [SerializeField] private TextMeshProUGUI warningText;

    private bool lowEnergyNotified = false;
    private Coroutine hideCoroutine;

    private void Awake()
    {
        if (popupContainer != null)
        {
            popupContainer.SetActive(false);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnEnergyChanged += HandleEnergyChanged;
        GameEvents.OnPredatorDangerChanged += HandlePredatorDangerChanged;
        GameEvents.OnVisionObstructed += HandleVisionObstructed;
    }

    private void OnDisable()
    {
        GameEvents.OnEnergyChanged -= HandleEnergyChanged;
        GameEvents.OnPredatorDangerChanged -= HandlePredatorDangerChanged;
        GameEvents.OnVisionObstructed -= HandleVisionObstructed;
    }

    private void HandleEnergyChanged(float normalizedEnergy)
    {
        if (balanceConfig == null) return;

        float thresholdNormalized = balanceConfig.lowEnergyThreshold / 100f;

        if (normalizedEnergy <= thresholdNormalized && !lowEnergyNotified)
        {
            lowEnergyNotified = true;
            ShowWarning("¡ENERGÍA BAJA!");
        }
        else if (normalizedEnergy > thresholdNormalized)
        {
            lowEnergyNotified = false;
        }
    }

    private void HandlePredatorDangerChanged(bool isDanger)
    {
        if (isDanger)
        {
            ShowWarning("¡DEPREDADOR CERCA!");
        }
    }

    private void HandleVisionObstructed(float duration, float intensity)
    {
        if (duration > 0f)
        {
            ShowWarning("¡VISIÓN REDUCIDA!");
        }
    }

    private void ShowWarning(string message)
    {
        if (popupContainer == null || warningText == null)
        {
            Debug.LogWarning("[WarningPopupUI] Popup container or TextMeshPro reference is missing.");
            return;
        }

        warningText.text = message;
        popupContainer.SetActive(true);

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDurationSeconds);
        if (popupContainer != null)
        {
            popupContainer.SetActive(false);
        }
        hideCoroutine = null;
    }
}