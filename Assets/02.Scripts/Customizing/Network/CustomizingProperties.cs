using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;

public static class CustomizingProperties
{
    private const string PROPERTY_KEY = "cust";

    // 현재 장착 정보 -> JSON 문자열로 변환
    public static string Serialize(Dictionary<CustomizingType, string> items)
    {
        if (items == null || items.Count == 0) return "{}";

        var data = new SerializedData();
        data.Items = new SerializedItem[items.Count];

        int i = 0;
        foreach (var kvp in items)
        {
            data.Items[i] = new SerializedItem
            {
                CustomizingType = (int)kvp.Key,
                ItemID = kvp.Value
            };
            i++;
        }

        return JsonUtility.ToJson(data);
    }

    // JSON 문자열 -> 현재 장착 정보로 변환
    public static Dictionary<CustomizingType, string> Deserialize(string json)
    {
        var result = new Dictionary<CustomizingType, string>();

        if (string.IsNullOrEmpty(json) || json == "{}")
            return result;

        try
        {
            var data = JsonUtility.FromJson<SerializedData>(json);
            if (data?.Items != null)
            {
                foreach (var item in data.Items)
                {
                    var type = (CustomizingType)item.CustomizingType;
                    result[type] = item.ItemID;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CustomizingProperties] 역직렬화 실패: {e.Message}");
        }

        return result;
    }

    // 로컬 플레이어의 커스터마이징 정보를
    // Photon LocalPlayer의 커스텀 프로퍼티에 저장
    public static void SetLocalPlayerCustomizing(Dictionary<CustomizingType, string> items)
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.LocalPlayer == null)
        {
            Debug.LogWarning("[CustomizingProperties] Photon에 연결되지 않음");
            return;
        }

        string json = Serialize(items);
        var props = new Hashtable { { PROPERTY_KEY, json } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        Debug.Log($"[CustomizingProperties] 로컬 커스터마이징 등록 완료");
    }

    // 특정 플레이어의 커스터마이징 정보 읽기
    public static Dictionary<CustomizingType, string> GetPlayerCustomizing(Player player)
    {
        if (player == null)
            return new Dictionary<CustomizingType, string>();

        if (player.CustomProperties.TryGetValue(PROPERTY_KEY, out object data))
        {
            return Deserialize(data as string);
        }

        return new Dictionary<CustomizingType, string>();
    }

    // 이번 변경분
    public static bool TryGetFromChangedProps(Hashtable changedProps, out Dictionary<CustomizingType, string> items)
    {
        items = null;

        if (changedProps.TryGetValue(PROPERTY_KEY, out object data))
        {
            items = Deserialize(data as string);
            return true;
        }

        return false;
    }

    [Serializable]
    private class SerializedData
    {
        public SerializedItem[] Items;
    }

    [Serializable]
    private class SerializedItem
    {
        public int CustomizingType;
        public string ItemID;
    }
}