using System.Collections.Generic;
using UnityEngine;

/// Fairness rules for random spawning (GDD section 12). It keeps the rectangles of the placed
/// obstacles and answers where something may be placed:
///  1. There is always a free vertical gap of at least minimumVerticalGapFishHeights x the fish height.
///  2. Obstacles keep at least minimumObstacleHorizontalSpacing between their edges
///     (ObstacleSpawner enforces it; here, obstacles closer than that count as one column).
///  3. Food never appears inside an obstacle, but it may appear right behind one (risk dilemma).
/// Values are read from GameBalanceConfig; the top and bottom come from PlayAreaBounds.
public class SpawnFairnessRules : MonoBehaviour
{
    [SerializeField] private GameBalanceConfig balanceConfig;
    [Tooltip("Fish collider, used to measure its height. Empty = it is searched for automatically.")]
    [SerializeField] private Collider2D playerCollider;

    [Header("Fallbacks (only if something is not assigned)")]
    [SerializeField, Min(0.1f)] private float fallbackFishHeight = 1f;
    [SerializeField] private float fallbackTopY = 4f;
    [SerializeField] private float fallbackBottomY = -4f;

    [Header("Fine tuning")]
    [Tooltip("Step in world units used to scan valid heights.")]
    [SerializeField, Min(0.05f)] private float scanStep = 0.25f;
    [Tooltip("Safety margin between food and obstacles.")]
    [SerializeField, Min(0f)] private float foodObstacleMargin = 0.3f;
    [Tooltip("Horizontal distance between an obstacle's edge and the food placed right behind it.")]
    [SerializeField, Min(0f)] private float behindObstacleGap = 0.6f;

    private readonly List<Rect> blocked = new List<Rect>();
    private readonly List<Vector2> intervals = new List<Vector2>();
    private readonly List<float> validValues = new List<float>();
    private readonly List<Rect> candidates = new List<Rect>();

    public float TopY => PlayAreaBounds.Instance != null ? PlayAreaBounds.Instance.TopY : fallbackTopY;
    public float BottomY => PlayAreaBounds.Instance != null ? PlayAreaBounds.Instance.BottomY : fallbackBottomY;
    public float FishHeight => playerCollider != null ? playerCollider.bounds.size.y : fallbackFishHeight;
    public float MinVerticalGap =>
        (balanceConfig != null ? balanceConfig.minimumVerticalGapFishHeights : 2.5f) * FishHeight;
    public float MinHorizontalSpacing =>
        balanceConfig != null ? balanceConfig.minimumObstacleHorizontalSpacing : 4f;

    private void Awake()
    {
        if (playerCollider == null)
        {
            PlayerController controller = FindAnyObjectByType<PlayerController>();
            if (controller != null) playerCollider = controller.GetComponent<Collider2D>();
        }

        if (balanceConfig == null)
            Debug.LogWarning("SpawnFairnessRules: assign GameBalanceConfig.", this);
    }

    public void RegisterBlocked(Rect rect) => blocked.Add(rect);
    public void Clear() => blocked.Clear();
    public void Prune(float minX) => blocked.RemoveAll(r => r.xMax < minX);

    /// Minimum distance between the CENTERS of two obstacles so that their edges are
    /// at least minimumObstacleHorizontalSpacing apart.
    public float MinCenterDistance(float widthA, float widthB)
    {
        return MinHorizontalSpacing + (widthA + widthB) * 0.5f;
    }

    /// True if, with this obstacle added, the fish still has a free vertical corridor.
    public bool IsGapValid(Rect candidate)
    {
        intervals.Clear();
        intervals.Add(new Vector2(candidate.yMin, candidate.yMax));

        foreach (Rect other in blocked)
        {
            if (HorizontalGap(other, candidate) < MinHorizontalSpacing)
                intervals.Add(new Vector2(other.yMin, other.yMax));
        }

        intervals.Sort((a, b) => a.x.CompareTo(b.x));

        float cursor = BottomY;
        float bestGap = 0f;
        foreach (Vector2 interval in intervals)
        {
            if (interval.y <= cursor) continue;
            bestGap = Mathf.Max(bestGap, interval.x - cursor);
            cursor = Mathf.Max(cursor, interval.y);
        }
        bestGap = Mathf.Max(bestGap, TopY - cursor);

        return bestGap >= MinVerticalGap - 0.001f;
    }

