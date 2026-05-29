public readonly struct LevelXpData
{
    public LevelXpData(int level, int needXp)
    {
        Level = level;
        NeedXp = needXp;
    }

    public int Level { get; }
    public int NeedXp { get; }
}
