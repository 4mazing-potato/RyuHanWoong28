public readonly struct StageMonsterData
{
    public StageMonsterData(
        int stageId,
        string monsterId,
        float spawnStartSec,
        float waveIntervalSec,
        int waveSizeStart,
        int waveSizeGrowth,
        int waveSizeMax,
        int totalBudget,
        int maxAliveCap)
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
}
