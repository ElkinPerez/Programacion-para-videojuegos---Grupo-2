using UnityEngine;

/// Obstacle category. Matches the three types of the GDD (section 11).
public enum ObstacleType
{
    Energy,  // only removes energy (and may limit vision); no life damage, no invulnerability
    Danger,  // removes life and some energy; triggers invulnerability
    Lethal   // instant death
}

/// Data for one obstacle of the catalog. Create one asset per obstacle:
/// Assets > Create > Murky Water > Obstacle Data.
/// The generic Obstacle prefab reads everything from here (sprite, hitbox and hit values).
[CreateAssetMenu(fileName = "ObstacleData", menuName = "Murky Water/Obstacle Data")]
public class ObstacleData : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Obstacle";
    public ObstacleType type = ObstacleType.Danger;
    public Sprite sprite;
    public int sortingOrder = 0;

    [Header("Hitbox (world units)")]
    [Tooltip("Collider size in Unity units. Also used by the fairness rules.")]
    public Vector2 hitboxSize = new Vector2(1f, 1f);
    [Tooltip("Collider offset from the object's pivot.")]
    public Vector2 hitboxOffset = Vector2.zero;

    [Header("Hit values")]
    [Tooltip("ON: damage values come from GameBalanceConfig (recommended). OFF: use the custom values below.")]
    public bool useBalanceDefaults = true;
    [Range(0f, 100f)] public float lifeDamagePercent = 25f;
    [Range(0f, 100f)] public float energyDamagePercent = 10f;
    [Min(0f)] public float visionDuration = 0f;
    [Range(0f, 1f)] public float visionIntensity = 0f;

    [Tooltip("Free notes for the catalog (zones, behavior, etc.).")]
    [TextArea] public string notes;

    private void OnValidate()
    {
        hitboxSize.x = Mathf.Max(0.05f, hitboxSize.x);
        hitboxSize.y = Mathf.Max(0.05f, hitboxSize.y);
    }

    /// Builds the HitData sent to PlayerCollisionHandler.ApplyHit.
    /// Percentages are percentage points of the maximum (25 = 25%), never of the current value.
    public HitData BuildHitData(GameBalanceConfig config)
    {
        HitData hit = new HitData { source = HitSource.Obstacle };

        if (type == ObstacleType.Lethal)
        {
            hit.isLethal = true;
            hit.lifeDamagePercent = 100f;
            return hit;
        }

        if (useBalanceDefaults && config != null)
        {
            switch (type)
            {
                case ObstacleType.Danger:
                    hit.lifeDamagePercent = config.dangerObstacleLifeDamagePercent;
                    hit.energyDamagePercent = config.dangerObstacleEnergyDamagePercent;
                    break;
                case ObstacleType.Energy:
                    hit.lifeDamagePercent = 0f;
                    hit.energyDamagePercent = config.energyObstacleDamagePercent;
                    hit.visionDuration = config.energyObstacleVisionDuration;
                    hit.visionIntensity = config.energyObstacleVisionIntensity;
                    break;
            }
            return hit;
        }

        hit.lifeDamagePercent = lifeDamagePercent;
        hit.energyDamagePercent = energyDamagePercent;
        hit.visionDuration = visionDuration;
        hit.visionIntensity = visionIntensity;
        return hit;
    }
}
