using UnityEngine;

/// Publishes the distance to the predator and whether it is within the danger threshold.
public class PredatorThreatMonitor : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform predator;
    [SerializeField] private GameBalanceConfig balanceConfig;
    [SerializeField, Min(0.01f)] private float publishDistanceDelta = 0.05f;

    private float lastPublishedDistance = -1f;
    private bool lastDangerState;

    private void Awake()
    {
        if (player == null)
        {
            PlayerController controller = FindAnyObjectByType<PlayerController>();
            if (controller != null) player = controller.transform;
        }

        if (predator == null)
        {
            PredatorController controller = FindAnyObjectByType<PredatorController>();
            if (controller != null) predator = controller.transform;
        }

        if (balanceConfig == null)
            Debug.LogWarning("PredatorThreatMonitor: assign GameBalanceConfig.", this);
    }

    private void Update()
    {
        if (player == null || predator == null) return;

        float distance = Mathf.Max(0f, player.position.x - predator.position.x);
        if (lastPublishedDistance < 0f || Mathf.Abs(distance - lastPublishedDistance) >= publishDistanceDelta)
        {
            lastPublishedDistance = distance;
            GameEvents.RaisePredatorDistanceChanged(distance);
        }

        float dangerThreshold = balanceConfig != null ? balanceConfig.predatorDangerDistance : 5f;
        bool danger = distance <= dangerThreshold;
        if (danger != lastDangerState)
        {
            lastDangerState = danger;
            GameEvents.RaisePredatorDangerChanged(danger);
        }
    }

    private void OnDisable()
    {
        // Prevents the interface from visually retaining a danger state when this monitor is disabled.
        if (lastDangerState) GameEvents.RaisePredatorDangerChanged(false);
        lastDangerState = false;
        lastPublishedDistance = -1f;
    }
}
