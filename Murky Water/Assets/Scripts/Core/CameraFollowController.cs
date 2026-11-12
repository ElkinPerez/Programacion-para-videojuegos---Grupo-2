using UnityEngine;

/// Follows the fish only along the X axis and keeps it near 25% of the screen width,
/// leaving visual space ahead to anticipate obstacles and see the predator.

[RequireComponent(typeof(Camera))]
public class CameraFollowController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private GameBalanceConfig balanceConfig;
    [SerializeField, Min(0f)] private float smoothTimeOverride = -1f;
    [SerializeField] private bool snapOnStart = true;

    private Camera targetCamera;
    private float velocityX;
    private float fixedY;
    private float fixedZ;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        fixedY = transform.position.y;
        fixedZ = transform.position.z;

        if (player == null)
        {
            PlayerController controller = FindAnyObjectByType<PlayerController>();
            if (controller != null) player = controller.transform;
        }

        if (balanceConfig == null)
            Debug.LogWarning("CameraFollowController: asign GameBalanceConfig.", this);
    }

    private void Start()
    {
        if (snapOnStart && player != null)
        {
            Vector3 position = transform.position;
            position.x = GetDesiredCameraX();
            transform.position = position;
        }
    }

    private void LateUpdate()
    {
        if (player == null || targetCamera == null) return;

        float desiredX = GetDesiredCameraX();
        float smoothTime = smoothTimeOverride >= 0f
            ? smoothTimeOverride
            : balanceConfig != null ? balanceConfig.cameraFollowSmoothTime : 0.2f;

        float nextX = smoothTime <= 0f
            ? desiredX
            : Mathf.SmoothDamp(transform.position.x, desiredX, ref velocityX, smoothTime);

        transform.position = new Vector3(nextX, fixedY, fixedZ);
    }

    private float GetDesiredCameraX()
    {
        float anchor = balanceConfig != null ? balanceConfig.playerScreenHorizontalAnchor : 0.25f;
        anchor = Mathf.Clamp01(anchor);

        // The viewport width in world units depends on the orthographic size and aspect ratio.
        float worldWidth = targetCamera.orthographic
            ? targetCamera.orthographicSize * 2f * targetCamera.aspect
            : GetPerspectiveWorldWidth();

        return player.position.x + (0.5f - anchor) * worldWidth;
    }

    private float GetPerspectiveWorldWidth()
    {
        float depth = Mathf.Abs(transform.position.z - player.position.z);
        float halfHeight = depth * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        return halfHeight * targetCamera.aspect * 2f;
    }
}
