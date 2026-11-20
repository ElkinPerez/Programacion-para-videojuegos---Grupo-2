using System.Collections.Generic;
using UnityEngine;

/// Spawn table for ONE zone. Create one asset per zone: Assets > Create > Murky Water > Spawn Table.
/// - The obstacle density (per 20 u) and the probability of each TYPE (energy / danger / lethal)
///   come from the zone's PhaseConfig, so they are not duplicated here.
/// - This table decides WHICH obstacles of each type exist in the zone and how likely each one is.
///   Zone-unique obstacles are just extra entries in this table.
/// - Zone 1: the only lethal entry must be the hook.
/// - Food also has weights: a higher weight means that food appears more often.
[CreateAssetMenu(fileName = "SpawnTable", menuName = "Murky Water/Spawn Table")]
public class SpawnTable : ScriptableObject
{
    [System.Serializable]
    public class ObstacleEntry
    {
        public ObstacleData data;
        [Min(0f)] public float weight = 1f;
        [Tooltip("Catalog flag: this obstacle only appears in this zone.")]
        public bool zoneUnique;
    }

    [System.Serializable]
    public class FoodEntry
    {
        [Tooltip("Food prefab (FoodItem).")]
        public GameObject prefab;
        [Tooltip("Higher = appears more often. Chance = weight / sum of all food weights. 0 = never.")]
        [Min(0f)] public float weight = 1f;
    }

    [Header("Zone")]
    [Tooltip("PhaseConfig of this zone. Its range, density, type weights and food spacing are used.")]
    public PhaseConfig phase;

    [Header("Obstacles")]
    public ObstacleEntry[] obstacles;

    [Header("Food")]
    [Tooltip("Food prefabs (FoodItem) with a weight each. Give good food a higher weight so it shows up more.")]
    public FoodEntry[] foodEntries;

    // Old list (no weights). Kept only so existing assets are not lost: OnValidate moves
    // its contents into foodEntries with weight 1 and then empties it.
    [SerializeField, HideInInspector] private GameObject[] foodPrefabs;

    [Header("Surface litter (level 2)")]
    public bool allowSurfaceLitter = false;
    [Min(0f)] public float litterPerTwentyUnits = 0.5f;
    public ObstacleEntry[] litterEntries;

    /// Picks the type according to the zone's weights (only among types that have entries),
    /// then an obstacle of that type according to the entry weights.
    public ObstacleData PickObstacle(System.Random rng)
    {
        if (obstacles == null || rng == null) return null;

        float energy = TypeWeight(ObstacleType.Energy, phase != null ? phase.energyObstacleWeight : 1f);
        float danger = TypeWeight(ObstacleType.Danger, phase != null ? phase.dangerObstacleWeight : 1f);
        float lethal = TypeWeight(ObstacleType.Lethal, phase != null ? phase.lethalObstacleWeight : 1f);
        float total = energy + danger + lethal;

        if (total <= 0f) return PickFromEntries(obstacles, null, rng);

        double roll = rng.NextDouble() * total;
        ObstacleType chosen = roll < energy ? ObstacleType.Energy
            : roll < energy + danger ? ObstacleType.Danger
            : ObstacleType.Lethal;

        ObstacleData picked = PickFromEntries(obstacles, chosen, rng);
        return picked != null ? picked : PickFromEntries(obstacles, null, rng);
    }

    public ObstacleData PickLitter(System.Random rng)
    {
        return PickFromEntries(litterEntries, null, rng);
    }

    /// Picks a food prefab according to the entry weights.
    public GameObject PickFood(System.Random rng)
    {
        if (rng == null) return null;

        float total = FoodWeight();
        if (total <= 0f) return PickLegacyFood(rng);

        double roll = rng.NextDouble() * total;
        GameObject last = null;
        foreach (FoodEntry entry in foodEntries)
        {
            if (entry == null || entry.prefab == null || entry.weight <= 0f) continue;

            last = entry.prefab;
            if (roll < entry.weight) return entry.prefab;
            roll -= entry.weight;
        }
        return last;
    }

    /// Every food prefab of this table (used by ObstacleSpawner to warm up the pool).
    public GameObject[] GetAllFoodPrefabs()
    {
        List<GameObject> list = new List<GameObject>();

        if (foodEntries != null)
        {
            foreach (FoodEntry entry in foodEntries)
                if (entry != null && entry.prefab != null) list.Add(entry.prefab);
        }

        if (foodPrefabs != null)
        {
            foreach (GameObject prefab in foodPrefabs)
                if (prefab != null && !list.Contains(prefab)) list.Add(prefab);
        }

        return list.ToArray();
    }

