using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UIOutline = UnityEngine.UI.Outline;

public class VoiceOverlaySlot : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nicknameText;
    [SerializeField] private UIOutline _iconOutline;

    [Header("State Visuals")]
    [SerializeField, Range(0f, 1f)] private float _idleAlpha = 0.45f;
    [SerializeField, Range(0f, 1f)] private float _speakingAlpha = 1f;
    [SerializeField] private Color _iconOutlineColor = Color.white;
    [SerializeField] private Vector2 _iconOutlineDistance = new Vector2(2f, -2f);

    private bool _isInitialized;
    private Color _iconBaseColor = Color.white;

    public void Initialize(Sprite iconSprite)
    {
        if (_isInitialized)
        {
            return;
        }

        _iconBaseColor = _iconImage.color;
        ApplyIcon(iconSprite);
        ApplySpeakingVisuals(false);
        _isInitialized = true;
    }

    public void SetState(string nickname, Color textColor, bool isSpeaking, Sprite iconSprite)
    {
        Initialize(iconSprite);

        gameObject.SetActive(true);
        _nicknameText.text = nickname;

        ApplyIcon(iconSprite);
        ApplySpeakingVisuals(isSpeaking, textColor);
    }

    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    private void ApplyIcon(Sprite iconSprite)
    {
        _iconImage.sprite = iconSprite;
        _iconImage.enabled = iconSprite != null;
    }

    private void ApplySpeakingVisuals(bool isSpeaking)
    {
        ApplySpeakingVisuals(isSpeaking, _nicknameText.color);
    }

    private void ApplySpeakingVisuals(bool isSpeaking, Color textColor)
    {
        float alpha = isSpeaking ? _speakingAlpha : _idleAlpha;
        _nicknameText.color = WithAlpha(textColor, alpha);
        _iconImage.color = WithAlpha(_iconBaseColor, alpha);

        _iconOutline.enabled = isSpeaking;
        _iconOutline.effectColor = _iconOutlineColor;
        _iconOutline.effectDistance = _iconOutlineDistance;
        _iconOutline.useGraphicAlpha = true;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
