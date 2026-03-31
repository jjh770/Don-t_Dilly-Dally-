using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VoiceOverlaySlot : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nicknameText;
    [SerializeField] private Image _muteImage;

    [Header("State Visuals")]
    [SerializeField, Range(0f, 1f)] private float _idleAlpha = 0.45f;
    [SerializeField, Range(0f, 1f)] private float _speakingAlpha = 1f;

    private bool _isInitialized;
    private Color _baseTextColor = Color.white;
    private Color _iconBaseColor = Color.white;

    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _iconBaseColor = _iconImage.color;
        _baseTextColor = _nicknameText.color;
        ApplySpeakingVisuals(false, _baseTextColor);
        ApplyMuteVisuals(false);
        _isInitialized = true;
    }

    public void SetIdentityContent(string nickname, Sprite iconSprite)
    {
        Initialize();

        gameObject.SetActive(true);
        _nicknameText.text = nickname;
        ApplyIcon(iconSprite);
    }

    public void SetTextColor(Color textColor)
    {
        Initialize();

        _baseTextColor = textColor;
        ApplySpeakingVisuals(false, _baseTextColor);
    }

    public void SetVoiceState(bool isSpeaking, bool isMuted)
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        ApplySpeakingVisuals(isSpeaking, _baseTextColor);
        ApplyMuteVisuals(isMuted);
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

    private void ApplySpeakingVisuals(bool isSpeaking, Color textColor)
    {
        float alpha = isSpeaking ? _speakingAlpha : _idleAlpha;
        _nicknameText.color = WithAlpha(textColor, alpha);
        _iconImage.color = WithAlpha(_iconBaseColor, alpha);
    }

    private void ApplyMuteVisuals(bool isMuted)
    {
        if (_muteImage == null)
        {
            return;
        }

        _muteImage.enabled = isMuted;
        _muteImage.gameObject.SetActive(isMuted);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
