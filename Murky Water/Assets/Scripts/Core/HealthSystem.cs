using System.Collections;
using UnityEngine;

/// Manages health, damage, temporary invulnerability, and health reset.
public class HealthSystem : MonoBehaviour
{
    [SerializeField] private GameBalanceConfig balanceConfig;
    [SerializeField, Range(0f, 100f)] private float currentHealthPercent = 100f;

    private Coroutine invulnerabilityRoutine;
    private bool invulnerable;

    public float CurrentHealthPercent => currentHealthPercent;
    public float NormalizedHealth => currentHealthPercent / 100f;
    public bool IsDead => currentHealthPercent <= 0f;
    public bool IsInvulnerable => invulnerable;

    private void Awake()
    {
        currentHealthPercent = balanceConfig != null ? balanceConfig.startingHealthPercent : 100f;
        PublishHealth();
    }

    public bool ApplyDamagePercent(float percent, bool bypassInvulnerability = false)
    {
        if (percent <= 0f || IsDead || (invulnerable && !bypassInvulnerability)) return false;

        currentHealthPercent = Mathf.Clamp(currentHealthPercent - percent, 0f, 100f);
        PublishHealth();

        if (!IsDead && !bypassInvulnerability && balanceConfig != null && balanceConfig.invulnerabilityDuration > 0f)
            BeginInvulnerability();

        return true;
    }

    public void ResetHealth()
    {
        StopInvulnerability();
        currentHealthPercent = balanceConfig != null ? balanceConfig.startingHealthPercent : 100f;
        PublishHealth();
    }

    private void BeginInvulnerability()
    {
        if (invulnerabilityRoutine != null) StopCoroutine(invulnerabilityRoutine);
        invulnerabilityRoutine = StartCoroutine(InvulnerabilityTimer());
    }

    private IEnumerator InvulnerabilityTimer()
    {
        SetInvulnerable(true);
        yield return new WaitForSeconds(balanceConfig.invulnerabilityDuration);
        SetInvulnerable(false);
        invulnerabilityRoutine = null;
    }

    private void StopInvulnerability()
    {
        if (invulnerabilityRoutine != null) StopCoroutine(invulnerabilityRoutine);
        invulnerabilityRoutine = null;
        SetInvulnerable(false);
    }

    private void SetInvulnerable(bool value)
    {
        if (invulnerable == value) return;
        invulnerable = value;
        GameEvents.RaiseInvulnerabilityChanged(value);
    }

    private void PublishHealth() => GameEvents.RaiseHealthChanged(NormalizedHealth);
}
