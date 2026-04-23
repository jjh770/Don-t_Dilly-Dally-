using System;
using UnityEngine;
using UnityEngine.UI;

namespace DontDillyDally.Data
{
    // 멸균기 위에 떠 있는 슬롯 아이콘 UI입니다.
    // Canvas 는 Screen Space Overlay 이며 기계의 월드 위치를 매 프레임 스크린 좌표로 변환해 따라갑니다.
    // 개수별로 미리 배치된 레이아웃 GameObject 를 토글해서 "한 칸씩 늘어나는" 연출을 만듭니다.
    [DisallowMultipleComponent]
    public class SterilizationMachineWorldUI : MonoBehaviour
    {
        [Serializable]
        private class LayoutEntry
        {
            public GameObject Root;
            public Image[] Icons;
        }

        [Header("References")]
        [SerializeField] private SterilizationMachineInteractable _machine;
        [SerializeField] private GameObject _root;
        [SerializeField] private RectTransform _positionRoot;
        [SerializeField] private Transform _worldAnchor;
        [SerializeField] private Vector3 _worldOffset = Vector3.up;
        [SerializeField] private Camera _targetCamera;

        [Header("Icon Mapping")]
        [SerializeField] private MaterialIconTable _iconTable;
        [SerializeField] private Sprite _normalTrayIcon;
        [SerializeField] private Sprite _sterilizedTrayIcon;

        [Header("Layouts (index = occupiedCount - 1)")]
        [Tooltip("0:1개용, 1:2개용, 2:3개용, 3:4개용 레이아웃입니다.")]
        [SerializeField] private LayoutEntry[] _layouts = new LayoutEntry[4];

        private CanvasGroup _canvasGroup;
        private Camera _cachedTargetCamera;
        private int _lastActiveLayoutIndex = -1;

        private void Awake()
        {
            if (_machine == null)
            {
                _machine = GetComponentInParent<SterilizationMachineInteractable>();
            }

            if (_root == null)
            {
                _root = gameObject;
            }

            if (_positionRoot == null)
            {
                _positionRoot = _root.GetComponent<RectTransform>();
            }

            if (_worldAnchor == null && _machine != null)
            {
                _worldAnchor = _machine.transform;
            }

            _canvasGroup = _root.GetComponent<CanvasGroup>();

            DeactivateAllLayouts();
            SetVisible(false);
        }

        private void Update()
        {
            if (_machine == null)
            {
                SetVisible(false);
                return;
            }

            bool hasContent = _machine.HasAnyStoredItem && !_machine.IsInteracting;
            if (!hasContent)
            {
                SetVisible(false);
                if (_lastActiveLayoutIndex != -1)
                {
                    DeactivateAllLayouts();
                    _lastActiveLayoutIndex = -1;
                }

                return;
            }

            if (!UpdateOverlayPosition())
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            RefreshSlots();
        }

        private bool UpdateOverlayPosition()
        {
            if (_positionRoot == null || _worldAnchor == null)
            {
                return true;
            }

            Camera targetCamera = GetTargetCamera();
            if (targetCamera == null)
            {
                return true;
            }

            Vector3 worldPosition = _worldAnchor.position + _worldOffset;
            Vector3 screenPosition = targetCamera.WorldToScreenPoint(worldPosition);
            if (screenPosition.z < 0f)
            {
                return false;
            }

            _positionRoot.position = screenPosition;
            return true;
        }

        private Camera GetTargetCamera()
        {
            if (_targetCamera != null)
            {
                return _targetCamera;
            }

            if (_cachedTargetCamera == null)
            {
                _cachedTargetCamera = Camera.main;
            }

            return _cachedTargetCamera;
        }

