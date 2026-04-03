using UnityEngine;

namespace DontDillyDally.StageFlow
{
    public class EmergencyEventPolicy
    {
        private float _timeSinceLastRandomCheck;

        public bool ShouldTriggerOnRecipeFail(StageRuntimeData stageData)
        {
            return Random.value <= GetSettings(stageData).RecipeFailTriggerChance;
        }

        public bool ShouldTriggerOnMiniGameFail(StageRuntimeData stageData)
        {
            return Random.value <= GetSettings(stageData).MiniGameFailTriggerChance;
        }

        public bool ShouldTriggerRandom(StageRuntimeData stageData, float deltaTime)
        {
            StageEmergencySettings settings = GetSettings(stageData);
            _timeSinceLastRandomCheck += deltaTime;

            if (_timeSinceLastRandomCheck < settings.RandomCheckIntervalSec)
            {
                return false;
            }

            _timeSinceLastRandomCheck = 0f;
            return Random.value <= settings.RandomTriggerChance;
        }

        public void ResetTimer()
        {
            _timeSinceLastRandomCheck = 0f;
        }

        private static StageEmergencySettings GetSettings(StageRuntimeData stageData)
        {
            return stageData?.Settings?.EmergencySettings ?? new StageEmergencySettings();
        }
    }
}
