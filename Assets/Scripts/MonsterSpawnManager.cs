using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MonsterSpawnManager : MonoBehaviour
{
    private const string GameSceneName = "GameScene";
    private const string PlayerTag = "Player";

    [SerializeField] private int currentStageId = 1;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private float spawnOutsideMargin = 1.5f;
    [SerializeField] private float spawnRadiusRandomRange = 2f;
    [SerializeField] private float spawnZ = 0f;

    private readonly List<SpawnRuleState> ruleStates = new List<SpawnRuleState>();
    private float stageStartTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapForGameScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != GameSceneName || FindObjectOfType<MonsterSpawnManager>() != null)
        {
            return;
        }

        new GameObject(nameof(MonsterSpawnManager)).AddComponent<MonsterSpawnManager>();
    }

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        FindPlayerTarget();
    }

    private void Start()
    {
        LoadStageSpawnRules();
    }

    private void Update()
    {
        if (playerTarget == null)
        {
            FindPlayerTarget();
            if (playerTarget == null)
            {
                return;
            }
        }

        float elapsedSec = Time.time - stageStartTime;
        for (int i = 0; i < ruleStates.Count; i++)
        {
            UpdateRule(ruleStates[i], elapsedSec);
        }
    }

    public void UnregisterMonster(int ruleIndex)
    {
        if (ruleIndex < 0 || ruleIndex >= ruleStates.Count)
        {
            return;
        }

        ruleStates[ruleIndex].AliveCount = Mathf.Max(0, ruleStates[ruleIndex].AliveCount - 1);
    }

    private void LoadStageSpawnRules()
    {
        StageTable.GetStage(currentStageId);
        IReadOnlyList<StageMonsterData> rows = StageMonsterTable.GetRowsForStage(currentStageId);

        ruleStates.Clear();
        for (int i = 0; i < rows.Count; i++)
        {
            GameObject prefab = LoadMonsterPrefab(rows[i].MonsterId);
            if (prefab == null)
            {
                Debug.LogError($"Monster prefab '{rows[i].MonsterId}' could not be found in a Resources folder.", this);
                continue;
            }

            ruleStates.Add(new SpawnRuleState(rows[i], prefab, ruleStates.Count));
        }

        stageStartTime = Time.time;
    }

    private void UpdateRule(SpawnRuleState state, float elapsedSec)
    {
        if (state.TotalSpawned >= state.Data.TotalBudget || elapsedSec < state.NextWaveTimeSec)
        {
            return;
        }

        int safetyCount = 0;
        while (elapsedSec >= state.NextWaveTimeSec && state.TotalSpawned < state.Data.TotalBudget && safetyCount < 16)
        {
            SpawnWave(state);
            state.CompletedWaveCount++;
            state.NextWaveTimeSec += state.Data.WaveIntervalSec;
            safetyCount++;
        }
    }

    private void SpawnWave(SpawnRuleState state)
    {
        int waveSize = Mathf.Min(
            state.Data.WaveSizeStart + (state.CompletedWaveCount * state.Data.WaveSizeGrowth),
            state.Data.WaveSizeMax);
        int remainingBudget = state.Data.TotalBudget - state.TotalSpawned;
        int remainingAliveSlots = state.Data.MaxAliveCap - state.AliveCount;
        int spawnCount = Mathf.Min(waveSize, remainingBudget, remainingAliveSlots);

        for (int i = 0; i < spawnCount; i++)
        {
            SpawnMonster(state);
        }
    }

    private void SpawnMonster(SpawnRuleState state)
    {
        Vector3 spawnPosition = GetSpawnPositionOutsideCameraView();
        GameObject monster = Instantiate(state.Prefab, spawnPosition, Quaternion.identity, transform);

        MonsterController controller = monster.GetComponent<MonsterController>();
        if (controller == null)
        {
            controller = monster.AddComponent<MonsterController>();
        }

        controller.Initialize(playerTarget, this, state.RuleIndex);
        state.TotalSpawned++;
        state.AliveCount++;
    }

    private Vector3 GetSpawnPositionOutsideCameraView()
    {
        Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
        Vector3 center = playerTarget != null ? playerTarget.position : Vector3.zero;
        float radius = GetCameraViewOuterRadius(cameraToUse, center) + Mathf.Max(0f, spawnOutsideMargin);
        radius += Random.Range(0f, Mathf.Max(0f, spawnRadiusRandomRange));

        Vector2 direction = Random.insideUnitCircle.normalized;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.right;
        }

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

    private float GetCameraViewOuterRadius(Camera cameraToUse, Vector3 center)
    {
        if (cameraToUse == null)
        {
            return 10f;
        }

        if (cameraToUse.orthographic)
        {
            float halfHeight = cameraToUse.orthographicSize;
            float halfWidth = halfHeight * cameraToUse.aspect;
            return Mathf.Sqrt((halfWidth * halfWidth) + (halfHeight * halfHeight));
        }

        Plane spawnPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, spawnZ));
        float maxDistance = 0f;
        Vector3[] viewportCorners =
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(1f, 1f, 0f)
        };

        for (int i = 0; i < viewportCorners.Length; i++)
        {
            Ray ray = cameraToUse.ViewportPointToRay(viewportCorners[i]);
            if (spawnPlane.Raycast(ray, out float enter))
            {
                Vector3 corner = ray.GetPoint(enter);
                maxDistance = Mathf.Max(maxDistance, Vector2.Distance(center, corner));
            }
        }

        return maxDistance > 0f ? maxDistance : 10f;
    }

    private void FindPlayerTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag(PlayerTag);
        playerTarget = player != null ? player.transform : null;
    }

    private static GameObject LoadMonsterPrefab(string monsterId)
    {
        GameObject prefab = Resources.Load<GameObject>(monsterId);
        if (prefab != null)
        {
            return prefab;
        }

        return Resources.Load<GameObject>($"Prefabs/{monsterId}");
    }

    private sealed class SpawnRuleState
    {
        public SpawnRuleState(StageMonsterData data, GameObject prefab, int ruleIndex)
        {
            Data = data;
            Prefab = prefab;
            RuleIndex = ruleIndex;
            NextWaveTimeSec = data.SpawnStartSec;
        }

        public StageMonsterData Data { get; }
        public GameObject Prefab { get; }
        public int RuleIndex { get; }
        public float NextWaveTimeSec { get; set; }
        public int CompletedWaveCount { get; set; }
        public int TotalSpawned { get; set; }
        public int AliveCount { get; set; }
    }
}
