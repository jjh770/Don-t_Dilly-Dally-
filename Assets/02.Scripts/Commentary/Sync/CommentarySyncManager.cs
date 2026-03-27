using System;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class CommentarySyncManager : MonoBehaviourPunCallbacks
{
    public static CommentarySyncManager Instance { get; private set; }

    public event Action<CommentarySyncData> OnCommentaryReceived;

    [Header("동기화 설정")]
    [SerializeField] private float _playbackDelaySeconds = 0.3f;

    private readonly HashSet<string> _processedCommentaryIds = new();
    private int _lastReceivedSequence = -1;

    private const byte COMMENTARY_EVENT_CODE = 100;
    private const byte GAME_EVENT_TO_HOST_CODE = 101;

    public bool IsHost => PhotonNetwork.IsMasterClient;
    public double NetworkTime => PhotonNetwork.Time;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEventReceived;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEventReceived;
    }

    public void SendEventToHost(GameEvent gameEvent)
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[CommentarySyncManager] 네트워크에 연결되지 않았습니다.");
            return;
        }

        if (IsHost)
        {
            // 호스트라면 직접 처리
            CommentaryController.Instance?.HandleEventAsHost(gameEvent);
            return;
        }

        // 클라이언트라면 호스트에게 전송
        var eventData = SerializeGameEvent(gameEvent);
        var raiseEventOptions = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.MasterClient
        };

        PhotonNetwork.RaiseEvent(GAME_EVENT_TO_HOST_CODE, eventData, raiseEventOptions, ExitGames.Client.Photon.SendOptions.SendReliable);
    }

    public void BroadcastCommentary(CommentarySyncData syncData)
    {
        if (!IsHost)
        {
            Debug.LogWarning("[CommentarySyncManager] 호스트만 코멘터리를 브로드캐스트할 수 있습니다.");
            return;
        }

        // 예약 재생 시간 설정
        syncData.ScheduledNetworkTime = NetworkTime + _playbackDelaySeconds;

        var eventData = SerializeSyncData(syncData);
        var raiseEventOptions = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.All
        };

        PhotonNetwork.RaiseEvent(COMMENTARY_EVENT_CODE, eventData, raiseEventOptions, ExitGames.Client.Photon.SendOptions.SendReliable);
    }

    private void OnPhotonEventReceived(ExitGames.Client.Photon.EventData photonEvent)
    {
        switch (photonEvent.Code)
        {
            case COMMENTARY_EVENT_CODE:
                HandleCommentaryEvent(photonEvent.CustomData as byte[]);
                break;

            case GAME_EVENT_TO_HOST_CODE:
                if (IsHost)
                {
                    HandleGameEventFromClient(photonEvent.CustomData as byte[]);
                }
                break;
        }
    }

    private void HandleCommentaryEvent(byte[] data)
    {
        if (data == null) return;

        var syncData = DeserializeSyncData(data);
        if (syncData == null) return;

        // 중복 재생 방지
        if (_processedCommentaryIds.Contains(syncData.CommentaryId))
        {
            return;
        }

        // 순서 역전 방지
        if (syncData.Sequence <= _lastReceivedSequence)
        {
            Debug.LogWarning($"[CommentarySyncManager] 순서 역전 감지: received={syncData.Sequence}, last={_lastReceivedSequence}");
            return;
        }

        _processedCommentaryIds.Add(syncData.CommentaryId);
        _lastReceivedSequence = syncData.Sequence;

        OnCommentaryReceived?.Invoke(syncData);
    }

    private void HandleGameEventFromClient(byte[] data)
    {
        if (data == null) return;

        var gameEvent = DeserializeGameEvent(data);
        if (gameEvent == null) return;

        CommentaryController.Instance?.HandleEventAsHost(gameEvent);
    }

    private byte[] SerializeSyncData(CommentarySyncData data)
    {
        string json = JsonUtility.ToJson(data);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    private CommentarySyncData DeserializeSyncData(byte[] data)
    {
        try
        {
            string json = System.Text.Encoding.UTF8.GetString(data);
            return JsonUtility.FromJson<CommentarySyncData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CommentarySyncManager] SyncData 역직렬화 실패: {e.Message}");
            return null;
        }
    }

    private byte[] SerializeGameEvent(GameEvent gameEvent)
    {
        var serializableEvent = new SerializableGameEvent
        {
            Type = (int)gameEvent.Type,
            Priority = (int)gameEvent.Priority,
            Description = gameEvent.Description
        };
        string json = JsonUtility.ToJson(serializableEvent);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    private GameEvent DeserializeGameEvent(byte[] data)
    {
        try
        {
            string json = System.Text.Encoding.UTF8.GetString(data);
            var serializableEvent = JsonUtility.FromJson<SerializableGameEvent>(json);
            return new GameEvent(
                (EventType)serializableEvent.Type,
                serializableEvent.Description,
                (EventPriority)serializableEvent.Priority
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"[CommentarySyncManager] GameEvent 역직렬화 실패: {e.Message}");
            return null;
        }
    }

    [Serializable]
    private class SerializableGameEvent
    {
        public int Type;
        public int Priority;
        public string Description;
    }
}
