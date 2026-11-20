using UnityEngine;

/// Obstacle behavior driven by an ObstacleData asset.
/// Can live on the root of an obstacle prefab OR on child parts (e.g., rope = Danger, hook = Lethal).
/// On contact it builds a HitData and sends it to PlayerCollisionHandler.ApplyHit once per activation.
[RequireComponent(typeof(Collider2D))]
public class Obstacle : MonoBehaviour, IPlayerInteractable
{
    [SerializeField] private GameBalanceConfig balanceConfig;
    [SerializeField] private ObstacleData data;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D hitbox;

    [Tooltip("GDD: invulnerability protects against the lethal obstacle. If ON, a lethal hit during " +
             "invulnerability is ignored (the obstacle is used up and the fish passes through).")]
    [SerializeField] private bool lethalRespectsInvulnerability = true;

    private bool hasHit;

    public ObstacleData Data => data;
    public bool HasHit => hasHit;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (hitbox == null) hitbox = GetComponent<Collider2D>();
        if (hitbox != null) hitbox.isTrigger = true;
        ApplyData();
    }

    // Also runs when the ObjectPool re-activates the object: it can hit again in its new life.
    private void OnEnable() => hasHit = false;

    /// Configures this instance with an obstacle of the catalog.
    public void Initialize(ObstacleData newData, GameBalanceConfig config = null)
    {
        if (newData != null) data = newData;
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
        if (hitbox != null && (data == null || data.prefab != null))
        {
            Bounds b = hitbox.bounds;
            return new Rect(b.min, b.size);
        }

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

        hasHit = true; // a single hit per obstacle/part, even if the fish keeps crossing it
        player.ApplyHit(hit);
    }

    private void ApplyData()
    {
        if (data == null) return;

        // When using a dedicated prefab, keep the prefab's own visuals, animator and collider setup.
        bool usesDedicatedPrefab = data.prefab != null;

        if (!usesDedicatedPrefab && spriteRenderer != null)
        {
            spriteRenderer.sprite = data.sprite;
            spriteRenderer.sortingOrder = data.sortingOrder;
        }

        if (hitbox != null)
        {
            if (!usesDedicatedPrefab && hitbox is BoxCollider2D box)
            {
                box.size = data.hitboxSize;
                box.offset = data.hitboxOffset;
            }
            hitbox.isTrigger = true;
            hitbox.enabled = true;
        }
    }
}