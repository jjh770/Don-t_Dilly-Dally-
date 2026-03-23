using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System;
using System.Collections.Generic;

/// <summary>
/// 커스터마이징 데이터를 Photon Player Custom Properties로 동기화
/// 플레이어 프리팹에 부착
/// </summary>
public class CustomizingNetworkSync : MonoBehaviourPunCallbacks
{
    private const string CUSTOMIZING_PROPERTY_KEY = "Customizing";

    [Header("참조")]
    [SerializeField] private PlayerCustomizing _playerCustomizing;

    private bool _isInitialized = false;

    private void Awake()
    {
        if (_playerCustomizing == null)
            _playerCustomizing = GetComponent<PlayerCustomizing>();
    }

    private void Start()
    {
        InitializeCustomizing();

        // 로컬 플레이어면 저장 이벤트 구독
        if (photonView.IsMine && CustomizingManager.Instance != null)
        {
            CustomizingManager.Instance.OnSaved += OnCustomizingSaved;
        }
    }

    private void OnDestroy()
    {
        if (photonView.IsMine && CustomizingManager.Instance != null)
        {
            CustomizingManager.Instance.OnSaved -= OnCustomizingSaved;
        }
    }

    private void InitializeCustomizing()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        if (photonView.IsMine)
        {
            // 로컬 플레이어: 저장된 커스터마이징을 네트워크에 등록
            RegisterLocalCustomizing();
        }
        else
        {
            // 원격 플레이어: 해당 플레이어의 커스터마이징 적용
            ApplyRemoteCustomizing(photonView.Owner);
        }
    }

    /// <summary>
    /// 로컬 플레이어의 저장된 커스터마이징을 Custom Properties에 등록
    /// </summary>
    private void RegisterLocalCustomizing()
    {
        if (CustomizingManager.Instance == null || CustomizingManager.Instance.Domain == null)
        {
            Debug.LogWarning("[CustomizingNetworkSync] CustomizingManager가 준비되지 않음");
            return;
        }

        var state = CustomizingManager.Instance.Domain.State;
        string serializedData = SerializeCustomizingState(state);

        var props = new Hashtable
        {
            { CUSTOMIZING_PROPERTY_KEY, serializedData }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        Debug.Log($"[CustomizingNetworkSync] 로컬 커스터마이징 등록: {serializedData}");
    }

    /// <summary>
    /// 원격 플레이어의 커스터마이징 적용
    /// </summary>
    private void ApplyRemoteCustomizing(Player player)
    {
        if (player == null) return;

        if (player.CustomProperties.TryGetValue(CUSTOMIZING_PROPERTY_KEY, out object data))
        {
            string serializedData = data as string;
            if (!string.IsNullOrEmpty(serializedData))
            {
                ApplyCustomizingFromData(serializedData);
                Debug.Log($"[CustomizingNetworkSync] 원격 플레이어 커스터마이징 적용: {player.NickName}");
            }
        }
        else
        {
            Debug.LogWarning($"[CustomizingNetworkSync] 원격 플레이어 커스터마이징 데이터 없음: {player.NickName}");
        }
    }

    /// <summary>
    /// Player Custom Properties 변경 콜백
    /// </summary>
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // 이 오브젝트의 소유자가 아니면 무시
        if (photonView.Owner != targetPlayer) return;

        // 내 캐릭터면 무시 (로컬은 CustomizingManager로 처리)
        if (photonView.IsMine) return;

        if (changedProps.ContainsKey(CUSTOMIZING_PROPERTY_KEY))
        {
            string serializedData = changedProps[CUSTOMIZING_PROPERTY_KEY] as string;
            if (!string.IsNullOrEmpty(serializedData))
            {
                ApplyCustomizingFromData(serializedData);
                Debug.Log($"[CustomizingNetworkSync] 커스터마이징 업데이트 수신: {targetPlayer.NickName}");
            }
        }
    }

    /// <summary>
    /// 직렬화된 데이터로 커스터마이징 적용
    /// </summary>
    private void ApplyCustomizingFromData(string serializedData)
    {
        if (_playerCustomizing == null)
        {
            Debug.LogError("[CustomizingNetworkSync] PlayerCustomizing이 없음");
            return;
        }

        var itemIds = DeserializeCustomizingData(serializedData);
        var catalog = CustomizingManager.Instance?.Catalog;

        if (catalog == null)
        {
            Debug.LogError("[CustomizingNetworkSync] Catalog가 없음");
            return;
        }

        // 기본 장착 먼저 적용
        var baseEquipmentCatalog = CustomizingManager.Instance?.BaseEquipmentCatalog;
        if (baseEquipmentCatalog != null)
        {
            _playerCustomizing.ApplyAllBaseEquipment(baseEquipmentCatalog);
        }

        // 커스터마이징 아이템 적용
        foreach (var kvp in itemIds)
        {
            CustomizingType type = kvp.Key;
            string itemId = kvp.Value;

            if (string.IsNullOrEmpty(itemId)) continue;

            var item = catalog.GetItemById(itemId);
            if (item != null)
            {
                _playerCustomizing.ApplyItem(type, item);
            }
            else
            {
                Debug.LogWarning($"[CustomizingNetworkSync] 아이템을 찾을 수 없음: {itemId}");
            }
        }
    }

    /// <summary>
    /// 커스터마이징 상태를 네트워크 전송용 문자열로 직렬화
    /// 형식: "SkinColor:itemId|Hat:itemId|..."
    /// </summary>
    private string SerializeCustomizingState(CustomizingState state)
    {
        if (state == null) return "";

        var parts = new List<string>();
        var allItems = state.GetAll();

        foreach (var kvp in allItems)
        {
            parts.Add($"{(int)kvp.Key}:{kvp.Value}");
        }

        return string.Join("|", parts);
    }

    /// <summary>
    /// 직렬화된 문자열을 Dictionary로 역직렬화
    /// </summary>
    private Dictionary<CustomizingType, string> DeserializeCustomizingData(string data)
    {
        var result = new Dictionary<CustomizingType, string>();

        if (string.IsNullOrEmpty(data)) return result;

        string[] parts = data.Split('|');
        foreach (string part in parts)
        {
            string[] keyValue = part.Split(':');
            if (keyValue.Length == 2)
            {
                if (int.TryParse(keyValue[0], out int typeInt))
                {
                    CustomizingType type = (CustomizingType)typeInt;
                    result[type] = keyValue[1];
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 런타임 중 커스터마이징 변경 시 호출 (로컬 플레이어용)
    /// </summary>
    public void SyncCurrentCustomizing()
    {
        if (!photonView.IsMine) return;
        RegisterLocalCustomizing();
    }

    /// <summary>
    /// 저장 시 네트워크에도 동기화
    /// </summary>
    public void OnCustomizingSaved()
    {
        if (!photonView.IsMine) return;
        RegisterLocalCustomizing();
    }
}
