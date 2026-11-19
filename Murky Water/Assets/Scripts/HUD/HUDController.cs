using UnityEngine;

/// <summary>
/// Controls the visibility of the main HUD container based on the current GameState.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Parent GameObject containing all HUD elements")]
    [SerializeField] private GameObject hudContainer;

    private void OnEnable()
    {
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (hudContainer == null)
        {
            Debug.LogWarning("[HUDController] HUD Container reference is missing.");
            return;
        }

        // Show HUD only during active gameplay or countdown
        bool shouldShow = (newState == GameState.Playing || newState == GameState.Countdown);
        hudContainer.SetActive(shouldShow);
    }
}
