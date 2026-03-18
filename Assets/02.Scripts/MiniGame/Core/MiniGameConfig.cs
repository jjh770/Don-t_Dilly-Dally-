using UnityEngine;

namespace DontDillyDally.MiniGame
{
    public abstract class MiniGameConfig : ScriptableObject
    {
        [Header("공통 설정")]
        [Tooltip("미니게임 제한 시간 (초)")]
        public float TimeLimit = 10f;
    }
}
