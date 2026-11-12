using System;
using UnityEngine;


/// Single event catalog for Murky Water.
/// Systems emit events through the Raise... methods, and the rest of the game listens through On...
/// Do not add or rename events without coordinating

public static class GameEvents
{
    public static event Action<HitData> OnPlayerHit;
    public static event Action<float> OnHealthChanged;
    public static event Action<float> OnEnergyChanged;
    public static event Action<bool> OnInvulnerabilityChanged;
    public static event Action<float> OnFoodConsumed;
    public static event Action<float, float> OnVisionObstructed;
    public static event Action<int> OnPredatorBite;
    public static event Action OnPredatorRetreat;
    public static event Action<float> OnPredatorDistanceChanged;
    public static event Action<bool> OnPredatorDangerChanged;
    public static event Action<float> OnProgressChanged;
    public static event Action<PhaseConfig> OnPhaseChanged;
    public static event Action<GameState> OnGameStateChanged;
    public static event Action OnCountdownFinished;
    public static event Action<DeathCause, float> OnPlayerDied;
    public static event Action OnRunCompleted;

    public static void RaisePlayerHit(HitData data) => OnPlayerHit?.Invoke(data);
    public static void RaiseHealthChanged(float normalized) => OnHealthChanged?.Invoke(Mathf.Clamp01(normalized));
    public static void RaiseEnergyChanged(float normalized) => OnEnergyChanged?.Invoke(Mathf.Clamp01(normalized));
    public static void RaiseInvulnerabilityChanged(bool active) => OnInvulnerabilityChanged?.Invoke(active);
    public static void RaiseFoodConsumed(float energyPercent) => OnFoodConsumed?.Invoke(energyPercent);
    public static void RaiseVisionObstructed(float duration, float intensity) =>
        OnVisionObstructed?.Invoke(Mathf.Max(0f, duration), Mathf.Clamp01(intensity));
    public static void RaisePredatorBite(int biteNumber) => OnPredatorBite?.Invoke(Mathf.Max(1, biteNumber));
    public static void RaisePredatorRetreat() => OnPredatorRetreat?.Invoke();
    public static void RaisePredatorDistanceChanged(float distance) =>
        OnPredatorDistanceChanged?.Invoke(Mathf.Max(0f, distance));
    public static void RaisePredatorDangerChanged(bool danger) => OnPredatorDangerChanged?.Invoke(danger);
    public static void RaiseProgressChanged(float normalized) => OnProgressChanged?.Invoke(Mathf.Clamp01(normalized));
    public static void RaisePhaseChanged(PhaseConfig phase)
    {
        if (phase != null) OnPhaseChanged?.Invoke(phase);
    }
    public static void RaiseGameStateChanged(GameState state) => OnGameStateChanged?.Invoke(state);
    public static void RaiseCountdownFinished() => OnCountdownFinished?.Invoke();
    public static void RaisePlayerDied(DeathCause cause, float progress) =>
        OnPlayerDied?.Invoke(cause, Mathf.Clamp01(progress));
    public static void RaiseRunCompleted() => OnRunCompleted?.Invoke();

    // Useful for preventing residual static subscribers when entering Play Mode
    // while Unity has Domain Reload disabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticEvents()
    {
        OnPlayerHit = null;
        OnHealthChanged = null;
        OnEnergyChanged = null;
        OnInvulnerabilityChanged = null;
        OnFoodConsumed = null;
        OnVisionObstructed = null;
        OnPredatorBite = null;
        OnPredatorRetreat = null;
        OnPredatorDistanceChanged = null;
        OnPredatorDangerChanged = null;
        OnProgressChanged = null;
        OnPhaseChanged = null;
        OnGameStateChanged = null;
        OnCountdownFinished = null;
        OnPlayerDied = null;
        OnRunCompleted = null;
    }
}
