using DontDillyDally.Data;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레시피 리스트의 개별 항목 UI입니다.
/// 프리팹으로 만들어서 StageCurrentRecipeUI에 연결합니다.
///
/// 각 재료 슬롯 구조:
///   ┌──────────┐
///   │ 재료아이콘 │
///   ├──────────┤
///   │ 액션아이콘 │  ← 멸균/Fill/Mix 등 필요 시에만 표시
///   └──────────┘
/// </summary>
public class UI_RecipeItemEntry : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Image _background;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private RectTransform _iconContainer;

    [Header("아이콘 설정")]
    [SerializeField] private Sprite _iconFrameSprite;
    [SerializeField] private float _frameSize = 80f;
    [SerializeField] private float _materialIconSize = 60f;
    [SerializeField] private float _actionIconSize = 40f;
    [SerializeField] private float _slotSpacing = 5f;

    private readonly List<MaterialSlot> _slotPool = new List<MaterialSlot>();

    public void SetData(string recipeName, List<CraftedMaterialType> materials, MaterialIconTable iconTable, Color textColor, Color bgColor, bool isBold, bool showSterilizedBadge)
    {
        if (_nameText != null)
        {
            _nameText.text = recipeName;
            _nameText.color = textColor;
            _nameText.fontStyle = isBold ? FontStyles.Bold : FontStyles.Normal;
        }

        if (_background != null)
        {
            _background.color = bgColor;
        }

        RefreshSlots(materials, iconTable, textColor, showSterilizedBadge);
    }

    private void RefreshSlots(List<CraftedMaterialType> materials, MaterialIconTable iconTable, Color tintColor, bool showSterilizedBadge)
    {
        if (_iconContainer == null || iconTable == null)
        {
            return;
        }

        // 필요한 슬롯 수 (SterilizedTray 제외 + 멸균 뱃지)
        int neededCount = 0;

        if (materials != null)
        {
            for (int i = 0; i < materials.Count; i++)
            {
                if (materials[i] != CraftedMaterialType.SterilizedTray)
                {
                    neededCount++;
                }
            }
        }

        if (showSterilizedBadge)
        {
            neededCount++;
        }

        EnsureSlotPoolSize(neededCount);

        int slotIndex = 0;

        // 멸균 트레이 뱃지
        if (showSterilizedBadge)
        {
            SetSlot(slotIndex, iconTable.GetMaterialIcon(CraftedMaterialType.SterilizedTray), ActionType.Sterilize, iconTable.GetActionIcon(ActionType.Sterilize), tintColor);
            slotIndex++;
        }

        // 재료 슬롯
        if (materials != null)
        {
            for (int i = 0; i < materials.Count; i++)
            {
                CraftedMaterialType material = materials[i];
                if (material == CraftedMaterialType.SterilizedTray)
                {
                    continue;
                }

                ActionType requiredAction = iconTable.GetRequiredAction(material);
                Sprite actionSprite = requiredAction != ActionType.None ? iconTable.GetActionIcon(requiredAction) : null;

                SetSlot(slotIndex, iconTable.GetMaterialIcon(material), requiredAction, actionSprite, tintColor);
                slotIndex++;
            }
        }

        // 남는 슬롯 비활성화
        for (int i = slotIndex; i < _slotPool.Count; i++)
        {
            _slotPool[i].Root.gameObject.SetActive(false);
        }
    }

    private void SetSlot(int index, Sprite materialSprite, ActionType action, Sprite actionSprite, Color tintColor)
    {
        MaterialSlot slot = _slotPool[index];
        slot.Root.gameObject.SetActive(true);

        // 재료 아이콘
        slot.MaterialIcon.sprite = materialSprite;
        slot.MaterialIcon.color = Color.white;

        // 액션 아이콘 (프레임 포함)
        bool hasAction = action != ActionType.None && actionSprite != null;
        slot.ActionFrameImage.gameObject.SetActive(hasAction);
        if (hasAction)
        {
            slot.ActionIcon.sprite = actionSprite;
            slot.ActionIcon.color = Color.white;
        }

        // 슬롯 높이 조정 (프레임 + 액션)
        float slotHeight = hasAction
            ? _frameSize * 2 + _slotSpacing
            : _frameSize;
        slot.Root.sizeDelta = new Vector2(_frameSize, slotHeight);
        slot.LayoutElement.preferredHeight = slotHeight;
    }

    // ================================================================
    //  슬롯 풀 관리
    // ================================================================

    private void EnsureSlotPoolSize(int count)
    {
        while (_slotPool.Count < count)
        {
            MaterialSlot slot = CreateSlot(_slotPool.Count);
            _slotPool.Add(slot);
        }
    }

    private MaterialSlot CreateSlot(int index)
    {
        // 슬롯 루트 (프레임 + 액션을 세로로 묶는 컨테이너)
        GameObject slotObj = new GameObject($"Slot_{index}", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        slotObj.layer = gameObject.layer;

        RectTransform slotRt = slotObj.GetComponent<RectTransform>();
        slotRt.SetParent(_iconContainer, false);
        slotRt.sizeDelta = new Vector2(_frameSize, _frameSize);

        VerticalLayoutGroup vlg = slotObj.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;
        vlg.spacing = _slotSpacing;

        LayoutElement layoutElement = slotObj.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = _frameSize;
        layoutElement.preferredHeight = _frameSize;

        // 아이콘 프레임 (배경 + 재료 아이콘 포함)
        Image frameImage = CreateFrameWithIcon(slotRt, $"Frame_{index}", _frameSize, _materialIconSize, out Image materialIcon);

        // 액션 아이콘 프레임 (프레임 아래, 기본 비활성화)
        Image actionFrameImage = CreateFrameWithIcon(slotRt, $"ActionFrame_{index}", _frameSize, _materialIconSize, out Image actionIcon);
        actionFrameImage.gameObject.SetActive(false);

        return new MaterialSlot
        {
            Root = slotRt,
            LayoutElement = layoutElement,
            FrameImage = frameImage,
            MaterialIcon = materialIcon,
            ActionFrameImage = actionFrameImage,
            ActionIcon = actionIcon
        };
    }

    private Image CreateFrameWithIcon(RectTransform parent, string objectName, float frameSize, float iconSize, out Image innerIcon)
    {
        // 프레임 오브젝트
        GameObject frameObj = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        frameObj.layer = gameObject.layer;

        RectTransform frameRt = frameObj.GetComponent<RectTransform>();
        frameRt.SetParent(parent, false);
        frameRt.sizeDelta = new Vector2(frameSize, frameSize);

        Image frameImage = frameObj.GetComponent<Image>();
        frameImage.sprite = _iconFrameSprite;
        frameImage.type = Image.Type.Sliced;
        frameImage.raycastTarget = false;

        // 내부 아이콘 (프레임 안에 중앙 배치)
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.layer = gameObject.layer;

        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.SetParent(frameRt, false);
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = Vector2.zero;
        iconRt.sizeDelta = new Vector2(iconSize, iconSize);

        innerIcon = iconObj.GetComponent<Image>();
        innerIcon.preserveAspect = true;
        innerIcon.raycastTarget = false;

        return frameImage;
    }

    private Image CreateIconImage(RectTransform parent, string objectName, float size)
    {
        GameObject iconObj = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        iconObj.layer = gameObject.layer;

        RectTransform rt = iconObj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(size, size);

        Image image = iconObj.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;

        return image;
    }

    private struct MaterialSlot
    {
        public RectTransform Root;
        public LayoutElement LayoutElement;
        public Image FrameImage;
        public Image MaterialIcon;
        public Image ActionFrameImage;
        public Image ActionIcon;
    }
}
