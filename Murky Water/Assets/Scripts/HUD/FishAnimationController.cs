using System.Collections;
using UnityEngine;

/// <summary>
/// Manages the protagonist fish's visual states (normal, happy, tired, hurt, bitten, dead)
/// and handles the white invulnerability flash respecting ScreenFlashLimiter.
/// Also feeds the Animator with the vertical speed so it can choose Swim / SwimUp / SwimDown.
/// </summary>
public class FishAnimationController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private GameBalanceConfig balanceConfig;

    [Header("Components")]
    [SerializeField] private Animator fishAnimator;
    [SerializeField] private SpriteRenderer fishSpriteRenderer;
    [Tooltip("Rigidbody2D of the fish, used to measure vertical speed. Empty = searched in the parents.")]
    [SerializeField] private Rigidbody2D fishBody;

    [Header("Visual Feedback Settings")]
    [SerializeField] private Color invulnerabilityFlashColor = Color.white;
    [SerializeField] private float reactionDuration = 0.8f;

    [Header("Vertical Movement")]
    [Tooltip("How fast the vertical speed value follows the real movement (higher = snappier).")]
    [SerializeField, Min(0.1f)] private float verticalSmoothing = 15f;

    private Color originalColor;
    private bool isInvulnerable = false;
    private bool isTired = false;
    private bool isDead = false;
    private Coroutine flashCoroutine;
    private Coroutine stateResetCoroutine;

    private float lastY;
    private float smoothedVertical;

    // Animator parameter hashes (the text must match the Animator exactly, including capitals)
    private static readonly int AnimIsTired = Animator.StringToHash("IsTired");
    private static readonly int AnimTriggerEat = Animator.StringToHash("Eat");
    private static readonly int AnimTriggerHurt = Animator.StringToHash("Hurt");
    private static readonly int AnimTriggerBite = Animator.StringToHash("Bite");
    private static readonly int AnimTriggerDie = Animator.StringToHash("Die");
    private static readonly int AnimTriggerDiePredator = Animator.StringToHash("DiePredator");
    private static readonly int AnimVerticalSpeed = Animator.StringToHash("VerticalSpeed");

    // Animator state the fish returns to when a new run starts (must match the state name).
    private static readonly int SwimState = Animator.StringToHash("Swim");

    private void Awake()
    {
        if (fishSpriteRenderer != null)
        {
            originalColor = fishSpriteRenderer.color;
        }
        if (fishBody == null) fishBody = GetComponentInParent<Rigidbody2D>();
        lastY = fishBody != null ? fishBody.position.y : transform.position.y;
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerHit += HandlePlayerHit;
        GameEvents.OnInvulnerabilityChanged += HandleInvulnerabilityChanged;
        GameEvents.OnFoodConsumed += HandleFoodConsumed;
        GameEvents.OnEnergyChanged += HandleEnergyChanged;
        GameEvents.OnPredatorBite += HandlePredatorBite;
        GameEvents.OnPlayerDied += HandlePlayerDied;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerHit -= HandlePlayerHit;
        GameEvents.OnInvulnerabilityChanged -= HandleInvulnerabilityChanged;
        GameEvents.OnFoodConsumed -= HandleFoodConsumed;
        GameEvents.OnEnergyChanged -= HandleEnergyChanged;
        GameEvents.OnPredatorBite -= HandlePredatorBite;
        GameEvents.OnPlayerDied -= HandlePlayerDied;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
    }

    // Measured in FixedUpdate because PlayerController moves the fish there.
    private void FixedUpdate()
    {
        if (fishAnimator == null) return;

        float y = fishBody != null ? fishBody.position.y : transform.position.y;
        float dt = Time.fixedDeltaTime;
        float rawSpeed = (y - lastY) / dt;
        lastY = y;

        smoothedVertical = Mathf.Lerp(smoothedVertical, rawSpeed, 1f - Mathf.Exp(-verticalSmoothing * dt));
        fishAnimator.SetFloat(AnimVerticalSpeed, smoothedVertical);
    }

    private void HandleEnergyChanged(float normalizedEnergy)
    {
        if (balanceConfig == null) return;

        float threshold = balanceConfig.lowEnergyThreshold / 100f;
        bool tiredNow = normalizedEnergy <= threshold;

        if (tiredNow != isTired)
        {
            isTired = tiredNow;
            if (fishAnimator != null)
            {
                fishAnimator.SetBool(AnimIsTired, isTired);
            }
        }
    }

    private void HandleFoodConsumed(float energyAmount)
    {
        TriggerTemporaryState(AnimTriggerEat);
    }

    private void HandlePlayerHit(HitData hitData)
    {
        TriggerTemporaryState(AnimTriggerHurt);
    }

    private void HandlePredatorBite(int biteNumber)
    {
        TriggerTemporaryState(AnimTriggerBite);
    }

    private void TriggerTemporaryState(int triggerHash)
    {
        // Once dead, no other reaction may override the death animation.
        if (isDead || fishAnimator == null) return;

        fishAnimator.SetTrigger(triggerHash);

        if (stateResetCoroutine != null)
        {
            StopCoroutine(stateResetCoroutine);
        }
        stateResetCoroutine = StartCoroutine(ResetTemporaryStateRoutine());
    }

    private IEnumerator ResetTemporaryStateRoutine()
    {
        yield return new WaitForSeconds(reactionDuration);
        // Animator transitions automatically back to Normal or Tired based on IsTired bool
    }

    // Plays the predator death or the normal death depending on the cause.
    private void HandlePlayerDied(DeathCause cause, float progress)
    {
        isDead = true;

        // Stop the invulnerability flash and restore the sprite color.
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
        if (fishSpriteRenderer != null) fishSpriteRenderer.color = originalColor;

        if (fishAnimator == null) return;

        // Clear pending reactions so they do not override the death animation.
        fishAnimator.ResetTrigger(AnimTriggerEat);
        fishAnimator.ResetTrigger(AnimTriggerHurt);
        fishAnimator.ResetTrigger(AnimTriggerBite);

        // Health, Energy and Lethal use the normal death; Predator has its own.
        bool byPredator = cause == DeathCause.Predator;
        fishAnimator.SetTrigger(byPredator ? AnimTriggerDiePredator : AnimTriggerDie);
    }

    // A new run starts at Countdown: bring the fish back to its normal state.
    private void HandleGameStateChanged(GameState state)
    {
        if (state != GameState.Countdown || fishAnimator == null) return;

        isDead = false;
        fishAnimator.ResetTrigger(AnimTriggerDie);
        fishAnimator.ResetTrigger(AnimTriggerDiePredator);
        fishAnimator.Play(SwimState, 0, 0f);
    }

    private void HandleInvulnerabilityChanged(bool active)
    {
        // A dead fish does not flash.
        if (isDead && active) return;

        isInvulnerable = active;

        if (isInvulnerable)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(InvulnerabilityFlashRoutine());
        }
        else
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
            }
            if (fishSpriteRenderer != null)
            {
                fishSpriteRenderer.color = originalColor;
            }
        }
    }

    private IEnumerator InvulnerabilityFlashRoutine()
    {
        float flashesPerSec = (balanceConfig != null && balanceConfig.invulnerabilityFlashesPerSecond > 0f)
            ? balanceConfig.invulnerabilityFlashesPerSecond
            : 2f;

        float halfPeriod = (1f / flashesPerSec) * 0.5f;

        while (isInvulnerable)
        {
            bool canFlash = ScreenFlashLimiter.Instance == null || ScreenFlashLimiter.Instance.RequestFlash();

            if (canFlash && fishSpriteRenderer != null)
            {
                fishSpriteRenderer.color = invulnerabilityFlashColor;
                yield return new WaitForSeconds(halfPeriod);
                if (fishSpriteRenderer != null) fishSpriteRenderer.color = originalColor;
                yield return new WaitForSeconds(halfPeriod);
            }
            else
            {
                yield return new WaitForSeconds(halfPeriod);
            }
        }
    }
}