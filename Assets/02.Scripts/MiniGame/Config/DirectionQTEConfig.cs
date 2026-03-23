using UnityEngine;

namespace DontDillyDally.MiniGame
{
    [CreateAssetMenu(menuName = "DontDillyDally/MiniGame/DirectionQTEConfig")]
    public sealed class DirectionQTEConfig : MiniGameConfig
    {
        [Header("QTE 설정")]
        [Tooltip("총 입력해야 하는 방향 수")]
        [Range(4, 12)]
        public int SequenceLength = 6;

    }
}
