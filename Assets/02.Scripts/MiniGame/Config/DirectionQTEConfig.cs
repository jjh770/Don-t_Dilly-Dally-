using UnityEngine;

namespace DontDillyDally.MiniGame
{
    [CreateAssetMenu(menuName = "DontDillyDally/MiniGame/DirectionQTEConfig")]
    public sealed class DirectionQTEConfig : MiniGameConfig
    {
        [Header("QTE 설정")]
        [Tooltip("총 입력해야 하는 방향 수")]
        [Range(4, 12)]
        public int sequenceLength = 6;

        // 4방향 고정이므로 static readonly로 선언
        public static readonly KeyCode[] DirectionKeys =
        {
            KeyCode.UpArrow,
            KeyCode.DownArrow,
            KeyCode.LeftArrow,
            KeyCode.RightArrow
        };
    }
}
