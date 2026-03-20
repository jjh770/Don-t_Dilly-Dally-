using Photon.Pun;
using UnityEngine;
using System;

// CommentarySystem의 네트워크 동기화 담당
// - Host가 코멘터리를 트리거하면 모든 클라이언트에 전파
// - CommentaryOrchestrator가 네트워크 세부 구현을 알지 않도록 분리
[RequireComponent(typeof(PhotonView))]
public class CommentaryNetworkBridge : MonoBehaviourPun
{
    public static CommentaryNetworkBridge Instance { get; private set; }

    public event Action<CommentaryData> OnCommentaryReceived;

    [Header("Debug")]
    [SerializeField] private bool _showDebugLogs = true;

    // 중복 실행 방지
    private int _lastReceivedSequence = -1;
    private int _currentSequence = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public bool IsHost => PhotonNetwork.IsMasterClient;

    [Header("동기화 설정")]
    [SerializeField] private float _syncDelaySeconds = 0.1f; // 네트워크 지연 보정용 딜레이

    // Host가 호출 - 모든 클라이언트에 코멘터리 전파
    public void BroadcastCommentary(EventType eventType, string narrationText, bool usePreGenerated)
    {
        if (!PhotonNetwork.IsConnected)
        {
            // 싱글플레이: 로컬에서 바로 재생
            var data = new CommentaryData(_currentSequence++, eventType, narrationText, usePreGenerated, 0);
            OnCommentaryReceived?.Invoke(data);
            return;
        }

        if (!IsHost)
        {
            Debug.LogWarning("[CommentaryNetwork] Host만 BroadcastCommentary를 호출할 수 있습니다.");
            return;
        }

        int sequence = _currentSequence++;

        double networkPlayTime = PhotonNetwork.Time + _syncDelaySeconds;

        if (_showDebugLogs)
            Debug.Log($"[CommentaryNetwork] 코멘터리 전파: seq={sequence}, type={eventType}, playTime={networkPlayTime:F3}");

        // 모든 클라이언트에게 RPC 전송
        photonView.RPC(
            nameof(RPC_PlayCommentary),
            RpcTarget.All,
            sequence,
            (int)eventType,
            narrationText,
            usePreGenerated,
            networkPlayTime
        );
    }

    [PunRPC]
    private void RPC_PlayCommentary(int sequence, int eventTypeInt, string narrationText, bool usePreGenerated, double networkPlayTime)
    {
        if (sequence <= _lastReceivedSequence)
        {
            if (_showDebugLogs)
                Debug.Log($"[CommentaryNetwork] 중복 코멘터리 무시: seq={sequence}, lastSeq={_lastReceivedSequence}");
            return;
        }

        if (sequence > _lastReceivedSequence + 1 && _lastReceivedSequence >= 0)
        {
            Debug.LogWarning($"[CommentaryNetwork] 코멘터리 순서 건너뜀: expected={_lastReceivedSequence + 1}, received={sequence}");
        }

        _lastReceivedSequence = sequence;

        var data = new CommentaryData(
            sequence,
            (EventType)eventTypeInt,
            narrationText,
            usePreGenerated,
            networkPlayTime
        );

        if (_showDebugLogs)
            Debug.Log($"[CommentaryNetwork] 코멘터리 수신: seq={sequence}, type={data.GetEventType()}, playTime={networkPlayTime:F3}");

        OnCommentaryReceived?.Invoke(data);
    }

    // ========== 이벤트 전달 (Non-host → Host) ==========
    public void RequestEvent(EventType eventType, string description)
    {
        if (!PhotonNetwork.IsConnected)
        {
            // 싱글플레이: 직접 발행
            EventManager.Instance?.Publish(eventType, description);
            return;
        }

        if (IsHost)
        {
            // Host: 직접 발행
            EventManager.Instance?.Publish(eventType, description);
        }
        else
        {
            // Non-host: Host에게 RPC 전송
            if (_showDebugLogs)
                Debug.Log($"[CommentaryNetwork] Host에게 이벤트 요청: {eventType}");

            photonView.RPC(nameof(RPC_RequestEventToHost), RpcTarget.MasterClient, (int)eventType, description);
        }
    }

    [PunRPC]
    private void RPC_RequestEventToHost(int eventTypeInt, string description)
    {
        if (!IsHost) return;

        if (_showDebugLogs)
            Debug.Log($"[CommentaryNetwork] 이벤트 요청 수신: {(EventType)eventTypeInt}");

        EventManager.Instance?.Publish((EventType)eventTypeInt, description);
    }
}
