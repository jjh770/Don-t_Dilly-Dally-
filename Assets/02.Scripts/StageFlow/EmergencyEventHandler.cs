using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DontDillyDally.MiniGame;
using UnityEngine;

namespace DontDillyDally.StageFlow
{
    [Serializable]
    public class EmergencyEventHandler
    {
        [Header("레시피 실패 시 긴급 이벤트")]
        [SerializeField, Range(0f, 1f)] private float _recipeFailChance = 0.7f;
        [SerializeField] private float _failHealthPenalty = 15f;

        [Header("랜덤 긴급 이벤트")]
        [SerializeField] private float _randomCheckIntervalSec = 30f;
        [SerializeField, Range(0f, 1f)] private float _randomChance = 0.05f;

        private float _timeSinceLastRandomCheck;

        public bool ShouldTriggerOnRecipeFail()
        {
            return UnityEngine.Random.value <= _recipeFailChance;
        }

        public bool ShouldTriggerRandom(float deltaTime)
        {
            _timeSinceLastRandomCheck += deltaTime;

            if (_timeSinceLastRandomCheck < _randomCheckIntervalSec)
                return false;

            _timeSinceLastRandomCheck = 0f;
            return UnityEngine.Random.value <= _randomChance;
        }

        public void ResetTimer()
        {
            _timeSinceLastRandomCheck = 0f;
        }

        public async UniTask<float> ExecuteEmergency(
            MiniGameLauncher launcher,
            CancellationToken ct)
        {
            MiniGameType type = SelectMiniGameType();

            var tcs = new UniTaskCompletionSource<MiniGameResult>();
            using (ct.Register(() => tcs.TrySetCanceled()))
            {
                launcher.Launch(type, result => tcs.TrySetResult(result));
                MiniGameResult result = await tcs.Task;

                if (!result.IsSuccess)
                {
                    return _failHealthPenalty;
                }
            }

            return 0f;
        }

        private MiniGameType SelectMiniGameType()
        {
            MiniGameType[] types = (MiniGameType[])Enum.GetValues(typeof(MiniGameType));
            return types[UnityEngine.Random.Range(0, types.Length)];
        }
    }
}
