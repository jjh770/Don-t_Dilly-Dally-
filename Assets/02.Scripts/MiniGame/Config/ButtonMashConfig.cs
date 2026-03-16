using UnityEngine;

namespace DontDillyDally.MiniGame
{
    [CreateAssetMenu(menuName = "DontDillyDally/MiniGame/ButtonMashConfig")]
    public sealed class ButtonMashConfig : MiniGameConfig
    {
        [Header("키 연타 설정")]
        [Tooltip("한 번 누를 때 게이지 증가량 (0~1 기준)")]
        public float gainPerPress = 0.05f;

        [Tooltip("초당 자연 감소량")]
        public float decayPerSecond = 0.15f;

        [Tooltip("초당 최대 입력 횟수 (매크로 방지)")]
        public float maxInputPerSecond = 15f;

        [Tooltip("연타 감지할 키")]
        public KeyCode inputKey = KeyCode.Space;
    }
}
