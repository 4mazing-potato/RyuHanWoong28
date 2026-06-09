using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class StageTable
{
    private const string ResourcePath = "Stage";
    private const int ExpectedColumnCount = 5;

    private static IReadOnlyList<StageData> cachedStages;
    private static IReadOnlyDictionary<int, StageData> cachedStagesById;

    public static IReadOnlyList<StageData> Stages
    {
        get
        {
            EnsureLoaded();
            return cachedStages;
        }
    }

    public static bool TryGetStage(int stageId, out StageData stage)
    {
        EnsureLoaded();
        return cachedStagesById.TryGetValue(stageId, out stage);
    }

    public static StageData GetStage(int stageId)
    {
        if (TryGetStage(stageId, out StageData stage))
        {
            return stage;
        }

        throw new KeyNotFoundException($"StageId {stageId} was not found in {ResourcePath}.csv.");
    }

    public static void Reload()
    {
        LoadStageCsv();
    }

    private static void EnsureLoaded()
    {
        if (cachedStages != null && cachedStagesById != null)
        {
            return;
        }

        LoadStageCsv();
    }

    private static void LoadStageCsv()
    {
        TextAsset stageCsv = Resources.Load<TextAsset>(ResourcePath);
        if (stageCsv == null)
        {
            throw new InvalidOperationException($"Resources/{ResourcePath}.csv could not be loaded.");
        }

        string[] lines = stageCsv.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        List<StageData> stages = new List<StageData>();
        Dictionary<int, StageData> stagesById = new Dictionary<int, StageData>();

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
                throw new FormatException($"Invalid {ResourcePath}.csv row at line {i + 1}: expected StageId,Tilemap,Time,Image,Name.");
            }

            int stageId = int.Parse(columns[0].Trim(), CultureInfo.InvariantCulture);
            string tilemap = columns[1].Trim();
            if (string.IsNullOrEmpty(tilemap))
            {
                throw new FormatException($"Invalid {ResourcePath}.csv row at line {i + 1}: Tilemap is empty.");
            }

            if (!float.TryParse(columns[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float time) || time < 0f)
            {
                throw new FormatException($"Invalid {ResourcePath}.csv row at line {i + 1}: Time must be a number greater than or equal to 0.");
            }

            string image = columns[3].Trim();
            if (string.IsNullOrEmpty(image))
            {
                throw new FormatException($"Invalid {ResourcePath}.csv row at line {i + 1}: Image is empty.");
            }

            string name = columns[4].Trim();
            if (string.IsNullOrEmpty(name))
            {
                throw new FormatException($"Invalid {ResourcePath}.csv row at line {i + 1}: Name is empty.");
            }

            StageData stage = new StageData(stageId, tilemap, time, image, name);
            if (stagesById.ContainsKey(stageId))
            {
                throw new FormatException($"Duplicate StageId {stageId} found in {ResourcePath}.csv.");
            }

            stages.Add(stage);
            stagesById.Add(stageId, stage);
        }

        cachedStages = stages;
        cachedStagesById = stagesById;
    }
}
