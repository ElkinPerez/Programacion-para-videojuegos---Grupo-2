using System.Collections;
using UnityEngine;

/// Gradually blends backgrounds, tint color and particles when the pollution zone changes.
/// Listens to GameEvents.OnPhaseChanged and matches each zone through PhaseConfig.phaseNumber.
/// The blend lasts blendSeconds (GDD: about 5 s). The first phase received is applied instantly.
///
/// Setup: one ZoneVisual per zone. Put each zone's background SpriteRenderers on a higher
/// Sorting Order than the previous zone so the crossfade reads correctly.
public class EnvironmentVisualController : MonoBehaviour
{
    [System.Serializable]
    public class ZoneVisual
    {
        public string label = "Zone";
        [Min(1)] public int phaseNumber = 1;
        [Tooltip("Background sprites of this zone; their alpha is blended.")]
        public SpriteRenderer[] backgroundLayers;
        [Tooltip("Water tint. The alpha channel controls how strong the tint is.")]
        public Color waterTint = new Color(0f, 0f, 0f, 0f);
        public Color cameraBackground = new Color(0.05f, 0.2f, 0.4f, 1f);
        [Min(0f)] public float particleRate = 5f;
    }

    [SerializeField] private ZoneVisual[] zones = new ZoneVisual[4];
    [SerializeField, Min(0f)] private float blendSeconds = 5f;

    [Header("Optional targets")]
    [Tooltip("Large sprite in front of the scene (child of the camera) used as water tint.")]
    [SerializeField] private SpriteRenderer tintOverlay;
    [Tooltip("The camera needs Clear Flags = Solid Color for the background color to show.")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private ParticleSystem[] pollutionParticles;

    private float[] weights;
    private Coroutine blendRoutine;
    private int targetIndex = -1;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        weights = new float[zones != null ? zones.Length : 0];
    }

    private void OnEnable() => GameEvents.OnPhaseChanged += HandlePhaseChanged;

    private void OnDisable()
    {
        GameEvents.OnPhaseChanged -= HandlePhaseChanged;
        if (blendRoutine != null) StopCoroutine(blendRoutine);
        blendRoutine = null;
    }

    private void HandlePhaseChanged(PhaseConfig phase)
    {
        if (phase == null || zones == null || zones.Length == 0) return;

        int index = FindZoneIndex(phase.phaseNumber);
        if (index < 0)
        {
            Debug.LogWarning($"EnvironmentVisualController: no zone for phase number {phase.phaseNumber}.", this);
            return;
        }
        if (index == targetIndex) return;

        bool firstPhase = targetIndex < 0;
        targetIndex = index;

        if (blendRoutine != null) StopCoroutine(blendRoutine);
        blendRoutine = null;

        if (firstPhase || blendSeconds <= 0f)
        {
            SetWeightsInstant(index);
            Apply();
        }
        else
        {
            blendRoutine = StartCoroutine(BlendTo(index));
        }
    }

    private IEnumerator BlendTo(int index)
    {
        float[] start = (float[])weights.Clone();
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / blendSeconds;
            float k = Mathf.Clamp01(t);
            for (int i = 0; i < weights.Length; i++)
                weights[i] = Mathf.Lerp(start[i], i == index ? 1f : 0f, k);
            Apply();
            yield return null;
        }
        SetWeightsInstant(index);
        Apply();
        blendRoutine = null;
    }

    private void SetWeightsInstant(int index)
    {
        for (int i = 0; i < weights.Length; i++) weights[i] = i == index ? 1f : 0f;
    }

    private int FindZoneIndex(int phaseNumber)
    {
        for (int i = 0; i < zones.Length; i++)
            if (zones[i] != null && zones[i].phaseNumber == phaseNumber) return i;
        return -1;
    }

    private void Apply()
    {
        Color tint = new Color(0f, 0f, 0f, 0f);
        Color background = new Color(0f, 0f, 0f, 0f);
        float particleRate = 0f;

        for (int i = 0; i < zones.Length; i++)
        {
            ZoneVisual zone = zones[i];
            if (zone == null) continue;
            float w = weights[i];

            if (zone.backgroundLayers != null)
            {
                foreach (SpriteRenderer layer in zone.backgroundLayers)
                {
                    if (layer == null) continue;
                    Color c = layer.color;
                    c.a = w;
                    layer.color = c;
                }
            }

            tint += zone.waterTint * w;
            background += zone.cameraBackground * w;
            particleRate += zone.particleRate * w;
        }

        if (tintOverlay != null) tintOverlay.color = tint;
        if (targetCamera != null) targetCamera.backgroundColor = background;

        if (pollutionParticles != null)
        {
            foreach (ParticleSystem ps in pollutionParticles)
            {
                if (ps == null) continue;
                ParticleSystem.EmissionModule emission = ps.emission;
                emission.rateOverTime = particleRate;
            }
        }
    }
}
