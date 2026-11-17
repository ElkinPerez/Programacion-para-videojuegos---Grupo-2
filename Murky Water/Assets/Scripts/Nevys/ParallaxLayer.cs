using UnityEngine;

/// Background layer with relative displacement against Camera.main.
/// Factor 0 = stays fixed in the world (moves past at full speed, foreground).
/// Factor 1 = follows the camera exactly (looks static, very far away).
/// Values in between give the depth effect. Optional horizontal tiling for endless scrolling.
/// Runs after CameraFollowController (execution order 100) so it reads the final camera position.
[DefaultExecutionOrder(100)]
public class ParallaxLayer : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float parallaxFactorX = 0.5f;
    [SerializeField, Range(0f, 1f)] private float parallaxFactorY = 0f;

    [Header("Horizontal tiling")]
    [Tooltip("Repeats the sprite endlessly. The sprite must tile seamlessly (Mesh Type: Full Rect).")]
    [SerializeField] private bool tileHorizontally = true;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Camera cam;
    private Vector3 startPosition;
    private Vector3 startCameraPosition;
    private float tileWorldWidth;
    private bool initialized;

    private void Awake()
    {
        cam = Camera.main;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (tileHorizontally && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            tileWorldWidth = spriteSize.x * Mathf.Abs(transform.lossyScale.x);

            // Three tiles centered on this object so there is always art on both sides of the camera.
            spriteRenderer.drawMode = SpriteDrawMode.Tiled;
            spriteRenderer.size = new Vector2(spriteSize.x * 3f, spriteSize.y);
        }
        else
        {
            tileHorizontally = false;
        }
    }

    private void LateUpdate()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        Vector3 camPos = cam.transform.position;

        // Captured on the first frame, after CameraFollowController has snapped to the fish.
        if (!initialized)
        {
            startPosition = transform.position;
            startCameraPosition = camPos;
            initialized = true;
        }

        Vector3 delta = camPos - startCameraPosition;
        float x = startPosition.x + delta.x * parallaxFactorX;
        float y = startPosition.y + delta.y * parallaxFactorY;

        if (tileHorizontally && tileWorldWidth > 0.01f)
            x += Mathf.Round((camPos.x - x) / tileWorldWidth) * tileWorldWidth;

        transform.position = new Vector3(x, y, startPosition.z);
    }
}
