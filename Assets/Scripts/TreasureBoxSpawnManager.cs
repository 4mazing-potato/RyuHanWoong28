using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TreasureBoxSpawnManager : MonoBehaviour
{
    private const string GameSceneName = "GameScene";
    private const string PlayerTag = "Player";

    [Header("Treasure Box Spawn")]
    [SerializeField] private GameObject treasureBoxPrefab;
    [SerializeField] private float spawnCooldown = 30f;
    [SerializeField] private float initialDelay = 10f;
    [SerializeField] private int maxActiveBoxes = 3;

    [Header("Spawn Position")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private float spawnOutsideMargin = 1.5f;
    [SerializeField] private float spawnDistanceRandomRange = 2f;
    [SerializeField] private float spawnZ = 0f;

    private readonly HashSet<TreasureBoxController> activeBoxes = new HashSet<TreasureBoxController>();
    private float cooldownTimer;

    public GameObject TreasureBoxPrefab
    {
        get => treasureBoxPrefab;
        set => treasureBoxPrefab = value;
    }

    public float SpawnCooldown
    {
        get => spawnCooldown;
        set => spawnCooldown = Mathf.Max(0.01f, value);
    }

    public float InitialDelay
    {
        get => initialDelay;
        set => initialDelay = Mathf.Max(0f, value);
    }

    public int MaxActiveBoxes
    {
        get => maxActiveBoxes;
        set => maxActiveBoxes = Mathf.Max(0, value);
    }

    public int ActiveBoxCount => activeBoxes.Count;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapForGameScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != GameSceneName || FindObjectOfType<TreasureBoxSpawnManager>() != null)
        {
            return;
        }

        new GameObject(nameof(TreasureBoxSpawnManager)).AddComponent<TreasureBoxSpawnManager>();
    }

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        FindPlayerTarget();
        cooldownTimer = InitialDelay;
    }

    private void OnValidate()
    {
        spawnCooldown = Mathf.Max(0.01f, spawnCooldown);
        initialDelay = Mathf.Max(0f, initialDelay);
        maxActiveBoxes = Mathf.Max(0, maxActiveBoxes);
        spawnOutsideMargin = Mathf.Max(0f, spawnOutsideMargin);
        spawnDistanceRandomRange = Mathf.Max(0f, spawnDistanceRandomRange);
    }

    private void Update()
    {
        if (GameplayPauseManager.IsPaused)
        {
            return;
        }

        if (playerTarget == null)
        {
            FindPlayerTarget();
            if (playerTarget == null)
            {
                return;
            }
        }

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f)
        {
            return;
        }

        cooldownTimer = SpawnCooldown;
        if (activeBoxes.Count < MaxActiveBoxes)
        {
            TrySpawnTreasureBox();
        }
    }

    public void UnregisterTreasureBox(TreasureBoxController box)
    {
        if (box == null)
        {
            return;
        }

        activeBoxes.Remove(box);
    }

    private void TrySpawnTreasureBox()
    {
        if (treasureBoxPrefab == null)
        {
            Debug.LogWarning($"{nameof(TreasureBoxSpawnManager)} requires a TreasureBoxPrefab assignment before it can spawn boxes.", this);
            return;
        }

        Vector3 spawnPosition = GetSpawnPositionOutsideCameraView();
        GameObject treasureBox = Instantiate(treasureBoxPrefab, spawnPosition, Quaternion.identity, transform);
        TreasureBoxController controller = treasureBox.GetComponent<TreasureBoxController>();
        if (controller == null)
        {
            controller = treasureBox.AddComponent<TreasureBoxController>();
        }

        controller.Initialize(this);
        controller.Destroyed += HandleTreasureBoxDestroyed;
        activeBoxes.Add(controller);
    }

    private void HandleTreasureBoxDestroyed(TreasureBoxController box)
    {
        if (box == null)
        {
            return;
        }

        box.Destroyed -= HandleTreasureBoxDestroyed;
        activeBoxes.Remove(box);
    }

    private Vector3 GetSpawnPositionOutsideCameraView()
    {
        Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
        Vector3 center = playerTarget != null ? playerTarget.position : Vector3.zero;
        float radius = GetCameraViewOuterRadius(cameraToUse, center) + spawnOutsideMargin + Random.Range(0f, spawnDistanceRandomRange);

        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector3 spawnPosition = new Vector3(center.x + direction.x * radius, center.y + direction.y * radius, spawnZ);

        int retryCount = 0;
        while (IsInsideCameraView(cameraToUse, spawnPosition) && retryCount < 8)
        {
            radius += Mathf.Max(1f, spawnOutsideMargin);
            spawnPosition = new Vector3(center.x + direction.x * radius, center.y + direction.y * radius, spawnZ);
            retryCount++;
        }

        return spawnPosition;
    }

    private static bool IsInsideCameraView(Camera cameraToUse, Vector3 worldPosition)
    {
        if (cameraToUse == null)
        {
            return false;
        }

        Vector3 viewportPosition = cameraToUse.WorldToViewportPoint(worldPosition);
        return viewportPosition.z > 0f
            && viewportPosition.x >= 0f
            && viewportPosition.x <= 1f
            && viewportPosition.y >= 0f
            && viewportPosition.y <= 1f;
    }

    private static float GetCameraViewOuterRadius(Camera cameraToUse, Vector3 center)
    {
        if (cameraToUse == null)
        {
            return 10f;
        }

        Vector3[] viewportCorners =
        {
            new Vector3(0f, 0f, Mathf.Abs(cameraToUse.transform.position.z - center.z)),
            new Vector3(0f, 1f, Mathf.Abs(cameraToUse.transform.position.z - center.z)),
            new Vector3(1f, 0f, Mathf.Abs(cameraToUse.transform.position.z - center.z)),
            new Vector3(1f, 1f, Mathf.Abs(cameraToUse.transform.position.z - center.z))
        };

        float maxDistance = 0f;
        for (int i = 0; i < viewportCorners.Length; i++)
        {
            Vector3 worldCorner = cameraToUse.ViewportToWorldPoint(viewportCorners[i]);
            worldCorner.z = center.z;
            maxDistance = Mathf.Max(maxDistance, Vector3.Distance(center, worldCorner));
        }

        return maxDistance > 0f ? maxDistance : 10f;
    }

    private void FindPlayerTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag(PlayerTag);
        playerTarget = player != null ? player.transform : null;
    }
}
