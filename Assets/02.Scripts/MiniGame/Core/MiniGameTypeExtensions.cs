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
    }
}
