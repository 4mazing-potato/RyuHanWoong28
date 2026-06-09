public readonly struct StageData
{
    public StageData(int stageId, string tilemap, float time, string image, string name)
    {
        StageId = stageId;
        Tilemap = tilemap;
        Time = time;
        Image = image;
        Name = name;
    }

    public int StageId { get; }
    public string Tilemap { get; }
    public float Time { get; }
    public string Image { get; }
    public string Name { get; }
}
