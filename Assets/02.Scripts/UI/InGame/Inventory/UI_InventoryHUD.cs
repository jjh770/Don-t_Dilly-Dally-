using DontDillyDally.Data;
using DontDillyDally.StageFlow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DontDillyDally.UI
{
    public class UI_InventoryHUD : MonoBehaviour
    {
        private const float OpaqueAlpha = 1f;
        private const float TranslucentAlpha = 0.5f;

        [Header("UI References")]
        [SerializeField] private Image _itemImage;
        [SerializeField] private TextMeshProUGUI _primaryText;
        [SerializeField] private TextMeshProUGUI _secondaryText;

        [Header("Guide Text Colors")]
        [SerializeField] private Color _keyColor = Color.yellow;
        [SerializeField] private Color _actionColor = Color.white;

        [Header("Guide Text Keys")]
        [SerializeField] private string _pickupKey = "[E]";
        [SerializeField] private string _pushKey = "[E]";
        [SerializeField] private string _dropKey = "[E]";
        [SerializeField] private string _throwKey = "[좌클릭]";
        [SerializeField] private string _moveKey = "[WASD]";

        [Header("Guide Text Actions")]
        [SerializeField] private string _pickupAction = "집기";
        [SerializeField] private string _pushAction = "밀기";
        [SerializeField] private string _dropAction = "놓기";
        [SerializeField] private string _throwAction = "던지기";
        [SerializeField] private string _moveAction = "이동";

        [Header("Data")]
        [SerializeField] private MaterialIconTable _iconTable;
        [SerializeField] private EmergencyUiCatalogSO _emergencyUiCatalog;

        private PlayerInteractionAbility _playerInteraction;
        private IInteractable _cachedNearestInteractable;
        private bool _isInitialized;

        private void OnEnable()
        {
            if (TryInitialize())
            {
                return;
            }

            PlayerRegistry.OnPlayerRegistered += HandlePlayerRegistered;
        }

        private void OnDisable()
        {
            PlayerRegistry.OnPlayerRegistered -= HandlePlayerRegistered;

            if (_playerInteraction != null)
            {
                _playerInteraction.OnNearestInteractableChanged -= HandleNearestInteractableChanged;
                _playerInteraction.OnHeldItemChanged -= HandleHeldItemChanged;
                _playerInteraction.OnPushStateChanged -= HandlePushStateChanged;
            }

            _playerInteraction = null;
            _isInitialized = false;
        }

        private void HandlePlayerRegistered(PlayerController player)
        {
            TryInitialize();
        }

        private bool TryInitialize()
        {
            if (_isInitialized)
            {
                return true;
            }

            if (!PlayerRegistry.TryGetLocalPlayer(out PlayerController localPlayer))
            {
                return false;
            }

            PlayerInteractionAbility interaction = localPlayer.GetComponent<PlayerInteractionAbility>();
            if (interaction == null)
            {
                return false;
            }

            _playerInteraction = interaction;
            _isInitialized = true;

            _playerInteraction.OnNearestInteractableChanged += HandleNearestInteractableChanged;
            _playerInteraction.OnHeldItemChanged += HandleHeldItemChanged;
            _playerInteraction.OnPushStateChanged += HandlePushStateChanged;

            PlayerRegistry.OnPlayerRegistered -= HandlePlayerRegistered;
            UpdateState();
            return true;
        }

        private void HandleNearestInteractableChanged(IInteractable previous, IInteractable current)
        {
            _cachedNearestInteractable = current;
            UpdateState();
        }

        private void HandleHeldItemChanged(ItemObject heldItem)
        {
            UpdateState();
        }

        private void HandlePushStateChanged(bool isPushing)
        {
            UpdateState();
        }

        private void UpdateState()
        {
            InventoryHUDState newState;
            Sprite icon = null;

            // 1순위: 아이템 소지 상태
            if (_playerInteraction.CurrentHeldItem != null)
            {
                newState = InventoryHUDState.HoldingSmallItem;
                icon = GetItemIcon(_playerInteraction.CurrentHeldItem);
            }
            // 1순위: 큰 아이템 밀기 상태
            else if (_playerInteraction.IsPushingItem)
            {
                newState = InventoryHUDState.HoldingLargeItem;
                icon = GetPushableItemIcon();
            }
            // 2순위: 집기 가능 상태
            else if (_cachedNearestInteractable is IHoldable)
            {
                newState = InventoryHUDState.CanPickup;
                icon = GetInteractableIcon(_cachedNearestInteractable);
            }
            // 3순위: 밀기 가능 상태
            else if (_cachedNearestInteractable is IPushable)
            {
                newState = InventoryHUDState.CanPush;
                icon = GetPushableIcon(_cachedNearestInteractable);
            }
            // 4순위: 기본 상태
            else
            {
                newState = InventoryHUDState.Default;
            }

            ApplyState(newState, icon);
        }

        private void ApplyState(InventoryHUDState state, Sprite icon)
        {
            switch (state)
            {
                case InventoryHUDState.Default:
                    SetItemImage(false, null, OpaqueAlpha);
                    SetGuideTexts(null, null);
                    break;

                case InventoryHUDState.CanPickup:
                    SetItemImage(true, icon, TranslucentAlpha);
                    SetGuideTexts(FormatGuideText(_pickupKey, _pickupAction), null);
                    break;

                case InventoryHUDState.CanPush:
                    SetItemImage(true, icon, TranslucentAlpha);
                    SetGuideTexts(FormatGuideText(_pushKey, _pushAction), null);
                    break;

                case InventoryHUDState.HoldingSmallItem:
                    SetItemImage(true, icon, OpaqueAlpha);
                    SetGuideTexts(FormatGuideText(_dropKey, _dropAction), FormatGuideText(_throwKey, _throwAction));
                    break;

                case InventoryHUDState.HoldingLargeItem:
                    SetItemImage(true, icon, OpaqueAlpha);
                    SetGuideTexts(FormatGuideText(_dropKey, _dropAction), FormatGuideText(_moveKey, _moveAction));
                    break;
            }
        }

        private string FormatGuideText(string key, string action)
        {
            string keyHex = ColorUtility.ToHtmlStringRGB(_keyColor);
            string actionHex = ColorUtility.ToHtmlStringRGB(_actionColor);
            return $"<color=#{keyHex}>{key}</color> <color=#{actionHex}>{action}</color>";
        }

        private void SetItemImage(bool isActive, Sprite sprite, float alpha)
        {
            if (_itemImage == null)
            {
                return;
            }

            _itemImage.gameObject.SetActive(isActive);

            if (isActive && sprite != null)
            {
                _itemImage.sprite = sprite;
                Color color = _itemImage.color;
                color.a = alpha;
                _itemImage.color = color;
            }
        }

        private void SetGuideTexts(string primary, string secondary)
        {
            if (_primaryText != null)
            {
                bool hasPrimary = !string.IsNullOrEmpty(primary);
                _primaryText.gameObject.SetActive(hasPrimary);
                if (hasPrimary)
                {
                    _primaryText.text = primary;
                }
            }

            if (_secondaryText != null)
            {
                bool hasSecondary = !string.IsNullOrEmpty(secondary);
                _secondaryText.gameObject.SetActive(hasSecondary);
                if (hasSecondary)
                {
                    _secondaryText.text = secondary;
                }
            }
        }

        private Sprite GetItemIcon(ItemObject itemObject)
        {
            if (_iconTable == null || itemObject == null)
            {
                return null;
            }

            if (itemObject is BasicMaterialItem materialItem)
            {
                // 주사기 결과물은 Fill 아이콘 사용
                if (materialItem.MaterialType == CraftedMaterialType.AnestheticSyringe ||
                    materialItem.MaterialType == CraftedMaterialType.SedativeSyringe)
                {
                    return _iconTable.GetActionIcon(ActionType.Fill);
                }
                return _iconTable.GetMaterialIcon(materialItem.MaterialType);
            }

            if (itemObject is MixToolItem mixToolItem)
            {
                // 주사기는 ActionType.Fill 아이콘 사용
                if (mixToolItem.ToolType == ToolType.Syringe)
                {
                    return _iconTable.GetActionIcon(ActionType.Fill);
                }

                CraftedMaterialType mappedType = ConvertToolTypeToMaterial(mixToolItem.ToolType);
                if (mappedType != CraftedMaterialType.None)
                {
                    return _iconTable.GetMaterialIcon(mappedType);
                }
            }

            if (itemObject is TrayItem trayItem)
            {
                return _iconTable.GetMaterialIcon(CraftedMaterialType.SterilizedTray);
            }

            return null;
        }

        private static CraftedMaterialType ConvertToolTypeToMaterial(ToolType toolType)
        {
            return toolType switch
            {
                // 수술 도구
                ToolType.ScalpelGreen => CraftedMaterialType.SterilizedScalpelGreen,
                ToolType.ScalpelGray => CraftedMaterialType.SterilizedScalpelGray,
                ToolType.PincetteCurved => CraftedMaterialType.SterilizedPincetteCurved,
                ToolType.PincetteStraight => CraftedMaterialType.SterilizedPincetteStraight,
                ToolType.ScissorsSmall => CraftedMaterialType.SterilizedScissorsSmall,
                ToolType.ScissorsLarge => CraftedMaterialType.SterilizedScissorsLarge,
                ToolType.ScissorsClamp => CraftedMaterialType.SterilizedScissorsClamp,
                ToolType.BoneSaw => CraftedMaterialType.SterilizedBoneSaw,

                // 액체
                ToolType.AnestheticFluid => CraftedMaterialType.AnestheticSyringe,
                ToolType.SedativeFluid => CraftedMaterialType.SedativeSyringe,

                // 포션
                ToolType.PotionCyan => CraftedMaterialType.FilledPotionCyan,
                ToolType.PotionMagenta => CraftedMaterialType.FilledPotionMagenta,
                ToolType.PotionYellow => CraftedMaterialType.FilledPotionYellow,

                _ => CraftedMaterialType.None
            };
        }

        private Sprite GetInteractableIcon(IInteractable interactable)
        {
            if (interactable is not Component component)
            {
                return null;
            }

            ItemObject itemObject = component.GetComponent<ItemObject>();
            return GetItemIcon(itemObject);
        }

        private Sprite GetPushableItemIcon()
        {
            return GetPushableIcon(_playerInteraction.CurrentPushInteractable);
        }

        private Sprite GetPushableIcon(IInteractable interactable)
        {
            if (interactable is not Component component)
            {
                return null;
            }

            if (component.TryGetComponent(out DiagnosisEmergencyMachine machine) && _emergencyUiCatalog != null)
            {
                return _emergencyUiCatalog.GetDiagnosisIcon(machine.MachineType);
            }

            return GetInteractableIcon(interactable);
        }
    }
}
