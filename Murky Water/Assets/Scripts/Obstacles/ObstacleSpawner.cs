using System.Collections.Generic;
using UnityEngine;

/// Generates obstacles and food ahead of the camera following the zone's SpawnTable.
///  - Spawning is by DISTANCE, not time: density = obstaclesPerTwentyUnits (PhaseConfig).
///  - The table is chosen by the progress at the spawn position (zones change by distance and
///    spawning happens ahead of the fish). OnPhaseChanged is a fallback if no table covers a position.
///  - Starts with OnGameStateChanged(Playing)/OnCountdownFinished and stops on pause, win or loss.
///  - Position rules (free gap, spacing, food outside obstacles) go through SpawnFairnessRules.
///  - Fixed seed optional for repeatable playtests; in the final build every run is different.
public class ObstacleSpawner : MonoBehaviour
{
    private struct Spawned
    {
        public GameObject go;
        public float halfWidth;
    }

    [Header("References")]
    [SerializeField] private GameBalanceConfig balanceConfig;
    [SerializeField] private ObjectPool pool;
    [SerializeField] private SpawnFairnessRules rules;
    [SerializeField] private Transform player;
    [SerializeField] private Camera targetCamera;

    [Header("Prefabs and tables")]
    [Tooltip("Generic prefab with Obstacle (+ SpriteRenderer + BoxCollider2D).")]
    [SerializeField] private GameObject obstaclePrefab;
    [Tooltip("One SpawnTable per zone. Each one points to its PhaseConfig.")]
    [SerializeField] private SpawnTable[] tables = new SpawnTable[4];

    [Header("Layout")]
    [Tooltip("How far beyond the camera's right edge things are generated.")]
    [SerializeField, Min(0f)] private float spawnAheadMargin = 6f;
    [Tooltip("Obstacle-free distance at the start of the run.")]
    [SerializeField, Min(0f)] private float initialSafeDistance = 20f;
    [SerializeField, Min(0f)] private float firstFoodDistance = 15f;
    [Tooltip("Obstacle-free distance before the finish line.")]
    [SerializeField, Min(0f)] private float endClearDistance = 10f;
    [Tooltip("Random variation of the spacing (0.25 = +-25%).")]
    [SerializeField, Range(0f, 0.9f)] private float spacingJitter = 0.25f;
    [Tooltip("Things this far behind the camera's left edge go back to the pool.")]
    [SerializeField, Min(0f)] private float cleanupMargin = 6f;

    [Header("Food")]
    [SerializeField] private Vector2 foodSize = new Vector2(0.8f, 0.8f);
    [Tooltip("Probability that food is placed right behind an obstacle (risk dilemma).")]
    [SerializeField, Range(0f, 1f)] private float riskFoodChance = 0.35f;
    [SerializeField, Min(0f)] private float riskSearchRange = 6f;

    [Header("Randomness")]
    [SerializeField] private bool useFixedSeed;
    [SerializeField] private int seed = 12345;

    [Header("Pool warm-up")]
    [SerializeField, Min(0)] private int prewarmObstacles = 16;
    [SerializeField, Min(0)] private int prewarmFoodPerPrefab = 4;

    private readonly List<Spawned> active = new List<Spawned>();
    private SpawnTable tableFromEvent;
    private float runStartX;
    private float obstacleCursor;
    private float foodCursor;
    private float lastObstacleCenterX = -1e9f;
    private float lastObstacleWidth;
    private bool initialized;
    private bool isSpawning;

    public System.Random Rng { get; private set; }
    public ObjectPool Pool => pool;
    public SpawnFairnessRules Rules => rules;
    public bool IsSpawning => isSpawning && initialized;
    public float RunStartX => runStartX;
    public float TotalRunDistance => balanceConfig != null ? balanceConfig.totalRunDistance : 900f;

    public float CameraRightX => ViewportX(1f);
    public float CameraLeftX => ViewportX(0f);

    private void Awake()
    {
        if (player == null)
        {
            PlayerController controller = FindAnyObjectByType<PlayerController>();
            if (controller != null) player = controller.transform;
        }
        if (pool == null) pool = GetComponent<ObjectPool>();
        if (pool == null) pool = FindAnyObjectByType<ObjectPool>();
        if (rules == null) rules = GetComponent<SpawnFairnessRules>();
        if (rules == null) rules = FindAnyObjectByType<SpawnFairnessRules>();
        if (targetCamera == null) targetCamera = Camera.main;

        Rng = new System.Random(useFixedSeed ? seed : System.Guid.NewGuid().GetHashCode());

        if (balanceConfig == null) Debug.LogWarning("ObstacleSpawner: assign GameBalanceConfig.", this);
        if (pool == null) Debug.LogError("ObstacleSpawner: an ObjectPool is required.", this);
        if (rules == null) Debug.LogError("ObstacleSpawner: a SpawnFairnessRules is required.", this);
        if (obstaclePrefab == null) Debug.LogError("ObstacleSpawner: assign the obstacle prefab.", this);

        Prewarm();
    }

