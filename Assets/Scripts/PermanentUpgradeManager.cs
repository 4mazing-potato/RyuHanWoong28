using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PermanentUpgradeManager
{
    public const int MaxUpgradeLevel = 5;

    private const string UpgradeStatusResourcePath = "UpgradeStatus";
    private const string UpgradeLevelSaveKeyPrefix = "UpgradeLevel_";

    private static readonly Dictionary<PlayerUpgradeStat, SortedDictionary<int, UpgradeStatusData>> dataByStat = new Dictionary<PlayerUpgradeStat, SortedDictionary<int, UpgradeStatusData>>();
    private static bool loaded;

    public static event Action UpgradesChanged;

    public static IReadOnlyList<PlayerUpgradeStat> Stats
    {
        get
        {
            EnsureLoaded();
            return dataByStat.Keys.OrderBy(stat => (int)stat).ToArray();
        }
    }

    public static void EnsureLoaded()
    {
        if (loaded)
        {
            return;
        }

        loaded = true;
        dataByStat.Clear();

        TextAsset csv = Resources.Load<TextAsset>(UpgradeStatusResourcePath);
        if (csv == null)
        {
            Debug.LogWarning($"{UpgradeStatusResourcePath}.csv could not be loaded from Resources.");
            return;
        }

        string[] lines = csv.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++)
        {
            if (!UpgradeStatusData.TryParse(lines[i], out UpgradeStatusData data))
            {
                Debug.LogWarning($"Invalid upgrade row skipped: {lines[i]}");
                continue;
            }

            if (!dataByStat.TryGetValue(data.Stat, out SortedDictionary<int, UpgradeStatusData> levels))
            {
                levels = new SortedDictionary<int, UpgradeStatusData>();
                dataByStat.Add(data.Stat, levels);
            }

            levels[data.Level] = data;
        }
    }

    public static int GetLevel(PlayerUpgradeStat stat)
    {
        EnsureLoaded();
        return Mathf.Clamp(PlayerPrefs.GetInt(GetLevelSaveKey(stat), 0), 0, GetMaxLevel(stat));
    }

    public static UpgradeStatusData GetCurrentData(PlayerUpgradeStat stat)
    {
        return GetData(stat, GetLevel(stat));
    }

    public static UpgradeStatusData GetNextData(PlayerUpgradeStat stat)
    {
        int currentLevel = GetLevel(stat);
        return currentLevel >= GetMaxLevel(stat) ? null : GetData(stat, currentLevel + 1);
    }

    public static UpgradeStatusData GetData(PlayerUpgradeStat stat, int level)
    {
        EnsureLoaded();
        if (dataByStat.TryGetValue(stat, out SortedDictionary<int, UpgradeStatusData> levels)
            && levels.TryGetValue(Mathf.Clamp(level, 0, MaxUpgradeLevel), out UpgradeStatusData data))
        {
            return data;
        }

        return null;
    }

    public static int GetMaxLevel(PlayerUpgradeStat stat)
    {
        EnsureLoaded();
        return dataByStat.TryGetValue(stat, out SortedDictionary<int, UpgradeStatusData> levels) && levels.Count > 0
            ? Mathf.Clamp(levels.Keys.Max(), 0, MaxUpgradeLevel)
            : 0;
    }

    public static bool TryUpgrade(PlayerUpgradeStat stat, out int spentCoin)
    {
        spentCoin = 0;
        UpgradeStatusData nextData = GetNextData(stat);
        if (nextData == null)
        {
            return false;
        }

        if (!CoinManager.TrySpendTotalCoins(nextData.CoinValue))
        {
            return false;
        }

        spentCoin = nextData.CoinValue;
        PlayerPrefs.SetInt(GetLevelSaveKey(stat), nextData.Level);
        PlayerPrefs.Save();
        UpgradesChanged?.Invoke();
        return true;
    }

    public static float GetStatValue(PlayerUpgradeStat stat, float fallbackValue)
    {
        UpgradeStatusData currentData = GetCurrentData(stat);
        return currentData != null ? currentData.StatValue : fallbackValue;
    }

    public static void ResetAllUpgradeLevels()
    {
        EnsureLoaded();
        foreach (PlayerUpgradeStat stat in Enum.GetValues(typeof(PlayerUpgradeStat)))
        {
            PlayerPrefs.SetInt(GetLevelSaveKey(stat), 0);
        }

        PlayerPrefs.Save();
        UpgradesChanged?.Invoke();
    }

    private static string GetLevelSaveKey(PlayerUpgradeStat stat)
    {
        return $"{UpgradeLevelSaveKeyPrefix}{stat}";
    }
}
