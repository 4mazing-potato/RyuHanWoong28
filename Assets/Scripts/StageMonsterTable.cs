using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class StageMonsterTable
{
    private const string ResourcePath = "StageMonster";
    private const int ExpectedColumnCount = 9;

    private static IReadOnlyList<StageMonsterData> cachedRows;
    private static IReadOnlyDictionary<int, IReadOnlyList<StageMonsterData>> cachedRowsByStageId;

    public static IReadOnlyList<StageMonsterData> Rows
    {
        get
        {
            EnsureLoaded();
            return cachedRows;
        }
    }

    public static IReadOnlyList<StageMonsterData> GetRowsForStage(int stageId)
    {
        EnsureLoaded();
        if (cachedRowsByStageId.TryGetValue(stageId, out IReadOnlyList<StageMonsterData> rows))
        {
            return rows;
        }

        return Array.Empty<StageMonsterData>();
    }

    public static void Reload()
    {
        LoadStageMonsterCsv();
    }

    private static void EnsureLoaded()
    {
        if (cachedRows != null && cachedRowsByStageId != null)
        {
            return;
        }

        LoadStageMonsterCsv();
    }

    private static void LoadStageMonsterCsv()
    {
        TextAsset stageMonsterCsv = Resources.Load<TextAsset>(ResourcePath);
        if (stageMonsterCsv == null)
        {
            throw new InvalidOperationException($"Resources/{ResourcePath}.csv could not be loaded.");
        }

        string[] lines = stageMonsterCsv.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        List<StageMonsterData> rows = new List<StageMonsterData>();
        Dictionary<int, List<StageMonsterData>> mutableRowsByStageId = new Dictionary<int, List<StageMonsterData>>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            string[] columns = line.Split(',');
            if (columns.Length < ExpectedColumnCount)
            {
                throw new FormatException($"Invalid {ResourcePath}.csv row at line {i + 1}: expected {ExpectedColumnCount} columns.");
            }

            StageMonsterData row = ParseRow(columns, i + 1);
            rows.Add(row);

            if (!mutableRowsByStageId.TryGetValue(row.StageId, out List<StageMonsterData> stageRows))
            {
                stageRows = new List<StageMonsterData>();
                mutableRowsByStageId.Add(row.StageId, stageRows);
            }

            stageRows.Add(row);
        }

        Dictionary<int, IReadOnlyList<StageMonsterData>> rowsByStageId = new Dictionary<int, IReadOnlyList<StageMonsterData>>();
        foreach (KeyValuePair<int, List<StageMonsterData>> stageRows in mutableRowsByStageId)
        {
            rowsByStageId.Add(stageRows.Key, stageRows.Value);
        }

        cachedRows = rows;
        cachedRowsByStageId = rowsByStageId;
    }

    private static StageMonsterData ParseRow(string[] columns, int lineNumber)
    {
        int stageId = ParseInt(columns[0], nameof(StageMonsterData.StageId), lineNumber, 0);
        string monsterId = columns[1].Trim();
        if (string.IsNullOrEmpty(monsterId))
        {
            throw new FormatException($"Invalid {ResourcePath}.csv row at line {lineNumber}: MonsterId is empty.");
        }

        float spawnStartSec = ParseFloat(columns[2], nameof(StageMonsterData.SpawnStartSec), lineNumber, 0f);
        float waveIntervalSec = ParseFloat(columns[3], nameof(StageMonsterData.WaveIntervalSec), lineNumber, 0.01f);
        int waveSizeStart = ParseInt(columns[4], nameof(StageMonsterData.WaveSizeStart), lineNumber, 0);
        int waveSizeGrowth = ParseInt(columns[5], nameof(StageMonsterData.WaveSizeGrowth), lineNumber, 0);
        int waveSizeMax = ParseInt(columns[6], nameof(StageMonsterData.WaveSizeMax), lineNumber, 0);
        int totalBudget = ParseInt(columns[7], nameof(StageMonsterData.TotalBudget), lineNumber, 0);
        int maxAliveCap = ParseInt(columns[8], nameof(StageMonsterData.MaxAliveCap), lineNumber, 0);

        return new StageMonsterData(
            stageId,
            monsterId,
            spawnStartSec,
            waveIntervalSec,
            waveSizeStart,
            waveSizeGrowth,
            waveSizeMax,
            totalBudget,
            maxAliveCap);
    }

    private static int ParseInt(string value, string columnName, int lineNumber, int minimumValue)
    {
        if (!int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) || result < minimumValue)
        {
            throw new FormatException($"Invalid {ResourcePath}.csv row at line {lineNumber}: {columnName} must be an integer greater than or equal to {minimumValue}.");
        }

        return result;
    }

    private static float ParseFloat(string value, string columnName, int lineNumber, float minimumValue)
    {
        if (!float.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float result) || result < minimumValue)
        {
            throw new FormatException($"Invalid {ResourcePath}.csv row at line {lineNumber}: {columnName} must be a number greater than or equal to {minimumValue}.");
        }

        return result;
    }
}
