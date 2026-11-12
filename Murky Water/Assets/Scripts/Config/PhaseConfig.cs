using UnityEngine;

/// Configuration for a zone and assign them to the PollutionManager.
[CreateAssetMenu(fileName = "PhaseConfig", menuName = "Murky Water/Phase Config")]
public class PhaseConfig : ScriptableObject
{
    [Header("Identity and Range")]
    [Min(1)] public int phaseNumber = 1;
    public string phaseName = "Initial";
    [Range(0f, 1f)] public float startProgress = 0f;
    [Range(0f, 1f)] public float endProgress = 0.2f;

    [Header("Survival")]
    [Min(0f)] public float energyDrainPercentPerSecond = 2f;
    [Min(0f)] public float predatorSpeed = 3.5f;

    [Header("Obstacle Spawning")]
    [Min(0)] public int obstaclesPerTwentyUnits = 1;
    [Range(0f, 1f)] public float energyObstacleWeight = 0.70f;
    [Range(0f, 1f)] public float dangerObstacleWeight = 0.20f;
    [Range(0f, 1f)] public float lethalObstacleWeight = 0.10f;
    [Min(0f)] public float foodSpacing = 40f;

    [Tooltip("Optional: obstacles exclusive to this zone are managed in SpawnTable.")]
    public string notes;

    private void OnValidate()
    {
        startProgress = Mathf.Clamp01(startProgress);
        endProgress = Mathf.Clamp(endProgress, startProgress, 1f);
        energyDrainPercentPerSecond = Mathf.Max(0f, energyDrainPercentPerSecond);
        predatorSpeed = Mathf.Max(0f, predatorSpeed);
        obstaclesPerTwentyUnits = Mathf.Max(0, obstaclesPerTwentyUnits);

        // Normalize weights to prevent tables from having a sum different from 1.
        float sum = energyObstacleWeight + dangerObstacleWeight + lethalObstacleWeight;
        if (sum > 0f)
        {
            energyObstacleWeight /= sum;
            dangerObstacleWeight /= sum;
            lethalObstacleWeight /= sum;
        }
    }
}
