using UnityEngine;

namespace DontDillyDally.Data
{
    public class PotionMixingMachine : MonoBehaviour
    {
        [SerializeField] private CraftingMachine _craftingMachine;

        public CraftingAttemptResult TryMixPotions(
            ToolType primaryPotion,
            ToolType secondaryPotion,
            int playerId)
        {
            if (_craftingMachine == null)
            {
                return new CraftingAttemptResult
                {
                    Success = false,
                    FailureReason = CraftingFailureReason.MissingDatabase,
                    ResultMaterial = CraftedMaterialType.Unknown
                };
            }

            return _craftingMachine.TryCraft(
                primaryPotion,
                secondaryPotion,
                ActionType.MixPotion,
                playerId);
        }
    }
}