    private void OnEnable()
    {
        GameEvents.OnPhaseChanged += HandlePhaseChanged;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        GameEvents.OnCountdownFinished += HandleCountdownFinished;
    }

    private void OnDisable()
    {
        GameEvents.OnPhaseChanged -= HandlePhaseChanged;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        GameEvents.OnCountdownFinished -= HandleCountdownFinished;
    }

    private void Update()
    {
        if (!IsSpawning || pool == null || rules == null) return;

        float horizon = CameraRightX + spawnAheadMargin;
        float obstacleEndX = runStartX + TotalRunDistance - endClearDistance;
        float foodEndX = runStartX + TotalRunDistance;

        SpawnObstaclesUpTo(horizon, obstacleEndX);
        SpawnFoodUpTo(horizon, foodEndX);
        Cleanup();
    }

    // ---------- Public API (used by SurfaceLitterSpawner) ----------

    public float ProgressAtX(float x)
    {
        return Mathf.Clamp01((x - runStartX) / Mathf.Max(1f, TotalRunDistance));
    }

    public SpawnTable GetTableAtProgress(float progress)
    {
        SpawnTable bestEarlier = null;
        SpawnTable firstValid = null;

        foreach (SpawnTable table in tables)
        {
            if (table == null) continue;
            if (firstValid == null) firstValid = table;
            if (table.phase == null) continue;

            PhaseConfig phase = table.phase;
            bool inside = progress >= phase.startProgress &&
                          (progress < phase.endProgress || (progress >= 1f && phase.endProgress >= 1f));
            if (inside) return table;

            if (phase.startProgress <= progress &&
                (bestEarlier == null || phase.startProgress >= bestEarlier.phase.startProgress))
                bestEarlier = table;
        }

        if (bestEarlier != null) return bestEarlier;
        if (tableFromEvent != null) return tableFromEvent;
        return firstValid;
    }

    // ---------- Events ----------

    private void HandlePhaseChanged(PhaseConfig phase)
    {
        if (phase == null) return;
        foreach (SpawnTable table in tables)
        {
            if (table != null && table.phase != null && table.phase.phaseNumber == phase.phaseNumber)
            {
                tableFromEvent = table;
                return;
            }
        }
    }

