using System.Collections.Generic;
using DontDillyDally.Data;
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
public class RecipeItemEntry : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Image _background;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private RectTransform _iconContainer;

    [Header("아이콘 설정")]
    [SerializeField] private float _materialIconSize = 48f;
    [SerializeField] private float _actionIconSize = 20f;
    [SerializeField] private float _slotSpacing = 8f;

    private readonly List<MaterialSlot> _slotPool = new List<MaterialSlot>();

    public void SetData(
        string recipeName,
        List<CraftedMaterialType> materials,
        MaterialIconTable iconTable,
        Color textColor,
        Color bgColor,
        bool isBold,
        bool showSterilizedBadge)
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

    private void RefreshSlots(
        List<CraftedMaterialType> materials,
        MaterialIconTable iconTable,
        Color tintColor,
        bool showSterilizedBadge)
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
            SetSlot(
                slotIndex,
                iconTable.GetMaterialIcon(CraftedMaterialType.SterilizedTray),
                ActionType.None,
                null,
                tintColor);
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
                Sprite actionSprite = requiredAction != ActionType.None
                    ? iconTable.GetActionIcon(requiredAction)
                    : null;

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
        slot.MaterialIcon.color = tintColor;

        // 액션 아이콘
        bool hasAction = action != ActionType.None && actionSprite != null;
        slot.ActionIcon.gameObject.SetActive(hasAction);
        if (hasAction)
        {
            slot.ActionIcon.sprite = actionSprite;
            slot.ActionIcon.color = tintColor;
        }

        // 슬롯 높이 조정 (액션 있으면 더 높게)
        float slotHeight = hasAction
            ? _materialIconSize + _actionIconSize + 2f
            : _materialIconSize;
        slot.Root.sizeDelta = new Vector2(_materialIconSize, slotHeight);
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
        // 슬롯 루트 (재료 + 액션을 세로로 묶는 컨테이너)
        GameObject slotObj = new GameObject($"Slot_{index}", typeof(RectTransform), typeof(VerticalLayoutGroup));
        slotObj.layer = gameObject.layer;

        RectTransform slotRt = slotObj.GetComponent<RectTransform>();
        slotRt.SetParent(_iconContainer, false);
        slotRt.sizeDelta = new Vector2(_materialIconSize, _materialIconSize);

        VerticalLayoutGroup vlg = slotObj.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;
        vlg.spacing = 2f;

        // 재료 아이콘
        Image materialIcon = CreateIconImage(slotRt, $"Material_{index}", _materialIconSize);

        // 액션 아이콘 (기본 비활성화)
        Image actionIcon = CreateIconImage(slotRt, $"Action_{index}", _actionIconSize);
        actionIcon.gameObject.SetActive(false);

        return new MaterialSlot
        {
            Root = slotRt,
            MaterialIcon = materialIcon,
            ActionIcon = actionIcon
        };
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
        public Image MaterialIcon;
        public Image ActionIcon;
    }
}
