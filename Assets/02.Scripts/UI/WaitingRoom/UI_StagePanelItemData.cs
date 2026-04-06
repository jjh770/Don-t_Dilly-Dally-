using UnityEngine;

public readonly struct UI_StagePanelItemData
{
    public UI_StagePanelItemData(
        int stageIndex,
        int stageNumber,
        string stageName,
        string description,
        Sprite stageThumbnail,
        bool isAvailable)
    {
        StageIndex = stageIndex;
        StageNumber = stageNumber;
        StageName = stageName;
        Description = description;
        StageThumbnail = stageThumbnail;
        IsAvailable = isAvailable;
    }

    public int StageIndex { get; }
    public int StageNumber { get; }
    public string StageName { get; }
    public string Description { get; }
    public Sprite StageThumbnail { get; }
    public bool IsAvailable { get; }
}
