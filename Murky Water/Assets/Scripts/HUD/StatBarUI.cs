using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reusable UI bar controller for Health and Energy stats (normalized 0 to 1).
/// </summary>
public class StatBarUI : MonoBehaviour
{
    public enum StatType { Health, Energy }

    [Header("Configuration")]
    [SerializeField] private StatType statType = StatType.Health;

    [Header("UI References")]
    [Tooltip("Image component with Image Type set to Filled")]
    [SerializeField] private Image fillImage;

    private void OnEnable()
    {
        if (statType == StatType.Health)
        {
            GameEvents.OnHealthChanged += UpdateBar;
        }
        else
        {
            GameEvents.OnEnergyChanged += UpdateBar;
        }
    }

    private void OnDisable()
    {
        if (statType == StatType.Health)
        {
            GameEvents.OnHealthChanged -= UpdateBar;
        }
        else
        {
            GameEvents.OnEnergyChanged -= UpdateBar;
        }
    }

    private void UpdateBar(float normalizedValue)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01(normalizedValue);
        }
        else
        {
            Debug.LogWarning($"[StatBarUI] Fill Image is not assigned on {gameObject.name}.");
        }
    }
}