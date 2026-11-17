using System.Collections.Generic;
using UnityEngine;

/// Level 2. Generates debris that falls from the surface, with a 1 s warning before the fall.
/// It uses ObstacleSpawner's pool, rules, random generator and zone tables:
/// each zone's SpawnTable says whether it allows litter, how much, and which debris.
/// Debris is only placed in columns that are free of obstacles (columnClearance), so the
/// fish never gets an unavoidable combination of a static obstacle and falling debris.
public class SurfaceLitterSpawner : MonoBehaviour
{
    [SerializeField] private ObstacleSpawner spawner;
    [Tooltip("Prefab with Obstacle + FallingDebris + BoxCollider2D (trigger) + Rigidbody2D.")]
    [SerializeField] private GameObject debrisPrefab;

    [Header("Timing")]
    [Tooltip("GDD: the debris shows a 1 second warning before falling.")]
    [SerializeField, Min(0f)] private float warningSeconds = 1f;
    [SerializeField, Min(0.1f)] private float fallSpeed = 6f;

    [Header("Layout")]
    [Tooltip("Litter-free distance at the start of the run.")]
    [SerializeField, Min(0f)] private float initialSafeDistance = 40f;
    [Tooltip("Debris appears when its X is this far inside the camera's right edge.")]
    [SerializeField, Min(0f)] private float insetFromRightEdge = 3f;
    [Tooltip("Minimum horizontal distance between debris and any obstacle.")]
    [SerializeField, Min(0f)] private float columnClearance = 2f;
    [SerializeField, Range(0f, 0.9f)] private float spacingJitter = 0.25f;

    private readonly List<GameObject> activeDebris = new List<GameObject>();
    private float cursor;
    private bool cursorReady;

    private void Awake()
    {
        if (spawner == null) spawner = FindAnyObjectByType<ObstacleSpawner>();
        if (spawner == null) Debug.LogError("SurfaceLitterSpawner: assign the ObstacleSpawner.", this);
        if (debrisPrefab == null) Debug.LogError("SurfaceLitterSpawner: assign the debris prefab.", this);
    }

    private void OnEnable() => GameEvents.OnGameStateChanged += HandleGameStateChanged;
    private void OnDisable() => GameEvents.OnGameStateChanged -= HandleGameStateChanged;

    private void Update()
    {
        if (spawner == null || debrisPrefab == null || !spawner.IsSpawning) return;

        if (!cursorReady)
        {
            cursor = spawner.RunStartX + initialSafeDistance;
            cursorReady = true;
        }

        float limit = spawner.CameraRightX - insetFromRightEdge;
        int guard = 0;

        while (cursor <= limit && guard++ < 16)
        {
            SpawnTable table = spawner.GetTableAtProgress(spawner.ProgressAtX(cursor));
            bool allowed = table != null && table.allowSurfaceLitter && table.litterPerTwentyUnits > 0f;
            if (!allowed)
            {
                cursor += 20f;
                continue;
            }

            float averageSpacing = 20f / table.litterPerTwentyUnits;
            ObstacleData data = table.PickLitter(spawner.Rng);
            if (data != null) TrySpawn(data, cursor);

            float jitter = 1f + ((float)spawner.Rng.NextDouble() * 2f - 1f) * spacingJitter;
            cursor += averageSpacing * jitter;
        }

        // Forget debris that was returned to the pool.
        activeDebris.RemoveAll(item => item == null || !item.activeSelf);
    }

    private bool TrySpawn(ObstacleData data, float x)
    {
        SpawnFairnessRules rules = spawner.Rules;
        ObjectPool pool = spawner.Pool;
        if (rules == null || pool == null) return false;

        // Look for a column free of obstacles, shifting forward a few times if needed.
        float halfWidth = data.hitboxSize.x * 0.5f;
        float centerX = x;
        bool free = false;
        for (int attempt = 0; attempt < 4; attempt++)
        {
            if (rules.IsColumnFree(centerX, halfWidth, columnClearance))
            {
                free = true;
                break;
            }
            centerX += columnClearance;
        }
        if (!free) return false;

        float startCenterY = rules.TopY - data.hitboxSize.y * 0.5f;
        float restCenterY = rules.BottomY + data.hitboxSize.y * 0.5f;
        Vector3 position = new Vector3(centerX - data.hitboxOffset.x, startCenterY - data.hitboxOffset.y,
            debrisPrefab.transform.position.z);

        GameObject instance = pool.Get(debrisPrefab, position, Quaternion.identity);
        if (instance == null) return false;

        Obstacle obstacle = instance.GetComponent<Obstacle>();
        FallingDebris debris = instance.GetComponent<FallingDebris>();
        if (obstacle == null || debris == null)
        {
            Debug.LogError("SurfaceLitterSpawner: the debris prefab needs Obstacle and FallingDebris.", this);
            pool.Release(instance);
            return false;
        }

        obstacle.Initialize(data);
        debris.Begin(pool, warningSeconds, fallSpeed, restCenterY - data.hitboxOffset.y);

        // Where it will end up resting, so later placements respect it.
        rules.RegisterBlocked(new Rect(centerX - halfWidth, restCenterY - data.hitboxSize.y * 0.5f,
            data.hitboxSize.x, data.hitboxSize.y));
        activeDebris.Add(instance);
        return true;
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (state != GameState.Countdown) return;

        // New run: forget the cursor and clear the litter.
        cursorReady = false;
        ObjectPool pool = spawner != null ? spawner.Pool : null;
        foreach (GameObject item in activeDebris)
            if (item != null && pool != null) pool.Release(item);
        activeDebris.Clear();
    }
}
