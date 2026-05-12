using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class InfiniteTilemapGenerator : MonoBehaviour
{
    [SerializeField] private GameObject tilemapPrefab;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform seedTilemap;
    [SerializeField] private int preloadChunks = 1;
    [SerializeField] private int cleanupPadding = 1;

    private readonly Dictionary<Vector2Int, Transform> spawnedChunks = new Dictionary<Vector2Int, Transform>();
    private Vector2 chunkSize = Vector2.one;
    private float chunkZ;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        CacheChunkSize();
        RegisterSeedTilemap();
    }

    private void Start()
    {
        UpdateChunksAroundCamera();
    }

    private void LateUpdate()
    {
        UpdateChunksAroundCamera();
    }

    private void CacheChunkSize()
    {
        Transform sizeSource = seedTilemap != null ? seedTilemap : tilemapPrefab != null ? tilemapPrefab.transform : null;
        Tilemap sourceTilemap = sizeSource != null ? sizeSource.GetComponentInChildren<Tilemap>() : null;

        if (sourceTilemap != null)
        {
            Vector3 localBoundsSize = sourceTilemap.localBounds.size;
            Vector3 cellBoundsSize = sourceTilemap.cellBounds.size;
            Grid parentGrid = GetComponent<Grid>();
            Vector3 cellSize = parentGrid != null ? parentGrid.cellSize : Vector3.one;

            chunkSize = new Vector2(
                Mathf.Max(localBoundsSize.x, cellBoundsSize.x * cellSize.x, 1f),
                Mathf.Max(localBoundsSize.y, cellBoundsSize.y * cellSize.y, 1f));
        }

        chunkZ = sizeSource != null ? sizeSource.position.z : transform.position.z;
    }

    private void RegisterSeedTilemap()
    {
        if (seedTilemap == null)
        {
            SpawnChunk(Vector2Int.zero);
            return;
        }

        Vector2Int seedCoordinate = WorldToChunkCoordinate(seedTilemap.position);
        spawnedChunks[seedCoordinate] = seedTilemap;
    }

    private void UpdateChunksAroundCamera()
    {
        if (targetCamera == null || tilemapPrefab == null)
        {
            return;
        }

        Vector2 cameraPosition = targetCamera.transform.position;
        Vector2Int centerCoordinate = WorldToChunkCoordinate(cameraPosition);
        Vector2 cameraExtents = GetCameraExtents();

        int horizontalRadius = Mathf.CeilToInt(cameraExtents.x / chunkSize.x) + preloadChunks;
        int verticalRadius = Mathf.CeilToInt(cameraExtents.y / chunkSize.y) + preloadChunks;

        for (int x = centerCoordinate.x - horizontalRadius; x <= centerCoordinate.x + horizontalRadius; x++)
        {
            for (int y = centerCoordinate.y - verticalRadius; y <= centerCoordinate.y + verticalRadius; y++)
            {
                Vector2Int coordinate = new Vector2Int(x, y);
                if (!spawnedChunks.ContainsKey(coordinate))
                {
                    SpawnChunk(coordinate);
                }
            }
        }

        RemoveFarChunks(centerCoordinate, horizontalRadius + cleanupPadding, verticalRadius + cleanupPadding);
    }

    private Vector2 GetCameraExtents()
    {
        if (targetCamera.orthographic)
        {
            return new Vector2(targetCamera.orthographicSize * targetCamera.aspect, targetCamera.orthographicSize);
        }

        float distanceToMap = Mathf.Abs(targetCamera.transform.position.z - chunkZ);
        Vector3 bottomLeft = targetCamera.ViewportToWorldPoint(new Vector3(0f, 0f, distanceToMap));
        Vector3 topRight = targetCamera.ViewportToWorldPoint(new Vector3(1f, 1f, distanceToMap));
        return new Vector2(Mathf.Abs(topRight.x - bottomLeft.x) * 0.5f, Mathf.Abs(topRight.y - bottomLeft.y) * 0.5f);
    }

    private Vector2Int WorldToChunkCoordinate(Vector2 worldPosition)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPosition.x / chunkSize.x),
            Mathf.RoundToInt(worldPosition.y / chunkSize.y));
    }

    private void SpawnChunk(Vector2Int coordinate)
    {
        if (tilemapPrefab == null)
        {
            return;
        }

        Vector3 position = new Vector3(coordinate.x * chunkSize.x, coordinate.y * chunkSize.y, chunkZ);
        GameObject chunk = Instantiate(tilemapPrefab, position, Quaternion.identity, transform);
        chunk.name = $"{tilemapPrefab.name}_{coordinate.x}_{coordinate.y}";
        spawnedChunks[coordinate] = chunk.transform;
    }

    private void RemoveFarChunks(Vector2Int centerCoordinate, int horizontalRadius, int verticalRadius)
    {
        List<Vector2Int> coordinatesToRemove = null;

        foreach (KeyValuePair<Vector2Int, Transform> spawnedChunk in spawnedChunks)
        {
            Vector2Int coordinate = spawnedChunk.Key;
            if (Mathf.Abs(coordinate.x - centerCoordinate.x) <= horizontalRadius &&
                Mathf.Abs(coordinate.y - centerCoordinate.y) <= verticalRadius)
            {
                continue;
            }

            if (coordinatesToRemove == null)
            {
                coordinatesToRemove = new List<Vector2Int>();
            }

            coordinatesToRemove.Add(coordinate);
        }

        if (coordinatesToRemove == null)
        {
            return;
        }

        foreach (Vector2Int coordinate in coordinatesToRemove)
        {
            Transform chunk = spawnedChunks[coordinate];
            spawnedChunks.Remove(coordinate);

            if (chunk != null && chunk != seedTilemap)
            {
                Destroy(chunk.gameObject);
            }
        }
    }
}
