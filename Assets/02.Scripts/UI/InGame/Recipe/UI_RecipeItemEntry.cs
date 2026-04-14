using DontDillyDally.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DontDillyDally.UI
{
    public class UI_RecipeItemEntry : MonoBehaviour
    {
        [Header("프리팹")]
        [SerializeField] private UI_ItemFrame _itemFramePrefab;
        [SerializeField] private GameObject _plusIconPrefab;

        [Header("컨테이너")]
        [SerializeField] private RectTransform _itemContainer;

        [Header("선택 참조 (기존 호환)")]
        [SerializeField] private Image _frame;
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _countText;
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
        private ActionType _actionType;
        private int _requiredCount;
        private int _ownedCount;
        private MaterialState _state;

        private UI_ItemFrame _materialFrame;
        private UI_ItemFrame _actionFrame;

        public enum MaterialState
        {
            Normal,
            Lack,
            Complete,
            Highlighted,
            Disabled
        }

        public CraftedMaterialType MaterialType => _materialType;
        public ActionType ActionType => _actionType;
        public int RequiredCount => _requiredCount;
        public int OwnedCount => _ownedCount;
        public MaterialState State => _state;
        public bool HasAction => _actionType != ActionType.None;

        /// <summary>
        /// 재료 데이터를 설정합니다. 액션 아이콘이 있으면 재료 + 플러스 + 액션 구조로 표시합니다.
        /// </summary>
        public void SetData(CraftedMaterialType materialType, Sprite materialIcon, int requiredCount,
            ActionType actionType = ActionType.None, Sprite actionIcon = null, string displayName = null)
        {
            _materialType = materialType;
            _actionType = actionType;
            _requiredCount = requiredCount;

            ClearDynamicItems();

            if (_itemContainer != null && _itemFramePrefab != null)
            {
                BuildDynamicLayout(materialIcon, actionType, actionIcon);
            }
            else
            {
                SetLegacyData(materialIcon, requiredCount, displayName);
            }

            SetState(MaterialState.Normal);
        }

        /// <summary>
        /// 기존 호환용 SetData (액션 없음).
        /// </summary>
        public void SetData(CraftedMaterialType materialType, Sprite icon, int requiredCount, string displayName = null)
        {
            SetData(materialType, icon, requiredCount, ActionType.None, null, displayName);
        }

        private void BuildDynamicLayout(Sprite materialIcon, ActionType actionType, Sprite actionIcon)
        {
            _materialFrame = Instantiate(_itemFramePrefab, _itemContainer);
            _materialFrame.gameObject.name = "MaterialFrame";
            _materialFrame.SetIcon(materialIcon);

            if (actionType != ActionType.None && actionIcon != null)
            {
                _actionFrame = Instantiate(_itemFramePrefab, _itemContainer);
                _actionFrame.gameObject.name = "ActionFrame";
                _actionFrame.SetIcon(actionIcon);
            }

            if (_countText != null)
            {
                _countText.text = _requiredCount > 1 ? $"x{_requiredCount}" : "";
            }
        }

        private void SetLegacyData(Sprite icon, int requiredCount, string displayName)
        {
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
                _nameText.text = displayName ?? _materialType.ToString();
            }
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

            if (_materialFrame != null)
            {
                _materialFrame.SetFrameColor(frameColor);
            }

            if (_actionFrame != null)
            {
                _actionFrame.SetFrameColor(frameColor);
            }

            if (_completeMark != null)
            {
                _completeMark.SetActive(_state == MaterialState.Complete);
            }

            if (_lackOverlay != null)
            {
                _lackOverlay.enabled = _state == MaterialState.Lack;
            }

            bool isDisabled = _state == MaterialState.Disabled;
            Color iconColor = isDisabled ? new Color(1f, 1f, 1f, 0.5f) : Color.white;

            if (_icon != null)
            {
                _icon.color = iconColor;
            }

            if (_materialFrame != null)
            {
                _materialFrame.SetDisabled(isDisabled);
            }

            if (_actionFrame != null)
            {
                _actionFrame.SetDisabled(isDisabled);
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

        private void ClearDynamicItems()
        {
            if (_materialFrame != null)
            {
                Destroy(_materialFrame.gameObject);
                _materialFrame = null;
            }

            if (_actionFrame != null)
            {
                Destroy(_actionFrame.gameObject);
                _actionFrame = null;
            }
        }

        private void OnDestroy()
        {
            ClearDynamicItems();
        }
    }
}
