public static class StageSelection
{
    public static int SelectedStageId { get; private set; } = 1;
    public static bool HasSelectedStage { get; private set; }

    public static void SelectStage(int stageId)
    {
        SelectedStageId = stageId;
        HasSelectedStage = true;
    }

    public static void Clear()
    {
        SelectedStageId = 1;
        HasSelectedStage = false;
    }
}
