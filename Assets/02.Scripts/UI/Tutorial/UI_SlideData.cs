using UnityEngine;

[CreateAssetMenu(menuName = "Tutorial/SlideData")]
public class UI_SlideData : ScriptableObject
{
    public enum Layout { ImageLeft, ImageRight, TextOnly }

    public Layout SlideLayout;
    public string StepLabel;   
    public string Title;
    public string Description;
    public Sprite Image;
}