    /// Distance between foods in this zone (GDD: 40 / 28 / 21 / 17 u).
    public float GetFoodSpacing(GameBalanceConfig config)
    {
        if (phase != null && phase.foodSpacing > 0f) return phase.foodSpacing;
        if (config == null) return 40f;

        switch (phase != null ? phase.phaseNumber : 1)
        {
            case 1: return config.foodSpawnSpacingZone1;
            case 2: return config.foodSpawnSpacingZone2;
            case 3: return config.foodSpawnSpacingZone3;
            default: return config.foodSpawnSpacingZone4;
        }
    }

    // Weight of a type: the zone weight if the table has at least one entry of that type, otherwise 0.
    private float TypeWeight(ObstacleType type, float zoneWeight)
    {
        return EntriesWeight(obstacles, type) > 0f ? Mathf.Max(0f, zoneWeight) : 0f;
    }

    private float FoodWeight()
    {
        if (foodEntries == null) return 0f;
        float sum = 0f;
        foreach (FoodEntry entry in foodEntries)
        {
            if (entry == null || entry.prefab == null || entry.weight <= 0f) continue;
            sum += entry.weight;
        }
        return sum;
    }

    // Fallback for assets that still have the old list and were not migrated yet: uniform random.
    private GameObject PickLegacyFood(System.Random rng)
    {
        if (foodPrefabs == null) return null;

        int valid = 0;
        foreach (GameObject prefab in foodPrefabs)
            if (prefab != null) valid++;
        if (valid == 0) return null;

        int target = rng.Next(valid);
        foreach (GameObject prefab in foodPrefabs)
        {
            if (prefab == null) continue;
            if (target-- == 0) return prefab;
        }
        return null;
    }

    private static float EntriesWeight(ObstacleEntry[] entries, ObstacleType? type)
    {
        if (entries == null) return 0f;
        float sum = 0f;
        foreach (ObstacleEntry entry in entries)
        {
            if (entry == null || entry.data == null || entry.weight <= 0f) continue;
            if (type.HasValue && entry.data.type != type.Value) continue;
            sum += entry.weight;
        }
        return sum;
    }

    private static ObstacleData PickFromEntries(ObstacleEntry[] entries, ObstacleType? type, System.Random rng)
    {
        float total = EntriesWeight(entries, type);
        if (total <= 0f || rng == null) return null;

        double roll = rng.NextDouble() * total;
        ObstacleData last = null;
        foreach (ObstacleEntry entry in entries)
        {
            if (entry == null || entry.data == null || entry.weight <= 0f) continue;
            if (type.HasValue && entry.data.type != type.Value) continue;

            last = entry.data;
            if (roll < entry.weight) return entry.data;
            roll -= entry.weight;
        }
        return last;
    }

    private void OnValidate()
    {
        litterPerTwentyUnits = Mathf.Max(0f, litterPerTwentyUnits);

        MigrateOldFoodList();

        // GDD: in zone 1 the only lethal obstacle is the hook.
        if (obstacles == null || phase == null || phase.phaseNumber != 1) return;
        ObstacleData firstLethal = null;
        foreach (ObstacleEntry entry in obstacles)
        {
            if (entry == null || entry.data == null || entry.data.type != ObstacleType.Lethal) continue;
            if (firstLethal == null) firstLethal = entry.data;
            else if (entry.data != firstLethal)
            {
                Debug.LogWarning("SpawnTable: zone 1 should only have ONE lethal obstacle (the hook).", this);
                break;
            }
        }
    }

    // Moves the old unweighted list into foodEntries (weight 1) once, keeping the assigned prefabs.
    private void MigrateOldFoodList()
    {
        if (foodPrefabs == null || foodPrefabs.Length == 0) return;

        List<FoodEntry> migrated = new List<FoodEntry>();
        if (foodEntries != null) migrated.AddRange(foodEntries);

        foreach (GameObject prefab in foodPrefabs)
        {
            if (prefab == null) continue;
            migrated.Add(new FoodEntry { prefab = prefab, weight = 1f });
        }

        foodEntries = migrated.ToArray();
        foodPrefabs = new GameObject[0];

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}