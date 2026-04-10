using UnityEngine;

[CreateAssetMenu(menuName = "Tutorial/SlideData")]
public class UI_SlideData : ScriptableObject
{
    public enum Layout { ImageLeft, ImageRight, TextOnly }

    public Layout layout;
    public string stepLabel;   
    public string title;
    public string description;
    public Sprite image;        // null이면 TextOnly 취급.
}