using System;
using System.Reflection;
using UnityEngine;

/// Level 2. Short camera shake when the fish takes damage (GameBalanceConfig.cameraShakeDuration, 0.2 s).
/// Can be disabled by the player through AccessibilitySettings.Instance.CameraShakeEnabled (Jose's script).
/// That class is read by reflection so this script compiles even before Jose delivers it;
/// until it exists, "shakeEnabledFallback" is used.
///
/// The offset is removed in Update and re-applied in LateUpdate (after CameraFollowController),
/// so the follow smoothing never "learns" the shake offset.
[DefaultExecutionOrder(200)]
[RequireComponent(typeof(Camera))]
public class CameraShake : MonoBehaviour
{
    [SerializeField] private GameBalanceConfig balanceConfig;
    [SerializeField, Min(0f)] private float amplitude = 0.15f;
    [Tooltip("Used only while AccessibilitySettings does not exist in the project.")]
    [SerializeField] private bool shakeEnabledFallback = true;

    private float remaining;
    private float total;
    private Vector3 appliedOffset;

    // Cached reflection lookups for AccessibilitySettings.
    private static bool lookupDone;
    private static MemberInfo instanceMember;
    private static MemberInfo enabledMember;

    private void Awake()
    {
        if (balanceConfig == null)
            Debug.LogWarning("CameraShake: assign GameBalanceConfig.", this);
    }

    private void OnEnable() => GameEvents.OnPlayerHit += HandlePlayerHit;

    private void OnDisable()
    {
        GameEvents.OnPlayerHit -= HandlePlayerHit;
        RemoveOffset();
        remaining = 0f;
    }

    private void HandlePlayerHit(HitData hit)
    {
        // Ignore hits that do nothing (no damage at all).
        if (!hit.isLethal && hit.lifeDamagePercent <= 0f && hit.energyDamagePercent <= 0f) return;
        if (!IsShakeAllowed()) return;

        total = balanceConfig != null ? balanceConfig.cameraShakeDuration : 0.2f;
        if (total <= 0f) return;
        remaining = total;
    }

    private void Update() => RemoveOffset();

    private void LateUpdate()
    {
        if (remaining <= 0f) return;

        remaining -= Time.deltaTime;
        float strength = Mathf.Clamp01(remaining / total);
        Vector2 random = UnityEngine.Random.insideUnitCircle * amplitude * strength;
        appliedOffset = new Vector3(random.x, random.y, 0f);
        transform.position += appliedOffset;
    }

    private void RemoveOffset()
    {
        if (appliedOffset == Vector3.zero) return;
        transform.position -= appliedOffset;
        appliedOffset = Vector3.zero;
    }

    private bool IsShakeAllowed()
    {
        if (!lookupDone)
        {
            lookupDone = true;
            Type type = Type.GetType("AccessibilitySettings");
            if (type != null)
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
                instanceMember = (MemberInfo)type.GetProperty("Instance", flags) ?? type.GetField("Instance", flags);
                enabledMember = (MemberInfo)type.GetProperty("CameraShakeEnabled", flags) ?? type.GetField("CameraShakeEnabled", flags);
            }
        }

        if (instanceMember == null || enabledMember == null) return shakeEnabledFallback;

        object instance = ReadMember(instanceMember, null);
        if (instance == null) return shakeEnabledFallback;

        object value = ReadMember(enabledMember, instance);
        return value is bool enabled ? enabled : shakeEnabledFallback;
    }

    private static object ReadMember(MemberInfo member, object target)
    {
        if (member is PropertyInfo property) return property.GetValue(target);
        if (member is FieldInfo field) return field.GetValue(target);
        return null;
    }
}
