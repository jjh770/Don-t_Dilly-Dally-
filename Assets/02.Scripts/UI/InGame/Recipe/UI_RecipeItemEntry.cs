using DontDillyDally.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DontDillyDally.UI
{
    /// <summary>
    /// 재료 1개 단위 UI입니다.
    /// Frame 안에 아이콘, 수량, 상태 정보를 배치합니다.
    /// </summary>
    public class UI_RecipeItemEntry : MonoBehaviour
    {
        [Header("필수 참조")]
        [SerializeField] private Image _frame;
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _countText;

        [Header("선택 참조")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _ownedCountText;
        [SerializeField] private GameObject _completeMark;
        [SerializeField] private Image _lackOverlay;

        [Header("상태 색상")]
        [SerializeField] private Color _normalFrameColor = Color.white;
        [SerializeField] private Color _lackFrameColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        [SerializeField] private Color _completeFrameColor = new Color(0.8f, 1f, 0.8f, 1f);
        [SerializeField] private Color _highlightFrameColor = new Color(1f, 0.9f, 0.5f, 1f);
        [SerializeField] private Color _disabledFrameColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        private CraftedMaterialType _materialType;
        private int _requiredCount;
        private int _ownedCount;
        private MaterialState _state;

        public enum MaterialState
        {
            Normal,
            Lack,
            Complete,
            Highlighted,
            Disabled
        }

        public CraftedMaterialType MaterialType => _materialType;
        public int RequiredCount => _requiredCount;
        public int OwnedCount => _ownedCount;
        public MaterialState State => _state;

        public void SetData(CraftedMaterialType materialType, Sprite icon, int requiredCount, string displayName = null)
        {
            _materialType = materialType;
            _requiredCount = requiredCount;

            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.enabled = icon != null;
            }

            if (_countText != null)
            {
                _countText.text = requiredCount > 1 ? $"x{requiredCount}" : "";
            }

            if (_nameText != null)
            {
                _nameText.text = displayName ?? materialType.ToString();
            }

            SetState(MaterialState.Normal);
        }

        public void SetOwnedCount(int ownedCount)
        {
            _ownedCount = ownedCount;

            if (_ownedCountText != null)
            {
                _ownedCountText.text = $"{ownedCount}/{_requiredCount}";
            }

            if (ownedCount >= _requiredCount)
            {
                SetState(MaterialState.Complete);
            }
            else if (ownedCount > 0)
            {
                SetState(MaterialState.Normal);
            }
            else
            {
                SetState(MaterialState.Lack);
            }
        }

        public void SetState(MaterialState state)
        {
            _state = state;
            ApplyState();
        }

        private void ApplyState()
        {
            Color frameColor = _state switch
            {
                MaterialState.Lack => _lackFrameColor,
                MaterialState.Complete => _completeFrameColor,
                MaterialState.Highlighted => _highlightFrameColor,
                MaterialState.Disabled => _disabledFrameColor,
                _ => _normalFrameColor
            };

            if (_frame != null)
            {
                _frame.color = frameColor;
            }

            if (_completeMark != null)
            {
                _completeMark.SetActive(_state == MaterialState.Complete);
            }

            if (_lackOverlay != null)
            {
                _lackOverlay.enabled = _state == MaterialState.Lack;
            }

            if (_icon != null)
            {
                _icon.color = _state == MaterialState.Disabled
                    ? new Color(1f, 1f, 1f, 0.5f)
                    : Color.white;
            }
        }

        public void SetHighlighted(bool highlighted)
        {
            if (highlighted && _state != MaterialState.Complete)
            {
                SetState(MaterialState.Highlighted);
            }
            else if (!highlighted && _state == MaterialState.Highlighted)
            {
                SetOwnedCount(_ownedCount);
            }
        }
    }
}
