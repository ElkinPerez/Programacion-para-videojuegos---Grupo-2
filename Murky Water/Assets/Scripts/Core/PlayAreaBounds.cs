using UnityEngine;

/// Defines the vertical boundaries of the playable area using two scene markers.
/// Other systems access PlayAreaBounds.Instance.TopY and BottomY.
public class PlayAreaBounds : MonoBehaviour
{
    public static PlayAreaBounds Instance { get; private set; }

    [Header("Scene Markers")]
    [SerializeField] private Transform topMarker;
    [SerializeField] private Transform bottomMarker;

    [Header("Temporary Fallback If Markers Are Not Assigned")]
    [SerializeField] private float fallbackTopY = 4f;
    [SerializeField] private float fallbackBottomY = -4f;

    public float TopY => topMarker != null ? topMarker.position.y : fallbackTopY;
    public float BottomY => bottomMarker != null ? bottomMarker.position.y : fallbackBottomY;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("There is more than one active PlayAreaBounds. Only one should exist per scene.", this);
            enabled = false;
            return;
        }

        Instance = this;
        ValidateBounds();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnValidate()
    {
        if (fallbackTopY < fallbackBottomY)
        {
            float temp = fallbackTopY;
            fallbackTopY = fallbackBottomY;
            fallbackBottomY = temp;
        }

        if (topMarker != null && bottomMarker != null && topMarker.position.y < bottomMarker.position.y)
            Debug.LogWarning("PlayAreaBounds: the Top marker must be above Bottom.", this);
    }

    private void ValidateBounds()
    {
        if (TopY <= BottomY)
            Debug.LogError("PlayAreaBounds: TopY must be greater than BottomY. Check the markers.", this);
    }
}
