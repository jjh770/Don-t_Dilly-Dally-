using System;

namespace DontDillyDally.MiniGame
{
    public static class MiniGameTypeExtensions
    {
        private static readonly MiniGameType[] _cachedTypes =
            (MiniGameType[])Enum.GetValues(typeof(MiniGameType));

        public static MiniGameType GetRandom()
        {
            return _cachedTypes[UnityEngine.Random.Range(0, _cachedTypes.Length)];
        }

        // 특정 타입을 제외한 나머지 중에서 랜덤 선택. 할당 없이 인덱스 스킵 방식.
        public static MiniGameType GetRandomExcluding(MiniGameType excluded)
        {
            int available = _cachedTypes.Length - 1;
            if (available <= 0)
            {
                return excluded;
            }

            int index = UnityEngine.Random.Range(0, available);
            for (int i = 0; i < _cachedTypes.Length; i++)
            {
                if (_cachedTypes[i] == excluded)
                {
                    continue;
                }

                if (index == 0)
                {
                    return _cachedTypes[i];
                }

                index--;
            }

            return excluded;
        }
    }
}