    private void HandleGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Countdown:
                ResetSpawner();
                isSpawning = false;
                break;
            case GameState.Playing:
                EnsureInitialized();
                isSpawning = true;
                break;
            default: // Menu, Paused, Won, Lost
                isSpawning = false;
                break;
        }
    }

    private void HandleCountdownFinished()
    {
        EnsureInitialized();
        isSpawning = true;
    }

    // ---------- Spawning ----------

    private void EnsureInitialized()
    {
        if (!initialized) ResetSpawner();
    }

    private void ResetSpawner()
    {
        ReleaseActive();
        if (rules != null) rules.Clear();
        if (useFixedSeed) Rng = new System.Random(seed);

        runStartX = player != null ? player.position.x : 0f;
        obstacleCursor = runStartX + initialSafeDistance;
        foodCursor = runStartX + firstFoodDistance;
        lastObstacleCenterX = -1e9f;
        lastObstacleWidth = 0f;
        initialized = true;
    }

    private void SpawnObstaclesUpTo(float horizon, float endX)
    {
        int guard = 0;
        while (obstacleCursor <= horizon && obstacleCursor <= endX && guard++ < 64)
        {
            SpawnTable table = GetTableAtProgress(ProgressAtX(obstacleCursor));
            if (table == null)
            {
                obstacleCursor += 20f;
                continue;
            }

            int perTwentyUnits = table.phase != null ? table.phase.obstaclesPerTwentyUnits : 1;
            if (perTwentyUnits <= 0)
            {
                obstacleCursor += 20f; // this zone has no obstacles
                continue;
            }

            float averageSpacing = 20f / perTwentyUnits;
            ObstacleData data = table.PickObstacle(Rng);
            if (data == null)
            {
                obstacleCursor += averageSpacing;
                continue;
            }

            float width = data.hitboxSize.x;
            float minCenterX = lastObstacleCenterX + rules.MinCenterDistance(lastObstacleWidth, width);
            float centerX = Mathf.Max(obstacleCursor, minCenterX);
            if (centerX > endX) break;

            if (TrySpawnObstacle(data, centerX))
            {
                lastObstacleCenterX = centerX;
                lastObstacleWidth = width;
            }

            obstacleCursor = centerX + averageSpacing * Jitter();
        }
    }

    private bool TrySpawnObstacle(ObstacleData data, float centerX)
    {
        if (obstaclePrefab == null) return false;

        Vector2 size = data.hitboxSize;
        if (!rules.TryPickObstacleCenterY(Rng, centerX, size, out float centerY))
        {
            Debug.LogWarning($"ObstacleSpawner: '{data.displayName}' does not fit while keeping the free gap " +
                             "(it is too tall for the play area). It was skipped.", this);
            return false;
        }

        Vector3 position = new Vector3(centerX - data.hitboxOffset.x, centerY - data.hitboxOffset.y,
            obstaclePrefab.transform.position.z);

        GameObject instance = pool.Get(obstaclePrefab, position, Quaternion.identity);
        if (instance == null) return false;

        Obstacle obstacle = instance.GetComponent<Obstacle>();
        if (obstacle == null)
        {
            Debug.LogError("ObstacleSpawner: the obstacle prefab needs the Obstacle component.", this);
            pool.Release(instance);
            return false;
        }

        obstacle.Initialize(data, balanceConfig);
        rules.RegisterBlocked(new Rect(centerX - size.x * 0.5f, centerY - size.y * 0.5f, size.x, size.y));
        active.Add(new Spawned { go = instance, halfWidth = size.x * 0.5f });
        return true;
    }

    private void SpawnFoodUpTo(float horizon, float endX)
    {
        int guard = 0;
        while (foodCursor <= horizon && foodCursor <= endX && guard++ < 64)
        {
            SpawnTable table = GetTableAtProgress(ProgressAtX(foodCursor));
            float spacing = table != null ? table.GetFoodSpacing(balanceConfig) : 0f;
            if (table == null || spacing <= 0f)
            {
                foodCursor += 20f;
                continue;
            }

            GameObject prefab = table.PickFood(Rng);
            if (prefab != null)
            {
                Vector2 position = Vector2.zero;
                bool found = false;

                if (Rng.NextDouble() < riskFoodChance)
                    found = rules.TryPickFoodBehindObstacle(Rng, foodCursor, foodSize, riskSearchRange, out position);
                if (!found)
                    found = rules.TryPickFoodPosition(Rng, foodCursor, foodSize, out position);

                if (found) SpawnFood(prefab, position);
            }

            foodCursor += spacing * Jitter();
        }
    }

    private void SpawnFood(GameObject prefab, Vector2 position)
    {
        Vector3 worldPosition = new Vector3(position.x, position.y, prefab.transform.position.z);
        GameObject instance = pool.Get(prefab, worldPosition, Quaternion.identity);
        if (instance == null) return;
        active.Add(new Spawned { go = instance, halfWidth = foodSize.x * 0.5f });
    }

    // ---------- Cleanup ----------

    private void Cleanup()
    {
        float limit = CameraLeftX - cleanupMargin;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            Spawned item = active[i];
            if (item.go == null || !item.go.activeSelf)
            {
                active.RemoveAt(i); // destroyed, or deactivated by itself (eaten food)
                continue;
            }

            if (item.go.transform.position.x + item.halfWidth < limit)
            {
                pool.Release(item.go);
                active.RemoveAt(i);
            }
        }

        rules.Prune(limit - 10f);
    }

    private void ReleaseActive()
    {
        foreach (Spawned item in active)
            if (item.go != null && pool != null) pool.Release(item.go);
        active.Clear();
    }

    private void Prewarm()
    {
        if (pool == null) return;
        pool.Prewarm(obstaclePrefab, prewarmObstacles);
        foreach (SpawnTable table in tables)
        {
            if (table == null || table.foodPrefabs == null) continue;
            foreach (GameObject food in table.foodPrefabs)
                pool.Prewarm(food, prewarmFoodPerPrefab);
        }
    }

    // ---------- Helpers ----------

    private float Jitter()
    {
        return 1f + ((float)Rng.NextDouble() * 2f - 1f) * spacingJitter;
    }

    private float ViewportX(float viewportX)
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return player != null ? player.position.x : 0f;
        float depth = Mathf.Abs(cam.transform.position.z - (player != null ? player.position.z : 0f));
        return cam.ViewportToWorldPoint(new Vector3(viewportX, 0.5f, depth)).x;
    }
}
