using UnityEngine;

/// Manages energy, phase-based drain, fixed damage, and food recovery.
public class EnergySystem : MonoBehaviour
{
    [SerializeField] private GameBalanceConfig balanceConfig;
    [SerializeField] private PhaseConfig startingPhase;
    [SerializeField, Range(0f, 100f)] private float currentEnergyPercent = 100f;

    private PhaseConfig currentPhase;
    private bool isPlaying;

    public float CurrentEnergyPercent => currentEnergyPercent;
    public float NormalizedEnergy => currentEnergyPercent / 100f;
    public bool IsEmpty => currentEnergyPercent <= 0f;

    private void Awake()
    {
        if (balanceConfig == null)
            Debug.LogWarning("EnergySystem: assign GameBalanceConfig in the Inspector.", this);
        currentPhase = startingPhase;
        currentEnergyPercent = balanceConfig != null ? balanceConfig.startingEnergyPercent : 100f;
        PublishEnergy();
    }

    private void OnEnable()
    {
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        GameEvents.OnPhaseChanged += HandlePhaseChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        GameEvents.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void Update()
    {
        if (!isPlaying || currentEnergyPercent <= 0f) return;
        float drain = currentPhase != null
            ? currentPhase.energyDrainPercentPerSecond
            : balanceConfig != null ? balanceConfig.baseEnergyDrainPercentPerSecond : 2f;
        if (drain > 0f) ModifyEnergy(-drain * Time.deltaTime);
    }

    public void ApplyDamagePercent(float percent)
    {
        if (percent > 0f) ModifyEnergy(-percent);
    }

    public void RecoverPercent(float percent)
    {
        if (percent > 0f && currentEnergyPercent > 0f) ModifyEnergy(percent);
    }

    public void ResetEnergy()
    {
        currentEnergyPercent = balanceConfig != null ? balanceConfig.startingEnergyPercent : 100f;
        PublishEnergy();
    }

    private void ModifyEnergy(float amount)
    {
        float before = currentEnergyPercent;
        currentEnergyPercent = Mathf.Clamp(currentEnergyPercent + amount, 0f, 100f);
        if (!Mathf.Approximately(before, currentEnergyPercent)) PublishEnergy();
    }

    private void PublishEnergy() => GameEvents.RaiseEnergyChanged(NormalizedEnergy);
    private void HandleGameStateChanged(GameState state) => isPlaying = state == GameState.Playing;
    private void HandlePhaseChanged(PhaseConfig phase) { if (phase != null) currentPhase = phase; }
}
