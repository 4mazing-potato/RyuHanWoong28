public readonly struct LevelUpCardData
{
    public LevelUpCardData(int id, float ratio, int? required, string icon, string desc, LevelUpCardEffect effect, float? value)
    {
        ID = id;
        Ratio = ratio;
        Required = required;
        Icon = icon;
        Desc = desc;
        Effect = effect;
        Value = value;
    }

    public int ID { get; }
    public float Ratio { get; }
    public int? Required { get; }
    public string Icon { get; }
    public string Desc { get; }
    public LevelUpCardEffect Effect { get; }
    public float? Value { get; }
}
