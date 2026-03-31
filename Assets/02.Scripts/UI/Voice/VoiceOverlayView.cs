using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class VoiceOverlayView : MonoBehaviour
{
    private readonly VoiceOverlaySlot[] _slots = new VoiceOverlaySlot[4];

    [SerializeField] private RectTransform _slotParent;
    [SerializeField] private VoiceOverlaySlot _slotTemplate;
    private bool _hasLoggedMissingSlotPrefab;
    private bool _isInitialized;
    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _slotParent ??= transform as RectTransform;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    public void Initialize(int slotCount)
    {
        if (_isInitialized)
        {
            return;
        }

        EnsureSlots(slotCount);
        HideAll();
        _isInitialized = true;
    }

    public void SetVisible(bool isVisible)
    {
        _canvasGroup ??= GetComponent<CanvasGroup>();

        _canvasGroup.alpha = isVisible ? 1f : 0f;
    }

    public void SetSlotIdentityContent(int index, string nickname, Sprite iconSprite)
    {
        if (!IsValidIndex(index))
        {
            return;
        }

        _slots[index].SetIdentityContent(nickname, iconSprite);
    }

    public void SetSlotTextColor(int index, Color textColor)
    {
        if (!IsValidIndex(index))
        {
            return;
        }

        _slots[index].SetTextColor(textColor);
    }

    public void SetSlotVoiceState(int index, bool isSpeaking, bool isMuted)
    {
        if (!IsValidIndex(index))
        {
            return;
        }

        _slots[index].SetVoiceState(isSpeaking, isMuted);
    }

    public void HideSlot(int index)
    {
        if (!IsValidIndex(index))
        {
            return;
        }

        _slots[index].SetVisible(false);
    }

    public void HideAll()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null)
            {
                _slots[i].SetVisible(false);
            }
        }
    }

    private void EnsureSlots(int slotCount)
    {
        List<VoiceOverlaySlot> resolvedSlots = CollectExistingSlots();
        for (int i = 0; i < slotCount && i < _slots.Length; i++)
        {
            VoiceOverlaySlot slotView = i < resolvedSlots.Count
                ? resolvedSlots[i]
                : CreateSlot(i);

            _slots[i] = slotView;

            if (_slots[i] != null)
            {
                _slots[i].Initialize();
                _slots[i].SetVisible(false);
            }
        }
    }

    private List<VoiceOverlaySlot> CollectExistingSlots()
    {
        List<VoiceOverlaySlot> slots = new List<VoiceOverlaySlot>();
        if (_slotParent == null)
        {
            return slots;
        }

        foreach (Transform child in _slotParent)
        {
            VoiceOverlaySlot slotView = child.GetComponent<VoiceOverlaySlot>();
            if (slotView != null)
            {
                slots.Add(slotView);
            }
        }

        return slots;
    }

    private VoiceOverlaySlot CreateSlot(int index)
    {
        if (_slotParent == null)
        {
            return null;
        }

        VoiceOverlaySlot slotView = null;
        if (_slotTemplate != null)
        {
            slotView = Instantiate(_slotTemplate, _slotParent);
            slotView.name = $"{_slotTemplate.gameObject.name}_{index + 1}";
        }
        else
        {
            if (!_hasLoggedMissingSlotPrefab)
            {
                Debug.LogWarning("[VoiceOverlayView] Slot template is not assigned. Additional voice overlay slots cannot be created.");
                _hasLoggedMissingSlotPrefab = true;
            }

            return null;
        }

        slotView.gameObject.SetActive(true);
        _hasLoggedMissingSlotPrefab = false;

        return slotView;
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < _slots.Length && _slots[index] != null;
    }
}
