public readonly struct StageData
{
    public StageData(int stageId, string tilemap, float time)
    {
        StageId = stageId;
        Tilemap = tilemap;
        Time = time;
    }

    public int StageId { get; }
    public string Tilemap { get; }
    public float Time { get; }
}
