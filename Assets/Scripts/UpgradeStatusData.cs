using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public sealed class UpgradeStatusData
{
    public int ID { get; }
    public PlayerUpgradeStat Stat { get; }
    public string StatName { get; }
    public int Level { get; }
    public float StatValue { get; }
    public int CoinValue { get; }

    public UpgradeStatusData(int id, PlayerUpgradeStat stat, string statName, int level, float statValue, int coinValue)
    {
        ID = id;
        Stat = stat;
        StatName = statName;
        Level = Mathf.Clamp(level, 0, PermanentUpgradeManager.MaxUpgradeLevel);
        StatValue = statValue;
        CoinValue = Mathf.Max(0, coinValue);
    }

    public static bool TryParse(string csvLine, out UpgradeStatusData data)
    {
        data = null;
        string[] columns = SplitCsvLine(csvLine);
        if (columns.Length < 6)
        {
            return false;
        }

        if (!int.TryParse(columns[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)
            || !Enum.TryParse(columns[1], true, out PlayerUpgradeStat stat)
            || !int.TryParse(columns[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int level)
            || !float.TryParse(columns[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float statValue)
            || !int.TryParse(columns[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int coinValue))
        {
            return false;
        }

        data = new UpgradeStatusData(id, stat, columns[2], level, statValue, coinValue);
        return true;
    }

    private static string[] SplitCsvLine(string csvLine)
    {
        List<string> columns = new List<string>();
        if (string.IsNullOrEmpty(csvLine))
        {
            return columns.ToArray();
        }

        bool inQuote = false;
        string current = string.Empty;
        for (int i = 0; i < csvLine.Length; i++)
        {
            char c = csvLine[i];
            if (c == '"')
            {
                if (inQuote && i + 1 < csvLine.Length && csvLine[i + 1] == '"')
                {
                    current += '"';
                    i++;
                }
                else
                {
                    inQuote = !inQuote;
                }
            }
            else if (c == ',' && !inQuote)
            {
                columns.Add(current.Trim());
                current = string.Empty;
            }
            else
            {
                current += c;
            }
        }

        columns.Add(current.Trim());
        return columns.ToArray();
    }
}
