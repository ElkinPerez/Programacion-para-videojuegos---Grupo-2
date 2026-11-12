using UnityEngine;
using UnityEngine.InputSystem;

/// Temporary tool for testing core events from a test scene.
public class DebugEventTester : MonoBehaviour
{
    [SerializeField] private bool enableKeyboardShortcuts = true;
    [SerializeField, Range(0f, 1f)] private float testHealth = 0.5f;
    [SerializeField, Range(0f, 1f)] private float testEnergy = 0.3f;

    private void Update()
    {
        if (!enableKeyboardShortcuts || Keyboard.current == null) return;
        if (Keyboard.current.f1Key.wasPressedThisFrame) GameEvents.RaiseHealthChanged(testHealth);
        if (Keyboard.current.f2Key.wasPressedThisFrame) GameEvents.RaiseEnergyChanged(testEnergy);
        if (Keyboard.current.f3Key.wasPressedThisFrame) GameEvents.RaiseInvulnerabilityChanged(true);
        if (Keyboard.current.f4Key.wasPressedThisFrame) GameEvents.RaiseVisionObstructed(2f, 0.7f);
        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            HitData sample = new HitData
            {
                isLethal = false,
                lifeDamagePercent = 40f,
                energyDamagePercent = 0f,
                visionDuration = 0f,
                visionIntensity = 0f,
                source = HitSource.Obstacle
            };
            GameEvents.RaisePlayerHit(sample);
        }
    }

    [ContextMenu("Test: publish health at 50%")]
    private void TestHealthEvent() => GameEvents.RaiseHealthChanged(0.5f);

    [ContextMenu("Test: publish energy at 30%")]
    private void TestEnergyEvent() => GameEvents.RaiseEnergyChanged(0.3f);
}
