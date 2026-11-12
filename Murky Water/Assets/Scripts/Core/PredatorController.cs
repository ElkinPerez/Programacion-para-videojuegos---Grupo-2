using System.Collections;
using UnityEngine;


/// Manages predator pursuit. Distance is interpreted as the horizontal separation
/// relative to the fish, allowing phase speed to control how quickly it approaches.
/// Each bite applies the damage defined in GameBalanceConfig and then retreats.

public class PredatorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerCollisionHandler playerCollision;
    [SerializeField] private GameBalanceConfig balanceConfig;
    [SerializeField] private PhaseConfig startingPhase;

    [Header("Optional Settings")]
    [SerializeField, Min(0f)] private float verticalFollowSpeed = 3f;

    [Header("Bite Point")]
    [SerializeField] private float mouthOffsetX = 1.5f;
    [SerializeField] private float biteDistance = 1f;

    private PhaseConfig currentPhase;
    private float distanceToPlayer;
    private int biteCount;
    private bool isPlaying;
    private bool retreating;
    private Coroutine retreatRoutine;

    public float DistanceToPlayer => distanceToPlayer;
    public int BiteCount => biteCount;

    private void Awake()
    {
        if (player == null)
        {
            PlayerController controller = FindAnyObjectByType<PlayerController>();
            if (controller != null) player = controller.transform;
        }

        if (player != null && playerCollision == null)
            playerCollision = player.GetComponent<PlayerCollisionHandler>();

        currentPhase = startingPhase;
        distanceToPlayer = balanceConfig != null ? balanceConfig.predatorInitialDistance : 12f;

        if (player == null)
            Debug.LogError("PredatorController: assign the fish Transform in Player.", this);
        if (playerCollision == null)
            Debug.LogError("PredatorController: the fish requires PlayerCollisionHandler.", this);
        if (balanceConfig == null)
            Debug.LogWarning("PredatorController: assign GameBalanceConfig.", this);

        PlaceBehindPlayer();
    }

    private void OnEnable()
    {
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        GameEvents.OnPhaseChanged += HandlePhaseChanged;
        GameEvents.OnCountdownFinished += HandleCountdownFinished;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        GameEvents.OnPhaseChanged -= HandlePhaseChanged;
        GameEvents.OnCountdownFinished -= HandleCountdownFinished;
        if (retreatRoutine != null) StopCoroutine(retreatRoutine);
    }

    private void Update()
    {
        if (!isPlaying || player == null) return;

        if (!retreating)
        {
            float speed = currentPhase != null ? currentPhase.predatorSpeed : 3.5f;
            distanceToPlayer = Mathf.MoveTowards(distanceToPlayer, 0f, speed * Time.deltaTime);

            float distanceFromMouth = distanceToPlayer - mouthOffsetX;

            if (distanceFromMouth <= biteDistance)
            {
                Bite();
                return;
            }
        }

        PlaceBehindPlayer();
        Vector3 targetPosition = new Vector3(transform.position.x, player.position.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, verticalFollowSpeed * Time.deltaTime);
    }

    private void Bite()
    {
        if (retreating || !isPlaying) return;

        biteCount++;
        GameEvents.RaisePredatorBite(biteCount);

        float damage = balanceConfig != null ? balanceConfig.firstBiteLifeDamagePercent : 50f;
        HitData hit = new HitData
        {
            isLethal = false,
            lifeDamagePercent = damage,
            energyDamagePercent = 0f,
            visionDuration = 0f,
            visionIntensity = 0f,
            source = HitSource.Predator
        };

        if (playerCollision != null)
            playerCollision.ApplyHit(hit);

        float retreatDistance = balanceConfig != null ? balanceConfig.predatorRetreatDistance : 8f;
        float retreatDuration = balanceConfig != null ? balanceConfig.predatorRetreatDuration : 1f;
        retreatRoutine = StartCoroutine(RetreatRoutine(retreatDistance, retreatDuration));
    }

    private IEnumerator RetreatRoutine(float retreatDistance, float duration)
    {
        retreating = true;
        float startDistance = distanceToPlayer;
        float targetDistance = startDistance + retreatDistance;
        float elapsed = 0f;

        GameEvents.RaisePredatorRetreat();

        while (elapsed < duration)
        {
            if (!isPlaying) yield break;
            elapsed += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            distanceToPlayer = Mathf.Lerp(startDistance, targetDistance, t);
            PlaceBehindPlayer();
            Vector3 target = new Vector3(transform.position.x, player.position.y, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, target, verticalFollowSpeed * Time.deltaTime);
            yield return null;
        }

        distanceToPlayer = targetDistance;
        retreating = false;
        retreatRoutine = null;
        PlaceBehindPlayer();
    }

    private void PlaceBehindPlayer()
    {
        if (player == null) return;
        transform.position = new Vector3(player.position.x - distanceToPlayer, transform.position.y, transform.position.z);
    }

    private void HandleGameStateChanged(GameState state)
    {
        isPlaying = state == GameState.Playing;
        if (!isPlaying && retreatRoutine != null)
        {
            StopCoroutine(retreatRoutine);
            retreatRoutine = null;
            retreating = false;
        }
    }

    private void HandleCountdownFinished() => isPlaying = true;
    private void HandlePhaseChanged(PhaseConfig phase)
    {
        if (phase != null) currentPhase = phase;
    }

    public void ResetPredator()
    {
        if (retreatRoutine != null) StopCoroutine(retreatRoutine);
        retreatRoutine = null;
        retreating = false;
        biteCount = 0;
        distanceToPlayer = balanceConfig != null ? balanceConfig.predatorInitialDistance : 12f;
        PlaceBehindPlayer();
    }
}
