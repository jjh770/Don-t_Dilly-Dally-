using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

// 컷씬 씬4(팀 카메라)에서 집도의를 제외한 플레이어 닉네임을
// 화면 오른쪽 → 가운데 → 왼쪽 순으로 슬라이드 애니메이션으로 순차 표시.
// CutsceneCharacterAnchor_3 → 0 카메라 순서에 맞춰 ActorNumber 내림차순으로 출력.
// 이 GameObject 자체를 Timeline Activation Track에 바인딩해서 활성/비활성 제어.
[RequireComponent(typeof(RectTransform))]
public class AssistantNamesLabel : MonoBehaviour
{
    [Header("템플릿")]
    [Tooltip("닉네임 하나를 표시할 TMP_Text 템플릿. 비활성 상태로 같은 부모 아래에 둘 것")]
    [SerializeField] private TMP_Text _template;

    [Header("문구")]
    [Tooltip("{0} 자리에 닉네임. 인스펙터에서 \\n 입력 시 줄바꿈으로 변환됨")]
    [TextArea(2, 4)]
    [SerializeField] private string _format = "보조의\n<size=120><color=#FFC107>{0}</color></size>";
    [SerializeField] private string _fallbackName = "???";

    [Header("타이밍 (초)")]
    [Tooltip("전체 애니메이션이 끝나야 하는 총 시간. 보조의 수에 상관없이 이 시간 안에 모든 이름 표시 완료")]
    [SerializeField] private float _totalDuration = 3f;
    [Tooltip("한 라벨이 화면 밖 → 가운데 (또는 가운데 → 화면 밖) 이동하는 데 걸리는 시간")]
    [SerializeField] private float _slideDuration = 0.3f;

    [Header("슬라이드 거리 (픽셀)")]
    [Tooltip("화면 밖 시작/끝 지점의 x 오프셋. + = 오른쪽 밖, - = 왼쪽 밖")]
    [SerializeField] private float _slideDistance = 1200f;

    [Header("이징")]
    [SerializeField] private Ease _slideInEase = Ease.OutCubic;
    [SerializeField] private Ease _slideOutEase = Ease.InCubic;

    private Sequence _sequence;
    private readonly List<TMP_Text> _spawned = new();

    private void Awake()
    {
        if (_template != null) _template.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        BuildAndPlay();
    }

    private void OnDisable()
    {
        _sequence?.Kill();
        _sequence = null;
        ClearSpawned();
    }

    private void BuildAndPlay()
    {
        ClearSpawned();
        if (_template == null) return;

        var players = GetOrderedAssistants();
        if (players.Count == 0) return;

        var format = _format.Replace("\\n", "\n");

        // 인원수에 따라 hold 시간을 자동 계산.
        // total = N * (slide + hold) + slide  →  hold = (total - slide*(N+1)) / N
        int n = players.Count;
        float hold = Mathf.Max(0f, (_totalDuration - _slideDuration * (n + 1)) / n);

        _sequence?.Kill();
        _sequence = DOTween.Sequence().SetLink(gameObject);

        for (int i = 0; i < n; i++)
        {
            var instance = Instantiate(_template, _template.transform.parent);
            instance.gameObject.SetActive(true);
            instance.text = string.Format(format, GetNickname(players[i]));
            _spawned.Add(instance);

            var rect = instance.rectTransform;
            rect.anchoredPosition = new Vector2(_slideDistance, rect.anchoredPosition.y);

            float startTime = i * (_slideDuration + hold);

            // Slide in: 오른쪽 밖 → 가운데
            _sequence.Insert(startTime,
                rect.DOAnchorPosX(0f, _slideDuration).SetEase(_slideInEase));

            // Slide out: 가운데 → 왼쪽 밖 (다음 라벨의 slide in과 동시 시작)
            _sequence.Insert(startTime + _slideDuration + hold,
                rect.DOAnchorPosX(-_slideDistance, _slideDuration).SetEase(_slideOutEase));
        }
    }

    // CutsceneManager.AssignPlayersToSlots와 동일하게 보조의는 ActorNumber 오름차순으로 slot 1..3에 배정됨.
    // 카메라는 anchor 3 → 0 진행이므로 ActorNumber 내림차순으로 뒤집어 반환.
    private List<Player> GetOrderedAssistants()
    {
        return PhotonNetwork.PlayerList
            .Where(p => RoleProperties.GetPlayerRole(p) != RoleType.Surgeon)
            .OrderByDescending(p => p.ActorNumber)
            .ToList();
    }

    private string GetNickname(Player player)
    {
        return string.IsNullOrEmpty(player.NickName) ? _fallbackName : player.NickName;
    }

    private void ClearSpawned()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null) Destroy(_spawned[i].gameObject);
        }
        _spawned.Clear();
    }
}
