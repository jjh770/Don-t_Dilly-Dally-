using System;
using UnityEngine;

[Serializable]
public class CommentaryClipData
{
    public string Subtitle;
    public AudioClip Clip;
}

[Serializable]
public class EventTypeClipGroup
{
    public EventType EventType;
    public CommentaryClipData[] ClipData;
}
