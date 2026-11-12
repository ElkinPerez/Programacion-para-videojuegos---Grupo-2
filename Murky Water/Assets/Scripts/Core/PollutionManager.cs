using System;
using UnityEngine;

/// Selects and announces the pollution phase based on route progress.
public class PollutionManager : MonoBehaviour
{
    [Tooltip("Assign the four phases in order; they will be sorted by Start Progress when the game starts.")]
    [SerializeField] private PhaseConfig[] phases = new PhaseConfig[4];

    private PhaseConfig currentPhase;

    public PhaseConfig CurrentPhase => currentPhase;

    private void Awake()
    {
        Array.Sort(phases, (a, b) =>
        {
            if (a == null) return b == null ? 0 : 1;
            if (b == null) return -1;
            return a.startProgress.CompareTo(b.startProgress);
        });
    }

    private void OnEnable() => GameEvents.OnProgressChanged += HandleProgressChanged;
    private void OnDisable() => GameEvents.OnProgressChanged -= HandleProgressChanged;

    private void Start()
    {
        if (phases == null || phases.Length == 0)
        {
            Debug.LogError("PollutionManager: assign all four PhaseConfig assets in the Inspector.", this);
            return;
        }

        for (int i = 0; i < phases.Length; i++)
        {
            if (phases[i] == null)
                Debug.LogWarning($"PollutionManager: the phase at index {i} has not been assigned.", this);

        }

        EvaluatePhase(0f);
    }

    private void HandleProgressChanged(float progress) => EvaluatePhase(progress);

    private void EvaluatePhase(float progress)
    {
        if (phases == null || phases.Length == 0) return;

        PhaseConfig selected = null;
        for (int i = 0; i < phases.Length; i++)
        {
            PhaseConfig phase = phases[i];
            if (phase == null) continue;

            bool insideRange = progress >= phase.startProgress &&
                               (progress < phase.endProgress || Mathf.Approximately(progress, 1f));
            if (insideRange) selected = phase;
        }

        // If the ranges leave a gap, keep the most recent phase whose start has already been reached.
        if (selected == null)
        {
            for (int i = 0; i < phases.Length; i++)
            {
                if (phases[i] != null && phases[i].startProgress <= progress)
                    selected = phases[i];
            }
        }

        if (selected == null || selected == currentPhase) return;

        currentPhase = selected;
        GameEvents.RaisePhaseChanged(currentPhase);
    }
}
