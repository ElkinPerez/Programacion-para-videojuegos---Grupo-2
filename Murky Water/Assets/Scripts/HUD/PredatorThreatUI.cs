using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the predator's proximity relative to the fish using GameBalanceConfig distances.
/// </summary>
public class PredatorThreatUI : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private GameBalanceConfig balanceConfig;

    [Header("UI References")]
    [Tooltip("Filled Image or Slider representing threat level (1 = biting, 0 = far away)")]
    [SerializeField] private Image threatFillImage;
    [SerializeField] private Slider threatSlider;

    private void OnEnable()
    {
        GameEvents.OnPredatorDistanceChanged += UpdateThreatDistance;
    }

    private void OnDisable()
    {
        GameEvents.OnPredatorDistanceChanged -= UpdateThreatDistance;
    }

    private void UpdateThreatDistance(float currentDistance)
    {
        if (balanceConfig == null)
        {
            Debug.LogError("[PredatorThreatUI] GameBalanceConfig is missing.");
            return;
        }

        // Inverse lerp: 1.0 when at bite distance, 0.0 when at initial (safe) distance
        float maxDist = balanceConfig.predatorInitialDistance;
        float minDist = balanceConfig.predatorBiteDistance;
        float threatNormalized = 1f - Mathf.InverseLerp(minDist, maxDist, currentDistance);

        if (threatFillImage != null)
        {
            threatFillImage.fillAmount = threatNormalized;
        }

        if (threatSlider != null)
        {
            threatSlider.value = threatNormalized;
        }
    }
}