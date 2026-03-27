using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Playables;

// 컷씬 씬의 자립형 매니저
// 씬 로드 시 자동 시작 → 커스터마이징 적용 → Timeline 재생 → GameScene 전환
[RequireComponent(typeof(PhotonView))]
public class CutsceneManager : MonoBehaviourPunCallbacks
{
    [Header("컷씬")]
    [SerializeField] private PlayableDirector _director;
    [SerializeField] private CutsceneCharacterSlot[] _characterSlots;
    [SerializeField] private float _customizingTimeoutSec = 10f;
    [SerializeField] private float _managerWaitTimeoutSec = 5f;

    [Header("스킵")]
    [SerializeField] private CutsceneSkipUI _skipUI;

    private CancellationTokenSource _cts;
    private bool _isPlaying;
    private bool _isSkipped;

    private void Start()
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        RunCutsceneFlow(_cts.Token).Forget();
    }

    private void Update()
    {
        if (!_isPlaying || _isSkipped) return;
        if (!PhotonNetwork.IsMasterClient) return;
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (_skipUI == null) return;

        if (!_skipUI.IsVisible)
        {
            _skipUI.Show();
        }
        else
        {
            SkipCutscene();
        }
    }

    private void SkipCutscene()
    {
        photonView.RPC(nameof(RPC_SkipCutscene), RpcTarget.All);
    }

    [PunRPC]
    private void RPC_SkipCutscene()
    {
        if (_isSkipped) return;
        _isSkipped = true;

        Debug.Log("[CutsceneManager] 컷씬 스킵");

        // Timeline 정지
        if (_director != null)
        {
            _director.Stop();
        }

        // 스킵 UI 숨김
        if (_skipUI != null)
        {
            _skipUI.Hide();
        }

        // 비동기 플로우 취소 → LoadGameScene은 별도 호출
        _cts?.Cancel();
        LoadGameScene();
    }

    private async UniTaskVoid RunCutsceneFlow(CancellationToken ct)
    {
        Debug.Log("[CutsceneManager] 컷씬 플로우 시작");

        // 0. CustomizingManager 준비 대기
        await WaitForCustomizingManager(ct);

        // 1. 플레이어-슬롯 매핑
        AssignPlayersToSlots();

        // 2. 할당된 슬롯에 커스터마이징 병렬 적용 (타임아웃 포함)
        await ApplyAllCustomizingAsync(ct);

        // 3. 미할당 슬롯에 기본 외형 적용
        foreach (var slot in _characterSlots)
        {
            if (!slot.IsAssigned)
            {
                slot.ApplyDefaultAppearance();
            }
        }

        // 4. Timeline 재생 및 완료 대기
        _isPlaying = true;
        if (_director != null)
        {
            _director.Play();
            await WaitForDirectorFinish(ct);
        }
        _isPlaying = false;

        // 스킵으로 취소된 경우 여기서 중단 (RPC_SkipCutscene이 LoadGameScene 처리)
        if (_isSkipped) return;

        Debug.Log("[CutsceneManager] 컷씬 완료 → GameScene 전환");

        // 5. GameScene으로 전환
        LoadGameScene();
    }

    // CustomizingManager가 초기화 + 로드 완료될 때까지 대기
    private async UniTask WaitForCustomizingManager(CancellationToken ct)
    {
        var manager = CustomizingManager.Instance;

        // 인스턴스 자체가 없으면 잠시 대기 (DontDestroyOnLoad 오브젝트 초기화 타이밍)
        if (manager == null)
        {
            var deadline = Time.time + _managerWaitTimeoutSec;
            while (CustomizingManager.Instance == null && Time.time < deadline)
            {
                await UniTask.Yield(ct);
            }
            manager = CustomizingManager.Instance;
        }

        if (manager == null)
        {
            Debug.LogWarning("[CutsceneManager] CustomizingManager를 찾을 수 없음 - 기본 외형으로 진행");
            return;
        }

        // 초기화는 됐지만 Load가 아직 안 끝났으면 OnLoaded 이벤트 대기
        if (!manager.IsInitialized)
        {
            var tcs = new UniTaskCompletionSource();
            void OnLoaded()
            {
                tcs.TrySetResult();
            }

            manager.OnLoaded += OnLoaded;
            try
            {
                var timeoutTask = UniTask.Delay(
                    TimeSpan.FromSeconds(_managerWaitTimeoutSec),
                    cancellationToken: ct);

                await UniTask.WhenAny(tcs.Task, timeoutTask);
            }
            finally
            {
                manager.OnLoaded -= OnLoaded;
            }
        }

        Debug.Log("[CutsceneManager] CustomizingManager 준비 완료");
    }

    private async UniTask ApplyAllCustomizingAsync(CancellationToken ct)
    {
        var applyTasks = _characterSlots
            .Where(s => s.IsAssigned)
            .Select(s => s.ApplyCustomizingAsync())
            .ToArray();

        if (applyTasks.Length == 0) return;

        var timeoutTask = UniTask.Delay(
            TimeSpan.FromSeconds(_customizingTimeoutSec),
            cancellationToken: ct);

        await UniTask.WhenAny(
            UniTask.WhenAll(applyTasks),
            timeoutTask);
    }

    // 집도의를 슬롯 0에 우선 배치, 나머지는 ActorNumber 순서
    // 집도의가 아직 선정되지 않았으면 ActorNumber 순서로 폴백
    private void AssignPlayersToSlots()
    {
        Player surgeon = null;
        var others = new List<Player>();

        foreach (var player in PhotonNetwork.PlayerList)
        {
            var role = RoleProperties.GetPlayerRole(player);
            if (role == RoleType.Surgeon)
            {
                surgeon = player;
            }
            else
            {
                others.Add(player);
            }
        }

        others.Sort((a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

        // 집도의 미선정 시 ActorNumber 순서로 폴백
        if (surgeon == null)
        {
            Debug.LogWarning("[CutsceneManager] 집도의 미선정 - ActorNumber 순서로 배치");
            var allSorted = PhotonNetwork.PlayerList
                .OrderBy(p => p.ActorNumber)
                .ToArray();

            AssignSortedPlayersToSlots(allSorted);
            return;
        }

        // 집도의 → 슬롯 0, 나머지 → 슬롯 1~3
        int slotIndex = 0;

        _characterSlots[slotIndex].AssignPlayer(surgeon);
        _characterSlots[slotIndex].SetVisible(true);
        slotIndex++;

        foreach (var player in others)
        {
            if (slotIndex >= _characterSlots.Length) break;
            _characterSlots[slotIndex].AssignPlayer(player);
            _characterSlots[slotIndex].SetVisible(true);
            slotIndex++;
        }

        // 나머지 슬롯: 미할당이지만 활성 유지 (기본 외형 표시용)
        for (int i = slotIndex; i < _characterSlots.Length; i++)
        {
            _characterSlots[i].SetVisible(true);
        }
    }

    private void AssignSortedPlayersToSlots(Player[] sortedPlayers)
    {
        for (int i = 0; i < _characterSlots.Length; i++)
        {
            if (i < sortedPlayers.Length)
            {
                _characterSlots[i].AssignPlayer(sortedPlayers[i]);
                _characterSlots[i].SetVisible(true);
            }
            else
            {
                _characterSlots[i].SetVisible(true);
            }
        }
    }

    private void LoadGameScene()
    {
        if (SceneLoadManager.Instance != null)
        {
            SceneLoadManager.Instance.BeginSceneLoad(ESceneType.Gameplay);
        }
        else
        {
            Debug.LogError("[CutsceneManager] SceneLoadManager가 없음 - GameScene 전환 실패");
        }
    }

    private async UniTask WaitForDirectorFinish(CancellationToken ct)
    {
        if (_director == null) return;

        var tcs = new UniTaskCompletionSource();

        void OnStopped(PlayableDirector director)
        {
            tcs.TrySetResult();
        }

        _director.stopped += OnStopped;

        try
        {
            await tcs.Task.AttachExternalCancellation(ct);
        }
        finally
        {
            _director.stopped -= OnStopped;
        }
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
