using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

public static class LevelUpCardTable
{
    private const string ResourcePath = "LevelUpCard";
    private const int ExpectedColumnCount = 7;

    private static IReadOnlyList<LevelUpCardData> cachedRows;
    private static IReadOnlyDictionary<int, LevelUpCardData> cachedRowsById;

    public static IReadOnlyList<LevelUpCardData> Rows
    {
        get
        {
            EnsureLoaded();
            return cachedRows;
        }
    }

    public static bool TryGetCard(int id, out LevelUpCardData card)
    {
        EnsureLoaded();
        return cachedRowsById.TryGetValue(id, out card);
    }

    public static LevelUpCardData GetCard(int id)
    {
        if (TryGetCard(id, out LevelUpCardData card))
        {
            return card;
        }

        throw new KeyNotFoundException($"Level-up card ID {id} was not found in Resources/{ResourcePath}.csv.");
    }

    public static void Reload()
    {
        LoadLevelUpCardCsv();
    }

    private static void EnsureLoaded()
    {
        if (cachedRows != null && cachedRowsById != null)
        {
            return;
        }

        LoadLevelUpCardCsv();
    }

    private static void LoadLevelUpCardCsv()
    {
        TextAsset levelUpCardCsv = Resources.Load<TextAsset>(ResourcePath);
        if (levelUpCardCsv == null)
        {
            throw new InvalidOperationException($"Resources/{ResourcePath}.csv could not be loaded.");
        }

        string[] lines = levelUpCardCsv.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        List<LevelUpCardData> rows = new List<LevelUpCardData>();
        Dictionary<int, LevelUpCardData> rowsById = new Dictionary<int, LevelUpCardData>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            List<string> columns = SplitCsvLine(line);
            if (columns.Count < ExpectedColumnCount)
            {
                throw new FormatException($"Invalid {ResourcePath}.csv row at line {i + 1}: expected ID,Ratio,Required,Icon,Desc,Effect,Value.");
            }

            int id = ParseInt(columns[0], nameof(LevelUpCardData.ID), i + 1, 1);
            float ratio = ParseFloat(columns[1], nameof(LevelUpCardData.Ratio), i + 1, 0f);
            int? required = ParseOptionalInt(columns[2], nameof(LevelUpCardData.Required), i + 1, 1);
            string icon = columns[3].Trim();
            string desc = columns[4].Trim();
            LevelUpCardEffect effect = ParseEffect(columns[5], i + 1);
            float? value = ParseOptionalFloat(columns[6], nameof(LevelUpCardData.Value), i + 1);

            if (string.IsNullOrEmpty(icon))
            {
                throw new FormatException($"Invalid {ResourcePath}.csv row at line {i + 1}: Icon is empty.");
            }

            if (string.IsNullOrEmpty(desc))
            {
                throw new FormatException($"Invalid {ResourcePath}.csv row at line {i + 1}: Desc is empty.");
            }

            LevelUpCardData row = new LevelUpCardData(id, ratio, required, icon, desc, effect, value);
            if (rowsById.ContainsKey(id))
            {
                throw new FormatException($"Duplicate ID {id} found in {ResourcePath}.csv.");
            }

            rows.Add(row);
            rowsById.Add(id, row);
        }

        if (rows.Count == 0)
        {
            throw new FormatException($"{ResourcePath}.csv must contain at least one card row.");
        }

        cachedRows = rows;
        cachedRowsById = rowsById;
    }

    private static List<string> SplitCsvLine(string line)
    {
        List<string> columns = new List<string>();
        StringBuilder column = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    column.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                columns.Add(column.ToString());
                column.Clear();
            }
            else
            {
                column.Append(c);
            }
        }

        columns.Add(column.ToString());
        return columns;
    }

    private static int ParseInt(string value, string columnName, int lineNumber, int minimumValue)
    {
        if (!int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) || result < minimumValue)
        {
            throw new FormatException($"Invalid {ResourcePath}.csv row at line {lineNumber}: {columnName} must be an integer greater than or equal to {minimumValue}.");
        }

        return result;
    }

    private static int? ParseOptionalInt(string value, string columnName, int lineNumber, int minimumValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return ParseInt(value, columnName, lineNumber, minimumValue);
    }

    private static float ParseFloat(string value, string columnName, int lineNumber, float minimumValue)
    {
        if (!float.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float result) || result < minimumValue)
        {
            throw new FormatException($"Invalid {ResourcePath}.csv row at line {lineNumber}: {columnName} must be a number greater than or equal to {minimumValue.ToString(CultureInfo.InvariantCulture)}.");
        }

        return result;
    }

    private static float? ParseOptionalFloat(string value, string columnName, int lineNumber)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return ParseFloat(value, columnName, lineNumber, float.MinValue);
    }

    private static LevelUpCardEffect ParseEffect(string value, int lineNumber)
    {
        if (!Enum.TryParse(value.Trim(), out LevelUpCardEffect effect))
        {
            throw new FormatException($"Invalid {ResourcePath}.csv row at line {lineNumber}: Effect '{value}' is not a {nameof(LevelUpCardEffect)} value.");
        }

        return effect;
    }
}
