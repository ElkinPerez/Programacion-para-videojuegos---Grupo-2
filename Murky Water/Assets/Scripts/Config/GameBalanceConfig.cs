using UnityEngine;


/// Global configuration editable in the Inspector. Prevents balance values
/// from being scattered across scripts. Create the asset via Assets > Create > Murky Water > Game Balance Config.
[CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "Murky Water/Game Balance Config")]
public class GameBalanceConfig : ScriptableObject
{
    [Header("Run")]
    [Min(1f)] public float totalRunDistance = 900f;
    [Min(1f)] public float targetRunDurationSeconds = 180f;

    [Header("Fish Movement")]
    [Min(0f)] public float horizontalBaseSpeed = 5f;
    [Min(0f)] public float verticalUpBaseSpeed = 4f;
    [Min(0f)] public float verticalUpMaxSpeed = 6f;
    [Min(0f)] public float verticalDownBaseSpeed = 4.5f;
    [Min(0f)] public float verticalDownMaxSpeed = 7f;
    [Min(0f)] public float verticalAcceleration = 8f;
    [Range(0f, 1f)] public float playerHitboxScale = 0.8f;

    [Header("Energy")]
    [Range(0f, 100f)] public float startingEnergyPercent = 100f;
    [Min(0f)] public float baseEnergyDrainPercentPerSecond = 2f;
    [Range(0f, 100f)] public float foodEnergyPercent = 20f;
    [Range(0f, 100f)] public float lowEnergyThreshold = 25f;
    [Range(0f, 1f)] public float minimumSpeedMultiplier = 0.5f;
    [Min(1f)] public float lowEnergySpeedMultiplier = 1.5f;
    [Range(0f, 100f)] public float energySpeedFullAtPercent = 100f;
    [Range(0f, 100f)] public float energySpeedHalfAtPercent = 10f;

    [Header("Health and Invulnerability")]
    [Range(0f, 100f)] public float startingHealthPercent = 100f;
    [Min(0f)] public float invulnerabilityDuration = 1.5f;
    [Min(0f)] public float invulnerabilityFlashesPerSecond = 2f;
    [Range(0f, 100f)] public float dangerObstacleLifeDamagePercent = 25f;
    [Range(0f, 100f)] public float dangerObstacleEnergyDamagePercent = 10f;
    [Range(0f, 100f)] public float energyObstacleDamagePercent = 50f;
    [Min(0f)] public float energyObstacleVisionDuration = 3f;
    [Range(0f, 1f)] public float energyObstacleVisionIntensity = 0.5f;

    [Header("Predator")]
    [Min(0f)] public float predatorInitialDistance = 12f;
    [Min(0f)] public float predatorBiteDistance = 1f;
    [Min(0f)] public float predatorDangerDistance = 5f;
    [Range(0f, 100f)] public float firstBiteLifeDamagePercent = 50f;
    [Min(0f)] public float predatorRetreatDistance = 8f;
    [Min(0f)] public float predatorRetreatDuration = 1f;

    [Header("Spawning and Fairness")]
    [Min(0f)] public float minimumObstacleHorizontalSpacing = 4f;
    [Min(0f)] public float minimumVerticalGapFishHeights = 2.5f;
    [Min(0f)] public float foodSpawnSpacingZone1 = 40f;
    [Min(0f)] public float foodSpawnSpacingZone2 = 28f;
    [Min(0f)] public float foodSpawnSpacingZone3 = 21f;
    [Min(0f)] public float foodSpawnSpacingZone4 = 17f;

    [Header("Camera")]
    [Range(0f, 1f)] public float playerScreenHorizontalAnchor = 0.25f;
    [Min(0f)] public float cameraFollowSmoothTime = 0.2f;
    [Min(0f)] public float cameraShakeDuration = 0.2f;

    private void OnValidate()
    {
        totalRunDistance = Mathf.Max(1f, totalRunDistance);
        targetRunDurationSeconds = Mathf.Max(1f, targetRunDurationSeconds);
        horizontalBaseSpeed = Mathf.Max(0f, horizontalBaseSpeed);
        verticalAcceleration = Mathf.Max(0f, verticalAcceleration);
        minimumObstacleHorizontalSpacing = Mathf.Max(0f, minimumObstacleHorizontalSpacing);
        minimumVerticalGapFishHeights = Mathf.Max(0f, minimumVerticalGapFishHeights);
        energySpeedHalfAtPercent = Mathf.Min(energySpeedHalfAtPercent, energySpeedFullAtPercent);
        lowEnergySpeedMultiplier = Mathf.Max(1f, lowEnergySpeedMultiplier);
    }
}
