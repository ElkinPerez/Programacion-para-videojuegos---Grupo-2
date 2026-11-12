using UnityEngine;

/// Single entry point for obstacle/predator hits and food consumption.
public class PlayerCollisionHandler : MonoBehaviour
{
    [SerializeField] private HealthSystem healthSystem;
    [SerializeField] private EnergySystem energySystem;

    public HealthSystem Health => healthSystem;
    public EnergySystem Energy => energySystem;

    private void Awake()
    {
        if (healthSystem == null) healthSystem = GetComponent<HealthSystem>();
        if (energySystem == null) energySystem = GetComponent<EnergySystem>();
        if (healthSystem == null) Debug.LogError("PlayerCollisionHandler requires HealthSystem on the player.", this);
        if (energySystem == null) Debug.LogError("PlayerCollisionHandler requires EnergySystem on the player.", this);
    }

    public void ApplyHit(HitData hit)
    {
        GameEvents.RaisePlayerHit(hit);

        if (hit.visionDuration > 0f)
            GameEvents.RaiseVisionObstructed(hit.visionDuration, hit.visionIntensity);

        if (hit.isLethal)
        {
            if (GameManager.Instance != null) GameManager.Instance.RegisterDeath(DeathCause.Lethal);
            if (healthSystem != null) healthSystem.ApplyDamagePercent(100f, true);
            return;
        }

        if (healthSystem != null && hit.lifeDamagePercent > 0f)
            healthSystem.ApplyDamagePercent(hit.lifeDamagePercent);
        if (energySystem != null && hit.energyDamagePercent > 0f)
            energySystem.ApplyDamagePercent(hit.energyDamagePercent);
    }

    /// Receives an energy recovery percentage, expressed as 20 = 20%.
    public void ApplyFood(float porcentaje)
    {
        if (energySystem == null || porcentaje <= 0f) return;
        float before = energySystem.CurrentEnergyPercent;
        energySystem.RecoverPercent(porcentaje);
        float recovered = energySystem.CurrentEnergyPercent - before;
        if (recovered > 0f) GameEvents.RaiseFoodConsumed(recovered);
    }

    private void OnTriggerEnter2D(Collider2D other) => TryInteract(other);
    private void OnCollisionEnter2D(Collision2D collision) => TryInteract(collision.collider);

    private void TryInteract(Collider2D other)
    {
        if (other == null) return;
        MonoBehaviour[] behaviours = other.GetComponentsInParent<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IPlayerInteractable interactable)
            {
                interactable.OnPlayerContact(this);
                return;
            }
        }
    }
}
