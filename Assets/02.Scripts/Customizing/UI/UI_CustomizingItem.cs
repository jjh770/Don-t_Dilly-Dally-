using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 커스터마이징 아이템 버튼 UI
/// 아이콘, 선택 상태, 클릭 이벤트 처리
/// </summary>
public class UI_CustomizingItem : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject selectedIndicator;
    [SerializeField] private Button button;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(0.8f, 0.9f, 1f);

    // 연결된 아이템 데이터
    private CustomizingItemSO itemData;
    private Action onClick;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(HandleClick);
    }

    /// <summary>
    /// 버튼 초기화
    /// </summary>
    public void Setup(CustomizingItemSO item, Action onClickCallback)
    {
        itemData = item;
        onClick = onClickCallback;

        // 아이콘 설정
        if (iconImage != null && item.PreviewIcon != null)
        {
            iconImage.sprite = item.PreviewIcon;
            iconImage.color = Color.white;
        }

        // 초기 선택 상태
        SetSelected(false);
    }

    /// <summary>
    /// 선택 상태 설정
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (selectedIndicator != null)
            selectedIndicator.SetActive(selected);

        if (backgroundImage != null)
            backgroundImage.color = selected ? selectedColor : normalColor;
    }

    /// <summary>
    /// 버튼 클릭 처리
    /// </summary>
    private void HandleClick()
    {
        onClick?.Invoke();
    }

    /// <summary>
    /// 연결된 아이템 데이터
    /// </summary>
    public CustomizingItemSO ItemData => itemData;
}
