using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class MonsterSpawnBalanceTool : EditorWindow
{
    private const string StageCsvPath = "Assets/Resources/Stage.csv";
    private const string StageMonsterCsvPath = "Assets/Resources/StageMonster.csv";
    private const string StageHeader = "StageId,Tilemap,Time,Image,Name";
    private const string StageMonsterHeader = "StageId,MonsterId,SpawnStartSec,WaveIntervalSec,WaveSizeStart,WaveSizeGrowth,WaveSizeMax,TotalBudget,MaxAliveCap";

    private readonly List<GameObject> monsterPrefabs = new List<GameObject>();
    private Vector2 scrollPosition;
    private int stageId = 1;
    private float stageTimeSec = 150f;
    private float clearSuccessProbability = 70f;
    private AnimationCurve difficultyCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private float playerAttack = 10f;
    private float attackIntervalSec = 0.5f;
    private float criticalChancePercent = 0f;
    private float criticalDamageMultiplier = 2f;
    private float projectileDamageMultiplier = 1f;
    private float combatEfficiency = 0.8f;

    [MenuItem("Tools/Monster Spawn Balancer")]
    public static void Open()
    {
        GetWindow<MonsterSpawnBalanceTool>("Monster Spawn Balancer");
    }

    private void OnEnable()
    {
        if (monsterPrefabs.Count == 0)
        {
            monsterPrefabs.Add(null);
        }

        LoadStageTimeFromCsv();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("Stage Target", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        stageId = EditorGUILayout.IntField("StageId", Mathf.Max(1, stageId));
        if (EditorGUI.EndChangeCheck())
        {
            LoadStageTimeFromCsv();
        }

        stageTimeSec = EditorGUILayout.FloatField("버티기 시간(초)", Mathf.Max(1f, stageTimeSec));
        clearSuccessProbability = EditorGUILayout.Slider("클리어 성공 확률(%)", clearSuccessProbability, 1f, 99f);
        difficultyCurve = EditorGUILayout.CurveField("난이도 증가 곡선", difficultyCurve, Color.red, new Rect(0f, 0f, 1f, 1f));

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Stage.csv 시간 불러오기"))
        {
            LoadStageTimeFromCsv();
        }

        if (GUILayout.Button("StageMonster.csv 몬스터 순서 불러오기"))
        {
            LoadMonsterOrderFromCsv();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Monster Order", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("위에서 아래 순서대로 StageMonster.SpawnStartSec가 배치됩니다. 프리팹 이름이 StageMonster.MonsterId로 저장됩니다.", MessageType.Info);
        DrawMonsterList();

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Player Baseline", EditorStyles.boldLabel);
        playerAttack = EditorGUILayout.FloatField("일반 공격력", Mathf.Max(0.01f, playerAttack));
        attackIntervalSec = EditorGUILayout.FloatField("공격 간격(초)", Mathf.Max(0.05f, attackIntervalSec));
        criticalChancePercent = EditorGUILayout.Slider("치명타 확률(%)", criticalChancePercent, 0f, 100f);
        criticalDamageMultiplier = EditorGUILayout.FloatField("치명타 배율", Mathf.Max(1f, criticalDamageMultiplier));
        projectileDamageMultiplier = EditorGUILayout.FloatField("투사체/스킬 데미지 배율", Mathf.Max(0.01f, projectileDamageMultiplier));
        combatEfficiency = EditorGUILayout.Slider("실전 명중/딜 효율", combatEfficiency, 0.1f, 1f);

        EditorGUILayout.Space(10f);
        DrawPreview();

        EditorGUILayout.Space(10f);
        using (new EditorGUI.DisabledScope(!CanApply()))
        {
            if (GUILayout.Button("Stage / StageMonster CSV에 적용", GUILayout.Height(32f)))
            {
                ApplyToCsv();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawMonsterList()
    {
        for (int i = 0; i < monsterPrefabs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{i + 1}", GUILayout.Width(24f));
            monsterPrefabs[i] = (GameObject)EditorGUILayout.ObjectField(monsterPrefabs[i], typeof(GameObject), false);

            using (new EditorGUI.DisabledScope(i == 0))
            {
                if (GUILayout.Button("↑", GUILayout.Width(28f)))
                {
                    SwapMonsters(i, i - 1);
                }
            }

            using (new EditorGUI.DisabledScope(i >= monsterPrefabs.Count - 1))
            {
                if (GUILayout.Button("↓", GUILayout.Width(28f)))
                {
                    SwapMonsters(i, i + 1);
                }
            }

            if (GUILayout.Button("-", GUILayout.Width(28f)))
            {
                monsterPrefabs.RemoveAt(i);
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("몬스터 추가"))
        {
            monsterPrefabs.Add(null);
        }
    }

    private void DrawPreview()
    {
        EditorGUILayout.LabelField("Generated Preview", EditorStyles.boldLabel);
        IReadOnlyList<GeneratedStageMonsterRow> rows = GenerateRows();
        if (rows.Count == 0)
        {
            EditorGUILayout.HelpBox("적용할 몬스터 프리팹을 1개 이상 지정하세요.", MessageType.Warning);
            return;
        }

        float estimatedDps = EstimatePlayerDps();
        EditorGUILayout.LabelField("예상 플레이어 DPS", FormatFloat(estimatedDps));
        EditorGUILayout.LabelField("성공 확률 보정", FormatFloat(GetPressureMultiplier()));

        foreach (GeneratedStageMonsterRow row in rows)
        {
            EditorGUILayout.LabelField(
                row.MonsterId,
                $"Start {FormatFloat(row.SpawnStartSec)}s / Interval {FormatFloat(row.WaveIntervalSec)}s / Size {row.WaveSizeStart}+{row.WaveSizeGrowth} <= {row.WaveSizeMax} / Budget {row.TotalBudget} / Alive {row.MaxAliveCap}");
        }
    }

    private bool CanApply()
    {
        if (stageId <= 0 || stageTimeSec <= 0f || monsterPrefabs.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < monsterPrefabs.Count; i++)
        {
            if (monsterPrefabs[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyToCsv()
    {
        List<GeneratedStageMonsterRow> generatedRows = GenerateRows();
        if (generatedRows.Count == 0)
        {
            EditorUtility.DisplayDialog("Monster Spawn Balancer", "몬스터 프리팹을 1개 이상 지정하세요.", "OK");
            return;
        }

        try
        {
            UpdateStageCsv();
            UpdateStageMonsterCsv(generatedRows);
            AssetDatabase.Refresh();
            StageTable.Reload();
            StageMonsterTable.Reload();
            EditorUtility.DisplayDialog("Monster Spawn Balancer", "Stage.csv와 StageMonster.csv에 밸런싱 결과를 적용했습니다.", "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Monster Spawn Balancer", exception.Message, "OK");
        }
    }

    private List<GeneratedStageMonsterRow> GenerateRows()
    {
        List<GameObject> validPrefabs = GetValidMonsterPrefabs();
        List<GeneratedStageMonsterRow> rows = new List<GeneratedStageMonsterRow>();
        if (validPrefabs.Count == 0)
        {
            return rows;
        }

        float dps = EstimatePlayerDps();
        float pressureMultiplier = GetPressureMultiplier();
        float playableDuration = Mathf.Max(1f, stageTimeSec);
        float spawnWindowScale = validPrefabs.Count <= 1 ? 0f : 0.82f / (validPrefabs.Count - 1);

        for (int i = 0; i < validPrefabs.Count; i++)
        {
            GameObject prefab = validPrefabs[i];
            float orderRatio = validPrefabs.Count <= 1 ? 0f : i * spawnWindowScale;
            float spawnStartSec = Mathf.Round(playableDuration * orderRatio * 10f) * 0.1f;
            float curveRatio = Mathf.Clamp01(spawnStartSec / playableDuration);
            float difficulty = Mathf.Clamp01(difficultyCurve.Evaluate(curveRatio));
            float monsterHealth = EstimateMonsterHealth(prefab);
            float killRatePerSec = Mathf.Max(0.05f, dps / Mathf.Max(0.1f, monsterHealth));
            float spawnRatePerSec = Mathf.Max(0.05f, killRatePerSec * Mathf.Lerp(0.65f, 1.45f, difficulty) * pressureMultiplier);
            float waveIntervalSec = Mathf.Clamp(3f / spawnRatePerSec, 1f, 8f);
            int waveSizeStart = Mathf.Max(1, Mathf.CeilToInt(spawnRatePerSec * waveIntervalSec * Mathf.Lerp(0.65f, 1.05f, difficulty)));
            int waveSizeGrowth = Mathf.Max(0, Mathf.RoundToInt(difficulty * pressureMultiplier));
            int waveSizeMax = Mathf.Max(waveSizeStart, Mathf.CeilToInt(spawnRatePerSec * waveIntervalSec * Mathf.Lerp(1.25f, 2.35f, difficulty) * pressureMultiplier));
            float activeDuration = Mathf.Max(1f, playableDuration - spawnStartSec);
            int totalBudget = Mathf.Max(waveSizeMax, Mathf.CeilToInt(activeDuration * spawnRatePerSec * Mathf.Lerp(1f, 1.35f, 1f - GetClearProbability01())));
            int maxAliveCap = Mathf.Max(waveSizeMax, Mathf.CeilToInt(spawnRatePerSec * Mathf.Lerp(8f, 18f, difficulty) * pressureMultiplier));

            rows.Add(new GeneratedStageMonsterRow(
                stageId,
                prefab.name,
                spawnStartSec,
                Mathf.Round(waveIntervalSec * 10f) * 0.1f,
                waveSizeStart,
                waveSizeGrowth,
                waveSizeMax,
                totalBudget,
                maxAliveCap));
        }

        return rows;
    }

    private List<GameObject> GetValidMonsterPrefabs()
    {
        List<GameObject> validPrefabs = new List<GameObject>();
        for (int i = 0; i < monsterPrefabs.Count; i++)
        {
            if (monsterPrefabs[i] == null)
            {
                continue;
            }

            validPrefabs.Add(monsterPrefabs[i]);
        }

        return validPrefabs;
    }

    private float EstimatePlayerDps()
    {
        float critBonus = 1f + (criticalChancePercent * 0.01f * (criticalDamageMultiplier - 1f));
        return playerAttack * projectileDamageMultiplier * critBonus * combatEfficiency / Mathf.Max(0.05f, attackIntervalSec);
    }

    private float GetPressureMultiplier()
    {
        return Mathf.Lerp(1.65f, 0.75f, GetClearProbability01());
    }

    private float GetClearProbability01()
    {
        return Mathf.Clamp01(clearSuccessProbability * 0.01f);
    }

    private static float EstimateMonsterHealth(GameObject prefab)
    {
        if (prefab == null)
        {
            return 1f;
        }

        MonsterController controller = prefab.GetComponent<MonsterController>();
        if (controller == null)
        {
            return 1f;
        }

        SerializedObject serializedObject = new SerializedObject(controller);
        SerializedProperty maxHpProperty = serializedObject.FindProperty("maxHP");
        return maxHpProperty != null ? Mathf.Max(0.1f, maxHpProperty.floatValue) : 1f;
    }

    private void LoadStageTimeFromCsv()
    {
        if (!File.Exists(StageCsvPath))
        {
            return;
        }

        string[] lines = File.ReadAllLines(StageCsvPath, Encoding.UTF8);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            string[] columns = lines[i].Split(',');
            if (columns.Length < 3 || !int.TryParse(columns[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedStageId) || parsedStageId != stageId)
            {
                continue;
            }

            if (float.TryParse(columns[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedTime))
            {
                stageTimeSec = Mathf.Max(1f, parsedTime);
            }

            return;
        }
    }

    private void LoadMonsterOrderFromCsv()
    {
        if (!File.Exists(StageMonsterCsvPath))
        {
            return;
        }

        string[] lines = File.ReadAllLines(StageMonsterCsvPath, Encoding.UTF8);
        List<GameObject> loadedPrefabs = new List<GameObject>();
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            string[] columns = lines[i].Split(',');
            if (columns.Length < 3 || !int.TryParse(columns[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedStageId) || parsedStageId != stageId)
            {
                continue;
            }

            GameObject prefab = LoadMonsterPrefabAsset(columns[1].Trim());
            if (prefab != null)
            {
                loadedPrefabs.Add(prefab);
            }
        }

        if (loadedPrefabs.Count <= 0)
        {
            EditorUtility.DisplayDialog("Monster Spawn Balancer", $"StageId {stageId}에 해당하는 몬스터 프리팹을 찾지 못했습니다.", "OK");
            return;
        }

        monsterPrefabs.Clear();
        monsterPrefabs.AddRange(loadedPrefabs);
    }

    private static GameObject LoadMonsterPrefabAsset(string monsterId)
    {
        if (string.IsNullOrEmpty(monsterId))
        {
            return null;
        }

        string[] guids = AssetDatabase.FindAssets($"{monsterId} t:Prefab");
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null && prefab.name == monsterId)
            {
                return prefab;
            }
        }

        return null;
    }

    private void UpdateStageCsv()
    {
        List<string> lines = ReadCsvLines(StageCsvPath, StageHeader);
        bool updated = false;

        for (int i = 1; i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            string[] columns = EnsureColumnCount(lines[i].Split(','), 5);
            if (!int.TryParse(columns[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedStageId) || parsedStageId != stageId)
            {
                continue;
            }

            columns[2] = FormatFloat(stageTimeSec);
            lines[i] = string.Join(",", columns);
            updated = true;
            break;
        }

        if (!updated)
        {
            lines.Add(string.Join(",", stageId.ToString(CultureInfo.InvariantCulture), $"Tilemap{stageId}", FormatFloat(stageTimeSec), $"Stage{stageId}", $"Stage {stageId}"));
        }

        WriteCsvLines(StageCsvPath, lines);
    }

    private void UpdateStageMonsterCsv(IReadOnlyList<GeneratedStageMonsterRow> generatedRows)
    {
        List<string> lines = ReadCsvLines(StageMonsterCsvPath, StageMonsterHeader);
        List<string> newLines = new List<string> { lines.Count > 0 ? lines[0] : StageMonsterHeader };

        for (int i = 1; i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            string[] columns = lines[i].Split(',');
            if (columns.Length == 0 || !int.TryParse(columns[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedStageId) || parsedStageId == stageId)
            {
                continue;
            }

            newLines.Add(lines[i]);
        }

        for (int i = 0; i < generatedRows.Count; i++)
        {
            newLines.Add(generatedRows[i].ToCsv());
        }

        WriteCsvLines(StageMonsterCsvPath, newLines);
    }

    private static List<string> ReadCsvLines(string path, string header)
    {
        if (!File.Exists(path))
        {
            return new List<string> { header };
        }

        List<string> lines = new List<string>(File.ReadAllLines(path, Encoding.UTF8));
        if (lines.Count == 0)
        {
            lines.Add(header);
        }

        return lines;
    }

    private static void WriteCsvLines(string path, IReadOnlyList<string> lines)
    {
        File.WriteAllText(path, string.Join("\n", lines) + "\n", new UTF8Encoding(false));
    }

    private static string[] EnsureColumnCount(string[] columns, int columnCount)
    {
        if (columns.Length >= columnCount)
        {
            return columns;
        }

        string[] expandedColumns = new string[columnCount];
        for (int i = 0; i < expandedColumns.Length; i++)
        {
            expandedColumns[i] = i < columns.Length ? columns[i] : string.Empty;
        }

        return expandedColumns;
    }

    private static string FormatFloat(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private void SwapMonsters(int firstIndex, int secondIndex)
    {
        GameObject previous = monsterPrefabs[firstIndex];
        monsterPrefabs[firstIndex] = monsterPrefabs[secondIndex];
        monsterPrefabs[secondIndex] = previous;
    }

    private readonly struct GeneratedStageMonsterRow
    {
        public GeneratedStageMonsterRow(int stageId, string monsterId, float spawnStartSec, float waveIntervalSec, int waveSizeStart, int waveSizeGrowth, int waveSizeMax, int totalBudget, int maxAliveCap)
        {
            StageId = stageId;
            MonsterId = monsterId;
            SpawnStartSec = spawnStartSec;
            WaveIntervalSec = waveIntervalSec;
            WaveSizeStart = waveSizeStart;
            WaveSizeGrowth = waveSizeGrowth;
            WaveSizeMax = waveSizeMax;
            TotalBudget = totalBudget;
            MaxAliveCap = maxAliveCap;
        }

        public int StageId { get; }
        public string MonsterId { get; }
        public float SpawnStartSec { get; }
        public float WaveIntervalSec { get; }
        public int WaveSizeStart { get; }
        public int WaveSizeGrowth { get; }
        public int WaveSizeMax { get; }
        public int TotalBudget { get; }
        public int MaxAliveCap { get; }

        public string ToCsv()
        {
            return string.Join(",",
                StageId.ToString(CultureInfo.InvariantCulture),
                MonsterId,
                FormatFloat(SpawnStartSec),
                FormatFloat(WaveIntervalSec),
                WaveSizeStart.ToString(CultureInfo.InvariantCulture),
                WaveSizeGrowth.ToString(CultureInfo.InvariantCulture),
                WaveSizeMax.ToString(CultureInfo.InvariantCulture),
                TotalBudget.ToString(CultureInfo.InvariantCulture),
                MaxAliveCap.ToString(CultureInfo.InvariantCulture));
        }
    }
}
