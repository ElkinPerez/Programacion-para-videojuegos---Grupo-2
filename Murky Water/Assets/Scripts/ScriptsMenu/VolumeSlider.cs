using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// Put this on a UI Slider to control one AudioMixer volume (music, SFX or ambient).
/// The value is saved, so it is remembered between scenes and sessions.
/// Setup: add it to the Slider, drag the AudioMixer, and write the exposed parameter name.
[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;
    [Tooltip("Exposed mixer parameter: MusicVolume, SFXVolume or AmbientVolume.")]
    [SerializeField] private string parameterName = VolumePrefs.MusicParam;
    [Tooltip("Value used the first time, before the player changes anything.")]
    [SerializeField, Range(0f, 1f)] private float defaultValue = 1f;

    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        if (mixer == null)
            Debug.LogWarning("VolumeSlider: assign the AudioMixer.", this);
    }

    private void OnEnable()
    {
        // Show the saved value without triggering the change event.
        slider.SetValueWithoutNotify(VolumePrefs.Get(parameterName, defaultValue));
        slider.onValueChanged.AddListener(HandleValueChanged);
    }

    // Applied in Start (not Awake/OnEnable) because exposed mixer parameters may not be ready earlier.
    private void Start()
    {
        VolumePrefs.Apply(mixer, parameterName, slider.value);
    }

    private void OnDisable()
    {
        slider.onValueChanged.RemoveListener(HandleValueChanged);
        PlayerPrefs.Save();
    }

    private void HandleValueChanged(float value)
    {
        VolumePrefs.Set(parameterName, value);
        VolumePrefs.Apply(mixer, parameterName, value);
    }
}
