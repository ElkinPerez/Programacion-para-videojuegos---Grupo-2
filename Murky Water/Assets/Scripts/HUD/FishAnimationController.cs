using System.Collections;
using UnityEngine;

/// <summary>
/// Manages the protagonist fish's visual states (normal, happy, tired, hurt, bitten)
/// and handles the white invulnerability flash respecting ScreenFlashLimiter.
/// </summary>
public class FishAnimationController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private GameBalanceConfig balanceConfig;

    [Header("Components")]
    [SerializeField] private Animator fishAnimator;
    [SerializeField] private SpriteRenderer fishSpriteRenderer;

    [Header("Visual Feedback Settings")]
    [SerializeField] private Color invulnerabilityFlashColor = Color.white;
    [SerializeField] private float reactionDuration = 0.8f;

    private Color originalColor;
    private bool isInvulnerable = false;
    private bool isTired = false;
    private Coroutine flashCoroutine;
    private Coroutine stateResetCoroutine;

    // Animator parameter hashes
    private static readonly int AnimIsTired = Animator.StringToHash("IsTired");
    private static readonly int AnimTriggerHappy = Animator.StringToHash("Happy");
    private static readonly int AnimTriggerHurt = Animator.StringToHash("Hurt");
    private static readonly int AnimTriggerBite = Animator.StringToHash("Bite");

    private void Awake()
    {
        if (fishSpriteRenderer != null)
        {
            originalColor = fishSpriteRenderer.color;
        }
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerHit += HandlePlayerHit;
        GameEvents.OnInvulnerabilityChanged += HandleInvulnerabilityChanged;
        GameEvents.OnFoodConsumed += HandleFoodConsumed;
        GameEvents.OnEnergyChanged += HandleEnergyChanged;
        GameEvents.OnPredatorBite += HandlePredatorBite;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerHit -= HandlePlayerHit;
        GameEvents.OnInvulnerabilityChanged -= HandleInvulnerabilityChanged;
        GameEvents.OnFoodConsumed -= HandleFoodConsumed;
        GameEvents.OnEnergyChanged -= HandleEnergyChanged;
        GameEvents.OnPredatorBite -= HandlePredatorBite;
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
        TriggerTemporaryState(AnimTriggerHappy);
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
        if (fishAnimator == null) return;

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

    private void HandleInvulnerabilityChanged(bool active)
    {
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