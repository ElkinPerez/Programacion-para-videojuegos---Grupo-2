using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Gradually blends backgrounds, tint color and particles when the pollution zone changes.
/// Listens to GameEvents.OnPhaseChanged and matches each zone through PhaseConfig.phaseNumber.
/// The blend lasts blendSeconds (GDD: about 5 s). The first phase received is applied instantly.
///
/// Setup: one ZoneVisual per zone. Put each zone's background groups on a higher
/// Sorting Order than the previous zone so the crossfade reads correctly.
/// Each zone takes GROUPS (GameObjects): every SpriteRenderer inside them (children included)
/// is found automatically and blended. Particle systems inside the groups (e.g., bubbles)
/// are also found: their emission fades with the zone and their renderer is hidden at weight 0.
public class EnvironmentVisualController : MonoBehaviour
{
    [System.Serializable]
    public class ZoneVisual
    {
        public string label = "Zone";
        [Min(1)] public int phaseNumber = 1;
        [Tooltip("Background groups of this zone. All SpriteRenderers and ParticleSystems inside (children included) are blended.")]
        public GameObject[] backgroundGroups;
        [Tooltip("Water tint. The alpha channel controls how strong the tint is.")]
        public Color waterTint = new Color(0f, 0f, 0f, 0f);
        public Color cameraBackground = new Color(0.05f, 0.2f, 0.4f, 1f);
        [Min(0f)] public float particleRate = 5f;

        [System.NonSerialized] private SpriteRenderer[] renderers;
        [System.NonSerialized] private float[] baseAlpha;
        [System.NonSerialized] private ParticleSystem[] particles;
        [System.NonSerialized] private ParticleSystemRenderer[] particleRenderers;
        [System.NonSerialized] private float[] baseEmission;

        /// Finds every SpriteRenderer and ParticleSystem inside the groups and remembers
        /// their original alpha / emission.
        public void CacheRenderers()
        {
            List<SpriteRenderer> found = new List<SpriteRenderer>();
            List<ParticleSystem> foundParticles = new List<ParticleSystem>();
            if (backgroundGroups != null)
            {
                foreach (GameObject group in backgroundGroups)
                {
                    if (group == null) continue;
                    found.AddRange(group.GetComponentsInChildren<SpriteRenderer>(true));
                    foundParticles.AddRange(group.GetComponentsInChildren<ParticleSystem>(true));
                }
            }

            renderers = found.ToArray();
            baseAlpha = new float[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                baseAlpha[i] = renderers[i].color.a;

            // Particle systems inside the groups (e.g., bubbles): remember their original emission.
            particles = foundParticles.ToArray();
            particleRenderers = new ParticleSystemRenderer[particles.Length];
            baseEmission = new float[particles.Length];
            for (int i = 0; i < particles.Length; i++)
            {
                particleRenderers[i] = particles[i].GetComponent<ParticleSystemRenderer>();
                baseEmission[i] = particles[i].emission.rateOverTimeMultiplier;
            }
        }

        /// Sets the zone's opacity (0-1), multiplied by each sprite's original alpha.
        public void SetOpacity(float opacity)
        {
            if (renderers == null) CacheRenderers();

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color c = renderers[i].color;
                c.a = baseAlpha[i] * opacity;
                renderers[i].color = c;
            }

            // Particles: fade by emitting less, and hide the renderer once the zone is fully out.
            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i] == null) continue;

                ParticleSystem.EmissionModule emission = particles[i].emission;
                emission.rateOverTimeMultiplier = baseEmission[i] * opacity;

                if (particleRenderers[i] != null)
                    particleRenderers[i].enabled = opacity > 0.001f;
            }
        }
    }

    [SerializeField] private ZoneVisual[] zones = new ZoneVisual[4];
    [SerializeField, Min(0f)] private float blendSeconds = 5f;

    [Header("Optional targets")]
    [Tooltip("Large sprite in front of the scene (child of the camera) used as water tint.")]
    [SerializeField] private SpriteRenderer tintOverlay;
    [Tooltip("The camera needs Clear Flags = Solid Color for the background color to show.")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("Global pollution particles only. Do NOT put particles that live inside a zone group here.")]
    [SerializeField] private ParticleSystem[] pollutionParticles;

    private float[] weights;
    private Coroutine blendRoutine;
    private int targetIndex = -1;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        weights = new float[zones != null ? zones.Length : 0];

        // Find the SpriteRenderers and ParticleSystems of every group once
        // (call CacheRenderers again if children are added later).
        if (zones != null)
            foreach (ZoneVisual zone in zones)
                if (zone != null) zone.CacheRenderers();
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

            // Alpha and particle emission only (the groups stay active so ParallaxLayer tiling keeps working).
            zone.SetOpacity(w);

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