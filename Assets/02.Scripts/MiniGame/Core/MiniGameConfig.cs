using UnityEngine;

namespace DontDillyDally.MiniGame
{
    public abstract class MiniGameConfig : ScriptableObject
    {
        [Header("공통 설정")]
        [Tooltip("미니게임 제한 시간 (초)")]
        public float timeLimit = 10f;

        [Tooltip("성공 판정 기준 (0~1)")]
        [Range(0f, 1f)]
        public float successThreshold = 0.8f;
    }
}
