using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class LevelXpTable
{
    private const string ResourcePath = "LevelXp";
    private const int ExpectedColumnCount = 2;

    private static IReadOnlyList<LevelXpData> cachedRows;
    private static IReadOnlyDictionary<int, LevelXpData> cachedRowsByLevel;

    public static IReadOnlyList<LevelXpData> Rows
    {
        get
        {
            EnsureLoaded();
            return cachedRows;
        }
    }

    public static bool TryGetNeedXp(int level, out int needXp)
    {
        EnsureLoaded();
        if (cachedRowsByLevel.TryGetValue(level, out LevelXpData levelXp))
        {
            needXp = levelXp.NeedXp;
            return true;
        }

        needXp = 0;
        return false;
    }

    public static int GetNeedXp(int level)
    {
        if (TryGetNeedXp(level, out int needXp))
        {
            return needXp;
        }

        throw new KeyNotFoundException($"Level {level} was not found in Resources/{ResourcePath}.csv.");
    }

    public static void Reload()
    {
        LoadLevelXpCsv();
    }

    private static void EnsureLoaded()
    {
        if (cachedRows != null && cachedRowsByLevel != null)
        {
            return;
        }

        LoadLevelXpCsv();
    }

    private static void LoadLevelXpCsv()
    {
        TextAsset levelXpCsv = Resources.Load<TextAsset>(ResourcePath);
        if (levelXpCsv == null)
        {
            throw new InvalidOperationException($"Resources/{ResourcePath}.csv could not be loaded.");
        }

        string[] lines = levelXpCsv.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        List<LevelXpData> rows = new List<LevelXpData>();
        Dictionary<int, LevelXpData> rowsByLevel = new Dictionary<int, LevelXpData>();

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
                throw new FormatException($"Invalid {ResourcePath}.csv row at line {i + 1}: expected Level,NeedXP.");
            }

            int level = ParseInt(columns[0], nameof(LevelXpData.Level), i + 1, 1);
            int needXp = ParseInt(columns[1], nameof(LevelXpData.NeedXp), i + 1, 1);
            LevelXpData row = new LevelXpData(level, needXp);

            if (rowsByLevel.ContainsKey(level))
            {
                throw new FormatException($"Duplicate Level {level} found in {ResourcePath}.csv.");
            }

            rows.Add(row);
            rowsByLevel.Add(level, row);
        }

        if (rows.Count == 0)
        {
            throw new FormatException($"{ResourcePath}.csv must contain at least one level row.");
        }

        cachedRows = rows;
        cachedRowsByLevel = rowsByLevel;
    }

    private static int ParseInt(string value, string columnName, int lineNumber, int minimumValue)
    {
        if (!int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) || result < minimumValue)
        {
            throw new FormatException($"Invalid {ResourcePath}.csv row at line {lineNumber}: {columnName} must be an integer greater than or equal to {minimumValue}.");
        }

        return result;
    }
}
