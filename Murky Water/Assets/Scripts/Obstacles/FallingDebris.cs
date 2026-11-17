using UnityEngine;

/// Level 2. Debris that falls from the surface and ends up resting on the seabed as an obstacle.
///  1. Warning (1 s by default): it waits at the top with an indicator and a disabled collider.
///     The indicator is static (it does not blink), so it adds nothing to the flash limit.
///  2. Falling: the collider is on; if it touches the fish it hits like any Obstacle.
///  3. Resting: it stays at the bottom as a normal obstacle until it is left behind the camera.
/// Prefab: Obstacle + FallingDebris + BoxCollider2D (trigger) + Rigidbody2D.
/// The Rigidbody2D is forced to Kinematic. Put the prefab on the layer that ContaminableFood
/// (Nevys) lists as a contaminant: falling debris contaminates the food it touches.
[RequireComponent(typeof(Obstacle))]
[RequireComponent(typeof(Rigidbody2D))]
public class FallingDebris : MonoBehaviour
{
    private enum State { Warning, Falling, Resting }

    [SerializeField] private Obstacle obstacle;
    [SerializeField] private Rigidbody2D body;
    [Tooltip("Child object shown during the warning (e.g., an '!' icon). Optional.")]
    [SerializeField] private GameObject warningIndicator;
    [Tooltip("When this far behind the camera's left edge, it goes back to the pool.")]
    [SerializeField, Min(0f)] private float cleanupMargin = 6f;

    private State state = State.Resting;
    private ObjectPool pool;
    private float warningTimer;
    private float fallSpeed;
    private float restY;
    private bool isPlaying = true;

    private void Awake()
    {
        if (obstacle == null) obstacle = GetComponent<Obstacle>();
        if (body == null) body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
    }

    private void OnEnable()
    {
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        isPlaying = GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        if (warningIndicator != null) warningIndicator.SetActive(false);
    }

    /// Starts the warning-fall-rest cycle. Call it right after Obstacle.Initialize.
    public void Begin(ObjectPool ownerPool, float warningSeconds, float speed, float restHeightY)
    {
        pool = ownerPool;
        warningTimer = Mathf.Max(0f, warningSeconds);
        fallSpeed = Mathf.Max(0.1f, speed);
        restY = restHeightY;

        state = State.Warning;
        obstacle.SetCollisionEnabled(false);
        if (warningIndicator != null) warningIndicator.SetActive(true);
    }

    private void Update()
    {
        if (!isPlaying) return;

        if (state == State.Warning)
        {
            warningTimer -= Time.deltaTime;
            if (warningTimer <= 0f)
            {
                state = State.Falling;
                obstacle.SetCollisionEnabled(true);
                if (warningIndicator != null) warningIndicator.SetActive(false);
            }
        }

        DespawnIfBehindCamera();
    }

    private void FixedUpdate()
    {
        if (!isPlaying || state != State.Falling) return;

        Vector2 position = body.position;
        position.y = Mathf.MoveTowards(position.y, restY, fallSpeed * Time.fixedDeltaTime);
        body.MovePosition(position);

        if (Mathf.Approximately(position.y, restY)) state = State.Resting;
    }

    private void DespawnIfBehindCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float depth = Mathf.Abs(cam.transform.position.z - transform.position.z);
        float leftEdge = cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, depth)).x;
        if (transform.position.x >= leftEdge - cleanupMargin) return;

        if (pool != null) pool.Release(gameObject);
        else gameObject.SetActive(false);
    }

    private void HandleGameStateChanged(GameState newState)
    {
        isPlaying = newState == GameState.Playing;
    }
}
