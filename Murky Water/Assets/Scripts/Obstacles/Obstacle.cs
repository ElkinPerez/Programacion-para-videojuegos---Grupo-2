using UnityEngine;

/// Single script for every obstacle. All its behavior comes from an ObstacleData asset.
/// On contact it builds a HitData and sends it to PlayerCollisionHandler.ApplyHit exactly once.
/// Contact detection: PlayerCollisionHandler calls IPlayerInteractable.OnPlayerContact
/// when the fish enters this object's collider (it is forced to be a trigger).
///
/// Prefab: SpriteRenderer + BoxCollider2D + Obstacle. ObstacleSpawner calls Initialize(...) after taking it from the pool.
[RequireComponent(typeof(BoxCollider2D))]
public class Obstacle : MonoBehaviour, IPlayerInteractable
{
    [SerializeField] private GameBalanceConfig balanceConfig;
    [SerializeField] private ObstacleData data;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private BoxCollider2D hitbox;

    [Tooltip("GDD: invulnerability protects against the lethal obstacle. If ON, a lethal hit during " +
             "invulnerability is ignored (the obstacle is used up and the fish passes through).")]
    [SerializeField] private bool lethalRespectsInvulnerability = true;

    private bool hasHit;

    public ObstacleData Data => data;
    public bool HasHit => hasHit;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (hitbox == null) hitbox = GetComponent<BoxCollider2D>();
        hitbox.isTrigger = true;
        ApplyData();
    }

    // Also runs when the ObjectPool re-activates the object: it can hit again in its new life.
    private void OnEnable() => hasHit = false;

    /// Configures this instance with an obstacle of the catalog.
    public void Initialize(ObstacleData newData, GameBalanceConfig config = null)
    {
        data = newData;
        if (config != null) balanceConfig = config;
        hasHit = false;
        ApplyData();
    }

    public void SetCollisionEnabled(bool value)
    {
        if (hitbox != null) hitbox.enabled = value;
    }

    /// World-space rectangle covered by the hitbox (assumes scale 1).
    public Rect GetFootprint()
    {
        Vector2 size = data != null ? data.hitboxSize : Vector2.one;
        Vector2 offset = data != null ? data.hitboxOffset : Vector2.zero;
        Vector2 center = (Vector2)transform.position + offset;
        return new Rect(center - size * 0.5f, size);
    }

    public void OnPlayerContact(PlayerCollisionHandler player)
    {
        if (hasHit || data == null || player == null) return;

        HitData hit = data.BuildHitData(balanceConfig);

        if (hit.isLethal && lethalRespectsInvulnerability &&
            player.Health != null && player.Health.IsInvulnerable)
        {
            hasHit = true; // consumed without effect
            return;
        }

        hasHit = true; // a single hit per obstacle, even if the fish keeps crossing it
        player.ApplyHit(hit);
    }

    private void ApplyData()
    {
        if (data == null) return;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = data.sprite;
            spriteRenderer.sortingOrder = data.sortingOrder;
        }

        if (hitbox != null)
        {
            hitbox.size = data.hitboxSize;
            hitbox.offset = data.hitboxOffset;
            hitbox.isTrigger = true;
            hitbox.enabled = true;
        }
    }
}
