using System;
using UnityEngine;
using UnityEngine.UI;

namespace DontDillyDally.UI
{
    // 여러 UI에서 공통으로 쓰는 Radial 타이머 UI 조각.
    // Image Type을 Filled, Fill Method를 Radial 360으로 설정해두면
    // SetRatio(0~1) 호출만으로 fillAmount + 색상 그라디언트가 동시에 갱신된다.
    [Serializable]
    public sealed class RadialTimerView
    {
        [Tooltip("Image Type을 Filled, Fill Method를 Radial 360으로 설정하세요")]
        [SerializeField] private Image _image;

        [Header("그라디언트 색상")]
        [Tooltip("시간 충분 (100%) 색상")]
        [SerializeField] private Color _colorFull = new Color(0.4f, 1f, 0.2f);
        [Tooltip("중간 (50%) 색상")]
        [SerializeField] private Color _colorMid = new Color(1f, 0.6f, 0f);
        [Tooltip("시간 부족 (0%) 색상")]
        [SerializeField] private Color _colorEmpty = new Color(1f, 0.2f, 0.2f);

        // 미니게임 시작 시 가득 찬 상태로 리셋.
        public void Initialize()
        {
            if (_image == null)
            {
                return;
            }

            _image.fillAmount = 1f;
            _image.color = _colorFull;
        }

        // 0~1 남은 시간 비율로 fillAmount + 색상 갱신.
        public void SetRatio(float timeRatio)
        {
            if (_image == null)
            {
                return;
            }

            float clampedRatio = Mathf.Clamp01(timeRatio);
            _image.fillAmount = clampedRatio;
            _image.color = EvaluateColor(clampedRatio);
        }

        public void BindImageIfEmpty(Image image)
        {
            if (_image != null || image == null)
            {
                return;
            }

            _image = image;
        }

        // 1.0 → _colorFull, 0.5 → _colorMid, 0.0 → _colorEmpty 로 선형 보간.
        private Color EvaluateColor(float t)
        {
            if (t >= 0.5f)
            {
                return Color.Lerp(_colorMid, _colorFull, (t - 0.5f) * 2f);
            }

            return Color.Lerp(_colorEmpty, _colorMid, t * 2f);
        }
    }
}
