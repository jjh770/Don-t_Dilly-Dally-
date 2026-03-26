using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class SelectRoleManager : MonoBehaviourPunCallbacks
{
    public static SelectRoleManager Instance { get; private set; }

    [Header("설정")]
    [SerializeField] private RoleVisualProfileSO _visualProfile;

    [Header("디버그")]
    [SerializeField] private bool _debugLog = true;

    private Dictionary<int, RoleType> _playerRoles = new();
    private bool _isRoleAssigned;

    public event Action OnRolesAssigned;
    public event Action OnRolesCleared;
    public event Action<int, RoleType> OnPlayerRoleChanged;

    public RoleVisualProfileSO VisualProfile => _visualProfile;
    public bool IsRoleAssigned => _isRoleAssigned;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // 스테이지 시작 시 호출 - 마스터 클라이언트가 역할 선정
    public int AssignRoles()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Log("마스터 클라이언트가 아님 - 역할 선정 대기");
            return -1;
        }

        var players = PhotonNetwork.PlayerList;
        if (players.Length < 2)
        {
            Debug.LogWarning("[SelectRoleManager] 플레이어가 2명 미만 - 역할 선정 불가");
            return -1;
        }

        if (players.Length > 4)
        {
            Debug.LogWarning("[SelectRoleManager] 플레이어가 4명 초과 - 초과 플레이어는 역할 없음");
        }

        // 플레이어 정렬
        var sortedPlayers = players.OrderBy(p => p.ActorNumber).ToList();

        // 집도의 선정 (랜덤)
        int surgeonIndex = SelectSurgeonIndex(sortedPlayers.Count);
        int surgeonActorNumber = sortedPlayers[surgeonIndex].ActorNumber;

        // 역할 배정
        var roleAssignments = new Dictionary<int, RoleType>();
        int assistantIndex = 0;

        for (int i = 0; i < sortedPlayers.Count && i < 4; i++)
        {
            var player = sortedPlayers[i];

            if (i == surgeonIndex)
            {
                roleAssignments[player.ActorNumber] = RoleType.Surgeon;
            }
            else
            {
                var assistantRole = RoleTypeExtensions.GetAssistantRole(assistantIndex);
                roleAssignments[player.ActorNumber] = assistantRole;
                assistantIndex++;
            }
        }

        // RPC로 모든 클라이언트에 역할 전파
        photonView.RPC(nameof(RPC_AssignRoles), RpcTarget.AllBuffered, SerializeRoles(roleAssignments));

        Log($"역할 배정 완료 - 집도의: Player {surgeonActorNumber}");
        return surgeonActorNumber;
    }

    // 집도의 인덱스 선정 (랜덤)
    private int SelectSurgeonIndex(int playerCount)
    {
        return UnityEngine.Random.Range(0, playerCount);
    }

    [PunRPC]
    private void RPC_AssignRoles(int[] serializedData)
    {
        _playerRoles.Clear();

        for (int i = 0; i < serializedData.Length; i += 2)
        {
            int actorNumber = serializedData[i];
            RoleType role = (RoleType)serializedData[i + 1];
            _playerRoles[actorNumber] = role;

            Log($"역할 수신 - Player {actorNumber}: {role}");
        }

        // 로컬 플레이어 역할을 Custom Properties에 저장
        if (_playerRoles.TryGetValue(PhotonNetwork.LocalPlayer.ActorNumber, out var localRole))
        {
            RoleProperties.SetLocalPlayerRole(localRole);
        }

        _isRoleAssigned = true;
        OnRolesAssigned?.Invoke();

        // 각 플레이어 역할 변경 이벤트 발생
        foreach (var kvp in _playerRoles)
        {
            OnPlayerRoleChanged?.Invoke(kvp.Key, kvp.Value);
        }
    }

    // 게임 종료 시 호출 - 모든 역할 초기화
    public void ClearRoles()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_ClearRoles), RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    private void RPC_ClearRoles()
    {
        var previousRoles = new Dictionary<int, RoleType>(_playerRoles);
        _playerRoles.Clear();
        _isRoleAssigned = false;

        RoleProperties.ClearLocalPlayerRole();

        // 모든 플레이어 역할 초기화 이벤트
        foreach (var kvp in previousRoles)
        {
            OnPlayerRoleChanged?.Invoke(kvp.Key, RoleType.None);
        }

        OnRolesCleared?.Invoke();
        Log("역할 초기화 완료");
    }

    public RoleType GetPlayerRole(int actorNumber)
    {
        return _playerRoles.TryGetValue(actorNumber, out var role) ? role : RoleType.None;
    }

    public RoleType GetPlayerRole(Player player)
    {
        return player != null ? GetPlayerRole(player.ActorNumber) : RoleType.None;
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (_playerRoles.ContainsKey(otherPlayer.ActorNumber))
        {
            _playerRoles.Remove(otherPlayer.ActorNumber);
            Log($"플레이어 퇴장으로 역할 제거 - Player {otherPlayer.ActorNumber}");
        }
    }

    private int[] SerializeRoles(Dictionary<int, RoleType> roles)
    {
        var data = new int[roles.Count * 2];
        int i = 0;
        foreach (var kvp in roles)
        {
            data[i++] = kvp.Key;
            data[i++] = (int)kvp.Value;
        }
        return data;
    }

    private void Log(string message)
    {
        if (_debugLog)
            Debug.Log($"[SelectRoleManager] {message}");
    }
}
