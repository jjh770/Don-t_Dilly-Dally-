using DG.Tweening;
using Photon.Pun;
using TMPro;
using UnityEngine;

// 같은 GameObject의 TMP_Text에 집도의 닉네임을 표시.
// 이 GameObject 자체를 Timeline Activation Track에 바인딩해서 활성/비활성 제어.
// 활성화되면 페이드인 → 일정 시간 표시 → 페이드아웃.
[RequireComponent(typeof(TMP_Text))]
[RequireComponent(typeof(CanvasGroup))]
public class SurgeonNameLabel : MonoBehaviour
{
    [Header("문구")]
    [Tooltip("{0} 자리에 닉네임. 인스펙터에서 \\n 입력 시 줄바꿈으로 변환됨")]
    [TextArea(2, 4)]
    [SerializeField] private string _format = "집도의\n<size=120><color=#FFC107>{0}</color></size>";
    [SerializeField] private string _fallbackName = "???";

    [Header("페이드 타이밍 (초)")]
    [SerializeField] private float _fadeInDuration = 0.5f;
    [SerializeField] private float _visibleDuration = 2f;
    [SerializeField] private float _fadeOutDuration = 0.5f;

    private TMP_Text _label;
    private CanvasGroup _canvasGroup;
    private Sequence _sequence;

    private void Awake()
    {
        _label = GetComponent<TMP_Text>();
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        _label.text = string.Format(_format.Replace("\\n", "\n"), GetSurgeonNickname());

        _canvasGroup.alpha = 0f;
        _sequence?.Kill();
        _sequence = DOTween.Sequence()
            .Append(_canvasGroup.DOFade(1f, _fadeInDuration))
            .AppendInterval(_visibleDuration)
            .Append(_canvasGroup.DOFade(0f, _fadeOutDuration))
            .SetLink(gameObject);
    }

    private void OnDisable()
    {
        _sequence?.Kill();
        _sequence = null;
    }

    private string GetSurgeonNickname()
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (RoleProperties.GetPlayerRole(player) != RoleType.Surgeon) continue;
            return string.IsNullOrEmpty(player.NickName) ? _fallbackName : player.NickName;
        }
        return _fallbackName;
    }
}
