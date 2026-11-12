using UnityEngine;

/// Automatic horizontal movement and vertical swimming controlled by PlayerInputReader.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private EnergySystem energySystem;
    [SerializeField] private GameBalanceConfig balanceConfig;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Collider2D playerCollider;

    private float verticalVelocity;
    private bool canMove;

    private void Awake()
    {
        if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
        if (energySystem == null) energySystem = GetComponent<EnergySystem>();
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (playerCollider == null) playerCollider = GetComponent<Collider2D>();
        if (balanceConfig == null) Debug.LogWarning("PlayerController: assign GameBalanceConfig.", this);
        body.gravityScale = 0f;
        body.freezeRotation = true;
    }

    private void OnEnable()
    {
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        GameEvents.OnCountdownFinished += HandleCountdownFinished;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        GameEvents.OnCountdownFinished -= HandleCountdownFinished;
    }

    private void FixedUpdate()
    {
        if (!canMove || balanceConfig == null || body == null) return;

        float energy = energySystem != null ? energySystem.NormalizedEnergy : 1f;
        float energyMultiplier = Mathf.Lerp(
            Mathf.Max(0.1f, balanceConfig.lowEnergySpeedMultiplier),
            1f,
            energy
        );
        float horizontal = balanceConfig.horizontalBaseSpeed * energyMultiplier;
        float verticalInput = inputReader != null ? inputReader.VerticalInput : 0f;
        float baseVerticalSpeed = verticalInput >= 0f
            ? balanceConfig.verticalUpBaseSpeed : balanceConfig.verticalDownBaseSpeed;
        float maxVerticalSpeed = verticalInput >= 0f
            ? balanceConfig.verticalUpMaxSpeed : balanceConfig.verticalDownMaxSpeed;
        float targetVertical = verticalInput * Mathf.Min(baseVerticalSpeed * energyMultiplier, maxVerticalSpeed * energyMultiplier);
        verticalVelocity = Mathf.MoveTowards(verticalVelocity, targetVertical, balanceConfig.verticalAcceleration * Time.fixedDeltaTime);

        Vector2 nextPosition = body.position + new Vector2(horizontal, verticalVelocity) * Time.fixedDeltaTime;
        ClampVerticalPosition(ref nextPosition);
        body.MovePosition(nextPosition);
    }

    public void SetMovementEnabled(bool enabled) => canMove = enabled;

    private void ClampVerticalPosition(ref Vector2 position)
    {
        if (PlayAreaBounds.Instance == null) return;
        float halfHeight = playerCollider != null ? playerCollider.bounds.extents.y : 0f;
        position.y = Mathf.Clamp(position.y,
            PlayAreaBounds.Instance.BottomY + halfHeight,
            PlayAreaBounds.Instance.TopY - halfHeight);
    }

    private void HandleGameStateChanged(GameState state)
    {
        canMove = state == GameState.Playing;
        if (!canMove) verticalVelocity = 0f;
    }

    private void HandleCountdownFinished() => canMove = true;
}