    /// Picks a random center height for an obstacle that keeps the free gap.
    public bool TryPickObstacleCenterY(System.Random rng, float centerX, Vector2 size, out float centerY)
    {
        centerY = 0f;
        float low = BottomY + size.y * 0.5f;
        float high = TopY - size.y * 0.5f;
        if (low > high) return false;

        validValues.Clear();
        for (float y = low; y <= high + 0.0001f; y += scanStep)
        {
            Rect rect = new Rect(centerX - size.x * 0.5f, y - size.y * 0.5f, size.x, size.y);
            if (IsGapValid(rect)) validValues.Add(y);
        }

        if (validValues.Count == 0) return false;
        centerY = validValues[rng.Next(validValues.Count)];
        return true;
    }

    /// Picks a random position for food at the given X that does not touch any obstacle.
    public bool TryPickFoodPosition(System.Random rng, float centerX, Vector2 size, out Vector2 position)
    {
        position = Vector2.zero;
        float low = BottomY + size.y * 0.5f;
        float high = TopY - size.y * 0.5f;
        if (low > high) return false;

        validValues.Clear();
        for (float y = low; y <= high + 0.0001f; y += scanStep)
        {
            Rect rect = new Rect(centerX - size.x * 0.5f, y - size.y * 0.5f, size.x, size.y);
            if (!OverlapsBlocked(rect, foodObstacleMargin)) validValues.Add(y);
        }

        if (validValues.Count == 0) return false;
        position = new Vector2(centerX, validValues[rng.Next(validValues.Count)]);
        return true;
    }

    /// Looks for an obstacle between foodX and foodX + searchRange and places the food right behind it
    /// (the fish has to cross the obstacle to reach it): the risk dilemma from the GDD.
    public bool TryPickFoodBehindObstacle(System.Random rng, float foodX, Vector2 size, float searchRange, out Vector2 position)
    {
        position = Vector2.zero;

        candidates.Clear();
        foreach (Rect other in blocked)
        {
            if (other.center.x >= foodX && other.center.x <= foodX + searchRange)
                candidates.Add(other);
        }
        if (candidates.Count == 0) return false;

        Rect obstacle = candidates[rng.Next(candidates.Count)];
        float x = obstacle.xMax + size.x * 0.5f + behindObstacleGap;
        float y = Mathf.Clamp(obstacle.center.y, BottomY + size.y * 0.5f, TopY - size.y * 0.5f);

        Rect rect = new Rect(x - size.x * 0.5f, y - size.y * 0.5f, size.x, size.y);
        if (OverlapsBlocked(rect, foodObstacleMargin)) return false;

        position = new Vector2(x, y);
        return true;
    }

    /// True if no registered obstacle is within "clearance" of the column [centerX +- halfWidth].
    public bool IsColumnFree(float centerX, float halfWidth, float clearance)
    {
        Rect column = new Rect(centerX - halfWidth, BottomY, halfWidth * 2f, Mathf.Max(0.01f, TopY - BottomY));
        foreach (Rect other in blocked)
            if (HorizontalGap(other, column) < clearance) return false;
        return true;
    }

    private bool OverlapsBlocked(Rect rect, float margin)
    {
        Rect inflated = new Rect(rect.xMin - margin, rect.yMin - margin,
            rect.width + margin * 2f, rect.height + margin * 2f);
        foreach (Rect other in blocked)
            if (other.Overlaps(inflated)) return true;
        return false;
    }

    private static float HorizontalGap(Rect a, Rect b)
    {
        return Mathf.Max(a.xMin - b.xMax, b.xMin - a.xMax);
    }

    // Select this object in the scene to see the registered obstacles (red) and the play area (cyan).
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        foreach (Rect rect in blocked)
            Gizmos.DrawWireCube(rect.center, rect.size);

        Camera cam = Camera.main;
        if (cam == null) return;
        Gizmos.color = Color.cyan;
        float left = cam.transform.position.x - 30f;
        float right = cam.transform.position.x + 30f;
        Gizmos.DrawLine(new Vector3(left, TopY, 0f), new Vector3(right, TopY, 0f));
        Gizmos.DrawLine(new Vector3(left, BottomY, 0f), new Vector3(right, BottomY, 0f));
    }
}
