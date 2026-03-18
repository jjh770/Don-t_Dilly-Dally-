using DontDillyDally.Data;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

// 아이템 공급원의 공통 동작을 담당하는 제네릭 베이스 클래스입니다.
// 생성 위치 관리, 자동 리스폰, 현재 생성 아이템 추적을 공통으로 처리합니다.
public abstract class ItemSource<TItem> : MonoBehaviourPunCallbacks where TItem : ItemObject
{
    [Tooltip("이 공급원에서 생성할 아이템 프리팹")]
    public TItem SpawnedItemPrefab;

    [Tooltip("아이템을 생성할 기준 위치이자 부모 Transform")]
    public Transform SpawnPoint;

    [Tooltip("생성된 아이템이 공급원을 벗어나면 자동으로 다시 생성할지 여부")]
    public bool AutoRespawn = true;

    protected TItem CurrentSpawnedItem;

    protected virtual void Start()
    {
        TryEnsureSpawnedItem();
    }

    public override void OnJoinedRoom()
    {
        TryEnsureSpawnedItem();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        TryEnsureSpawnedItem();
    }

    protected virtual void Update()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (!AutoRespawn)
            return;

        if (ShouldRespawn())
        {
            CurrentSpawnedItem = null;
            EnsureSpawnedItem();
        }
    }

    public void ForceRespawn()
    {
        if (!CanUsePhotonSpawn())
            return;
        EnsureSpawnedItem(forceRespawn: true);
    }

    protected virtual bool CanSpawnItem()
    {
        return SpawnedItemPrefab != null;
    }

    protected virtual bool ShouldRespawn()
    {
        if (CurrentSpawnedItem == null)
            return true;
        return CurrentSpawnedItem.HasLeftSource;
    }

    protected abstract object[] GetInstantiationData();

    protected abstract string GetDefaultItemName();

    protected Transform GetSpawnParent()
    {
        return SpawnPoint != null ? SpawnPoint : transform;
    }

    private void TryEnsureSpawnedItem()
    {
        if (!CanUsePhotonSpawn())
            return;

        EnsureSpawnedItem();
    }

    private static bool CanUsePhotonSpawn()
    {
        return PhotonNetwork.IsConnected && PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient;
    }

    private void EnsureSpawnedItem(bool forceRespawn = false)
    {
        if (!CanSpawnItem())
            return;

        Transform parent = GetSpawnParent();

        if (forceRespawn && CurrentSpawnedItem != null && CurrentSpawnedItem.IsStillAt(parent))
        {
            PhotonNetwork.Destroy(CurrentSpawnedItem.gameObject);
            CurrentSpawnedItem = null;
        }

        if (CurrentSpawnedItem != null)
            return;

        GameObject spawnedObject = PhotonNetwork.InstantiateRoomObject(
            SpawnedItemPrefab.name,
            parent.position,
            parent.rotation,
            0,
            GetInstantiationData());

        TItem spawnedItem = spawnedObject.GetComponent<TItem>();
        spawnedItem.name = GetDefaultItemName();
        CurrentSpawnedItem = spawnedItem;
    }
}
