public readonly struct StageData
{
    public StageData(int stageId, string tilemap)
    {
        StageId = stageId;
        Tilemap = tilemap;
    }

    public int StageId { get; }
    public string Tilemap { get; }
}