        private void RefreshSlots()
        {
            int occupiedCount = _machine.OccupiedSlotCount;
            if (occupiedCount <= 0 || _layouts == null)
            {
                DeactivateAllLayouts();
                _lastActiveLayoutIndex = -1;
                return;
            }

            int desiredIndex = occupiedCount - 1;
            if (desiredIndex >= _layouts.Length)
            {
                // 레이아웃 개수가 부족하면 안전하게 숨깁니다.
                DeactivateAllLayouts();
                _lastActiveLayoutIndex = -1;
                return;
            }

            if (desiredIndex != _lastActiveLayoutIndex)
            {
                SwitchLayout(desiredIndex);
                _lastActiveLayoutIndex = desiredIndex;
            }

            LayoutEntry activeLayout = _layouts[desiredIndex];
            if (activeLayout == null || activeLayout.Icons == null)
            {
                return;
            }

            ApplySlotIcons(activeLayout.Icons);
        }

        private void ApplySlotIcons(Image[] icons)
        {
            int displayIndex = 0;
            int slotCount = _machine.SlotCount;

            for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                SterilizationSlotDisplayInfo info = _machine.GetSlotDisplayInfo(slotIndex);
                if (info.Kind == SterilizationSlotDisplayKind.Empty)
                {
                    continue;
                }

                if (displayIndex >= icons.Length)
                {
                    break;
                }

                Image target = icons[displayIndex];
                if (target != null)
                {
                    ApplyIcon(target, info);
                }

                displayIndex++;
            }

            // 레이아웃 슬롯보다 점유 슬롯이 적으면 남은 Image 는 비활성화합니다.
            for (int i = displayIndex; i < icons.Length; i++)
            {
                Image leftover = icons[i];
                if (leftover == null)
                {
                    continue;
                }

                if (leftover.enabled)
                {
                    leftover.enabled = false;
                }
            }
        }

        private void ApplyIcon(Image target, SterilizationSlotDisplayInfo info)
        {
            Sprite sprite = ResolveSprite(info);
            if (target.sprite != sprite)
            {
                target.sprite = sprite;
            }

            bool hasSprite = sprite != null;
            if (target.enabled != hasSprite)
            {
                target.enabled = hasSprite;
            }
        }

        private Sprite ResolveSprite(SterilizationSlotDisplayInfo info)
        {
            switch (info.Kind)
            {
                case SterilizationSlotDisplayKind.Material:
                    return _iconTable != null ? _iconTable.GetMaterialIcon(info.Material) : null;
                case SterilizationSlotDisplayKind.Tray:
                    return info.TrayKind == TrayKind.Sterilized ? _sterilizedTrayIcon : _normalTrayIcon;
                default:
                    return null;
            }
        }

        private void SwitchLayout(int activeIndex)
        {
            for (int i = 0; i < _layouts.Length; i++)
            {
                LayoutEntry entry = _layouts[i];
                if (entry == null || entry.Root == null)
                {
                    continue;
                }

                bool shouldActivate = i == activeIndex;
                if (entry.Root.activeSelf != shouldActivate)
                {
                    entry.Root.SetActive(shouldActivate);
                }
            }
        }

        private void DeactivateAllLayouts()
        {
            if (_layouts == null)
            {
                return;
            }

            for (int i = 0; i < _layouts.Length; i++)
            {
                LayoutEntry entry = _layouts[i];
                if (entry == null || entry.Root == null)
                {
                    continue;
                }

                if (entry.Root.activeSelf)
                {
                    entry.Root.SetActive(false);
                }
            }
        }

        private void SetVisible(bool visible)
        {
            if (_root == null)
            {
                return;
            }

            if (_canvasGroup != null)
            {
                float targetAlpha = visible ? 1f : 0f;
                if (!Mathf.Approximately(_canvasGroup.alpha, targetAlpha))
                {
                    _canvasGroup.alpha = targetAlpha;
                }

                if (_canvasGroup.interactable != visible)
                {
                    _canvasGroup.interactable = visible;
                }

                if (_canvasGroup.blocksRaycasts != visible)
                {
                    _canvasGroup.blocksRaycasts = visible;
                }

                return;
            }

            if (_root.activeSelf != visible)
            {
                _root.SetActive(visible);
            }
        }
    }
}
