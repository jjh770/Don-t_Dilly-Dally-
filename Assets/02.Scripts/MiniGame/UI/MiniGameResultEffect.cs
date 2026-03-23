using DG.Tweening;
using TMPro;
using UnityEngine;

namespace DontDillyDally.MiniGame
{
    // 모든 미니게임에서 재활용 가능한 성공/실패 결과 연출 컴포넌트.
    // MiniGameCanvas 프리팹의 공통 영역에 하나만 배치한다.
    public sealed class MiniGameResultEffect : MonoBehaviour
    {
        [Header("파티클")]
        [SerializeField] private ParticleSystem _successParticle;
        [SerializeField] private ParticleSystem _failParticle;

        [Header("결과 텍스트")]
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] private Color _successColor = new Color(0.2f, 1f, 0.2f, 1f);
        [SerializeField] private Color _failColor = new Color(1f, 0.2f, 0.2f, 1f);

        [Header("DOTween 연출")]
        [SerializeField] private float _textPunchScale = 0.3f;
        [SerializeField] private float _textPunchDuration = 0.3f;
        [SerializeField] private float _shakeStrength = 5f;
        [SerializeField] private float _shakeDuration = 0.3f;

        [Header("텍스트 내용")]
        [SerializeField] private string _successMessage = "SUCCESS!";
        [SerializeField] private string _failMessage = "FAIL";

        public void PlaySuccess()
        {
            Reset();

            if (_successParticle != null)
            {
                _successParticle.Play();
            }

            if (_resultText != null)
            {
                _resultText.gameObject.SetActive(true);
                _resultText.text = _successMessage;
                _resultText.color = _successColor;
                _resultText.transform.localScale = Vector3.one;

                _resultText.transform
                    .DOPunchScale(Vector3.one * _textPunchScale, _textPunchDuration, 6, 0.5f)
                    .SetUpdate(true);
            }
        }

        public void PlayFail()
        {
            Reset();

            if (_failParticle != null)
            {
                _failParticle.Play();
            }

            if (_resultText != null)
            {
                _resultText.gameObject.SetActive(true);
                _resultText.text = _failMessage;
                _resultText.color = _failColor;
                _resultText.transform.localScale = Vector3.one;

                _resultText.transform
                    .DOShakePosition(_shakeDuration, _shakeStrength, 20, 90f, false, true)
                    .SetUpdate(true);
            }
        }

        public void Reset()
        {
            if (_resultText != null)
            {
                DOTween.Kill(_resultText.transform);
                _resultText.text = "";
                _resultText.transform.localScale = Vector3.one;
                _resultText.gameObject.SetActive(false);
            }

            if (_successParticle != null)
            {
                _successParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (_failParticle != null)
            {
                _failParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
