using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Updates the run progress indicator (Slider or Filled Image) from 0 to 1.
/// </summary>
public class ProgressIndicatorUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Assign a UI Slider if using a slider-based progress bar")]
    [SerializeField] private Slider progressSlider;

    [Tooltip("Assign a Filled Image if using an image-based progress bar")]
    [SerializeField] private Image progressFillImage;

    private void OnEnable()
    {
        GameEvents.OnProgressChanged += UpdateProgress;
    }

    private void OnDisable()
    {
        GameEvents.OnProgressChanged -= UpdateProgress;
    }

    private void UpdateProgress(float normalizedProgress)
    {
        float clamped = Mathf.Clamp01(normalizedProgress);

        if (progressSlider != null)
        {
            progressSlider.value = clamped;
        }

        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = clamped;
        }
    }
}