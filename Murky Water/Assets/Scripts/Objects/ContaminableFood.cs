using UnityEngine;

/// Level 2. Food that can become contaminated when a pollutant touches it.
/// Works with FoodItem (same GameObject). It does not depend on FallingDebris:
/// any trigger/collider on one of the "contaminantLayers" contaminates the food.
/// Erlys can also call Contaminate() directly from the debris code.
/// Requirements: Collider2D (trigger) on this object; the debris needs a Collider2D and a Rigidbody2D.
[RequireComponent(typeof(FoodItem))]
public class ContaminableFood : MonoBehaviour
{
    [SerializeField] private FoodItem food;
    [Tooltip("Layers whose objects contaminate this food (e.g., a 'Debris' layer).")]
    [SerializeField] private LayerMask contaminantLayers;

    [Header("Warning feedback (optional)")]
    [SerializeField] private GameObject contaminationEffectPrefab;

    public bool IsContaminated => food != null && food.IsContaminated;

    private void Awake()
    {
        if (food == null) food = GetComponent<FoodItem>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if ((contaminantLayers.value & (1 << other.gameObject.layer)) == 0) return;
        Contaminate();
    }

    public void Contaminate()
    {
        if (food == null || food.IsContaminated) return;
        food.SetContaminated(true);

        if (contaminationEffectPrefab != null)
            Instantiate(contaminationEffectPrefab, transform.position, Quaternion.identity);
    }
}
