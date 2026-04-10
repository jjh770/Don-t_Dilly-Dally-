using UnityEngine;
using UnityEngine.UI;

namespace DontDillyDally.UI
{
    /// <summary>
    /// 하나의 재료 또는 액션 아이콘을 표현하는 최소 단위 UI입니다.
    /// </summary>
    public class UI_ItemFrame : MonoBehaviour
    {
        [Header("필수 참조")]
        [SerializeField] private Image _frameImage;
        [SerializeField] private Image _iconImage;

        [Header("상태 색상")]
        [SerializeField] private Color _normalFrameColor = Color.white;
        [SerializeField] private Color _completeFrameColor = new Color(0.8f, 1f, 0.8f, 1f);
        [SerializeField] private Color _disabledFrameColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        private bool _isComplete;

        public Image FrameImage => _frameImage;
        public Image IconImage => _iconImage;
        public bool IsComplete => _isComplete;

        public void SetIcon(Sprite icon)
        {
            if (_iconImage == null)
            {
                return;
            }

            _iconImage.sprite = icon;
            _iconImage.enabled = icon != null;
        }

        public void SetComplete(bool complete)
        {
            _isComplete = complete;

            if (_frameImage != null)
            {
                _frameImage.color = complete ? _completeFrameColor : _normalFrameColor;
            }

            if (_iconImage != null)
            {
                _iconImage.color = Color.white;
            }
        }

        public void SetDisabled(bool disabled)
        {
            if (_frameImage != null)
            {
                _frameImage.color = disabled ? _disabledFrameColor : _normalFrameColor;
            }

            if (_iconImage != null)
            {
                _iconImage.color = disabled ? new Color(1f, 1f, 1f, 0.5f) : Color.white;
            }
        }

        public void SetFrameColor(Color color)
        {
            if (_frameImage != null)
            {
                _frameImage.color = color;
            }
        }
    }
}
