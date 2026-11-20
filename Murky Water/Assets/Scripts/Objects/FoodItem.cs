using UnityEngine;

/// Food pickup. Healthy: recovers energy (GameBalanceConfig.foodEnergyPercent).
/// Contaminated: removes energy instead (energy-only hit, no life damage, no invulnerability).
/// Contact is detected by PlayerCollisionHandler, which looks for IPlayerInteractable on the touched object.
/// NOTE: PlayerCollisionHandler.ApplyFood already raises GameEvents.RaiseFoodConsumed,
/// so this script must NOT raise it again (it would be published twice).
/// Requirements: a Collider2D with Is Trigger enabled on this object.
[DisallowMultipleComponent]
public class FoodItem : MonoBehaviour, IPlayerInteractable
{
    [Header("Config")]
    [SerializeField] private GameBalanceConfig balanceConfig;

    [Header("Contamination")]
    [SerializeField] private bool startsContaminated;
    [Tooltip("Energy percentage points lost when eating contaminated food. -1 = use the same value as foodEnergyPercent.")]
    [SerializeField] private float contaminatedEnergyDamagePercent = -1f;

    [Header("Visuals (optional)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite healthySprite;
    [Tooltip("If empty, the contaminated state is shown with the tint below.")]
    [SerializeField] private Sprite contaminatedSprite;
    [SerializeField] private Color healthyTint = Color.white;
    [SerializeField] private Color contaminatedTint = new Color(0.55f, 0.4f, 0.2f, 1f);
    [SerializeField] private GameObject collectParticlesPrefab;

    private bool consumed;
    private bool contaminated;

    public bool IsContaminated => contaminated;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (balanceConfig == null)
            Debug.LogWarning("FoodItem: assign GameBalanceConfig.", this);
    }

    // Also runs when an ObjectPool re-activates the object, so the state is always reset.
    private void OnEnable()
    {
        consumed = false;
        SetContaminated(startsContaminated);
    }

    public void SetContaminated(bool value)
    {
        contaminated = value;
        if (spriteRenderer == null) return;

        if (contaminated && contaminatedSprite != null)
        {
            spriteRenderer.sprite = contaminatedSprite;
            spriteRenderer.color = Color.white;
        }
        else
        {
            if (healthySprite != null) spriteRenderer.sprite = healthySprite;
            spriteRenderer.color = contaminated ? contaminatedTint : healthyTint;
        }
    }

    public void OnPlayerContact(PlayerCollisionHandler player)
    {
        if (consumed || player == null) return;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

        consumed = true;

        if (contaminated) ApplyContaminatedEffect(player);
        else ApplyHealthyEffect(player);

        if (collectParticlesPrefab != null)
            Instantiate(collectParticlesPrefab, transform.position, Quaternion.identity);

        gameObject.SetActive(false);
    }

    private void ApplyHealthyEffect(PlayerCollisionHandler player)
    {
        float percent = balanceConfig != null ? balanceConfig.foodEnergyPercent : 20f;
        player.ApplyFood(percent); // raises OnFoodConsumed with the energy actually recovered
    }

    private void ApplyContaminatedEffect(PlayerCollisionHandler player)
    {
        float baseFood = balanceConfig != null ? balanceConfig.foodEnergyPercent : 20f;
        float damage = contaminatedEnergyDamagePercent >= 0f ? contaminatedEnergyDamagePercent : baseFood;

        // Energy-only hit: no life damage (so no invulnerability), no vision effect, not lethal.
        HitData hit = new HitData
        {
            isLethal = false,
            lifeDamagePercent = 0f,
            energyDamagePercent = damage,
            visionDuration = 0f,
            visionIntensity = 0f,
            source = HitSource.Obstacle
        };
        player.ApplyHit(hit);
    }
}
