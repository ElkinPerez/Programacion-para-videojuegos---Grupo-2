using System.Collections.Generic;
using UnityEngine;

/// Reuses obstacles, food and debris instead of creating/destroying them continuously.
/// An instance counts as FREE when it is inactive. That is deliberate: FoodItem deactivates itself
/// when eaten, so it automatically becomes available again without calling Release.
/// Keep this object at position (0,0,0) with scale 1, because spawned objects are parented to it.
public class ObjectPool : MonoBehaviour
{
    [Tooltip("Parent of the instances. Empty = this object.")]
    [SerializeField] private Transform container;
    [Tooltip("Safety limit per prefab. 0 = unlimited.")]
    [SerializeField, Min(0)] private int maxInstancesPerPrefab = 128;

    private readonly Dictionary<GameObject, List<GameObject>> pools = new Dictionary<GameObject, List<GameObject>>();
    private bool warnedLimit;

    private void Awake()
    {
        if (container == null) container = transform;
    }

    /// Returns an active instance placed at the given position, reusing a free one when possible.
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;
        if (container == null) container = transform;

        List<GameObject> list = GetList(prefab);

        for (int i = list.Count - 1; i >= 0; i--)
        {
            GameObject item = list[i];
            if (item == null)
            {
                list.RemoveAt(i);
                continue;
            }

            if (!item.activeSelf)
            {
                item.transform.SetPositionAndRotation(position, rotation);
                item.SetActive(true);
                return item;
            }
        }

        if (maxInstancesPerPrefab > 0 && list.Count >= maxInstancesPerPrefab)
        {
            if (!warnedLimit)
            {
                warnedLimit = true;
                Debug.LogWarning($"ObjectPool: limit of {maxInstancesPerPrefab} instances reached for '{prefab.name}'.", this);
            }
            return null;
        }

        GameObject created = Instantiate(prefab, position, rotation, container);
        list.Add(created);
        return created;
    }

    /// Returns the instance to the pool (it simply gets deactivated).
    public void Release(GameObject instance)
    {
        if (instance != null) instance.SetActive(false);
    }

    /// Creates inactive instances in advance to avoid hiccups during play.
    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;
        if (container == null) container = transform;

        List<GameObject> list = GetList(prefab);
        for (int i = 0; i < count; i++)
        {
            if (maxInstancesPerPrefab > 0 && list.Count >= maxInstancesPerPrefab) break;
            GameObject created = Instantiate(prefab, container);
            created.SetActive(false);
            list.Add(created);
        }
    }

    /// Deactivates every instance managed by this pool.
    public void ReleaseAll()
    {
        foreach (KeyValuePair<GameObject, List<GameObject>> pair in pools)
        {
            foreach (GameObject item in pair.Value)
                if (item != null) item.SetActive(false);
        }
    }

    private List<GameObject> GetList(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out List<GameObject> list))
        {
            list = new List<GameObject>();
            pools[prefab] = list;
        }
        return list;
    }
}
