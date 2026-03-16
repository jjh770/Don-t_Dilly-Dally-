using UnityEngine;

namespace DontDillyDally.MiniGame
{
    public readonly struct MiniGameResult
    {
        public readonly MiniGameType GameType;
        public readonly bool IsSuccess;
        public readonly float Score;
        public readonly float ElapsedTime;

        public MiniGameResult(MiniGameType gameType, bool isSuccess, float score, float elapsedTime)
        {
            GameType = gameType;
            IsSuccess = isSuccess;
            Score = Mathf.Clamp01(score);
            ElapsedTime = elapsedTime;
        }
    }
}
