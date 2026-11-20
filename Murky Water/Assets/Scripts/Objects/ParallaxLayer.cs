
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies parallax movement to an entire group of objects.
/// Supports multiple SpriteRenderers and continuous horizontal recycling.
/// Runs after CameraFollowController.
/// </summary>
[DefaultExecutionOrder(100)]
public class ParallaxLayer : MonoBehaviour
{
    [Header("Parallax Settings")]
    [SerializeField, Range(0f, 1f)]
    private float parallaxFactorX = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float parallaxFactorY = 0f;

    [Header("Horizontal Tiling")]
    [SerializeField]
    private bool tileHorizontally = false;

    private Camera cam;

    private Vector3 startPosition;
    private Vector3 startCameraPosition;
    private bool initialized;

    private SpriteRenderer[] tileRenderers;
    private float tileGroupWidth;

    private void Awake()
    {
        cam = Camera.main;

        if (tileHorizontally)
            InitializeTiling();
    }

    private void InitializeTiling()
    {
        // Collect all SpriteRenderers belonging to this group.
        tileRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (tileRenderers.Length == 0)
        {
            Debug.LogWarning(
                $"ParallaxLayer: No SpriteRenderers found in {name}."
            );

            tileHorizontally = false;
            return;
        }

        // Calculate the complete width of the group.
        float minX = float.MaxValue;
        float maxX = float.MinValue;

        foreach (SpriteRenderer sr in tileRenderers)
        {
            minX = Mathf.Min(minX, sr.bounds.min.x);
            maxX = Mathf.Max(maxX, sr.bounds.max.x);
        }

        tileGroupWidth = maxX - minX;

        if (tileGroupWidth <= 0.01f)
        {
            Debug.LogWarning(
                $"ParallaxLayer: Invalid tiling width in {name}."
            );

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

        if (!initialized)
        {
            startPosition = transform.position;
            startCameraPosition = camPos;
            initialized = true;
        }

        // Apply parallax movement to the entire group.
        Vector3 delta = camPos - startCameraPosition;

        float x = startPosition.x + delta.x * parallaxFactorX;
        float y = startPosition.y + delta.y * parallaxFactorY;

        transform.position = new Vector3(
            x,
            y,
            startPosition.z
        );

        // Recycle individual sprites when they leave the camera.
        if (tileHorizontally)
            UpdateTiling();
    }

    private void UpdateTiling()
    {
        if (!cam.orthographic) return;

        float cameraHalfWidth =
            cam.orthographicSize * cam.aspect;

        float cameraLeft =
            cam.transform.position.x - cameraHalfWidth;

        float cameraRight =
            cam.transform.position.x + cameraHalfWidth;

        // Avoid moving the same Transform more than once.
        HashSet<Transform> processed = new HashSet<Transform>();

        foreach (SpriteRenderer sr in tileRenderers)
        {
            if (sr == null || !sr.enabled) continue;

            Transform spriteTransform = sr.transform;

            if (!processed.Add(spriteTransform))
                continue;

            Bounds bounds = sr.bounds;

            // Move sprites leaving the left side to the right.
            while (bounds.max.x < cameraLeft)
            {
                spriteTransform.position +=
                    Vector3.right * tileGroupWidth;

                bounds = sr.bounds;
            }

            // Move sprites leaving the right side to the left.
            while (bounds.min.x > cameraRight)
            {
                spriteTransform.position -=
                    Vector3.right * tileGroupWidth;

                bounds = sr.bounds;
            }
        }
    }
}
