using UnityEngine;

namespace DontDillyDally.MiniGame
{
    [CreateAssetMenu(menuName = "DontDillyDally/MiniGame/ReCaptchaConfig")]
    public sealed class ReCaptchaConfig : MiniGameConfig
    {
        [Header("reCAPTCHA 설정")]
        [Tooltip("체크박스를 누른 뒤 스피너가 도는 시간(초). 1~2초 권장")]
        [Range(0.3f, 3f)]
        public float LoadingDuration = 1.2f;

        [Tooltip("로딩이 끝난 뒤 체크표시를 그리고 유지하는 전체 연출 시간(초). 이 기간 동안 타이머는 정지하고 Succeeded 판정이 지연된다. 체크마크 fill 시간 + 여유 hold를 합친 값")]
        [Range(0.3f, 3f)]
        public float SuccessHoldDuration = 1.3f;
    }
}
