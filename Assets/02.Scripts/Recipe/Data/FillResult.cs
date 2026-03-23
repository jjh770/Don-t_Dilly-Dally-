namespace DontDillyDally.Data
{
    public readonly struct FillResult
    {
        public readonly bool Success;
        public readonly ToolType UsedToolsMask;
        public readonly CraftedMaterialType ResultMaterial;
        public readonly float FillDuration;

        private FillResult(bool canFill, ToolType usedToolsMask, CraftedMaterialType resultMaterial, float fillDuration)
        {
            Success = canFill;
            UsedToolsMask = usedToolsMask;
            ResultMaterial = resultMaterial;
            FillDuration = fillDuration;
        }

        public static FillResult Succeed(ToolType usedToolsMask, CraftedMaterialType resultMaterial, float fillDuration)
            => new FillResult(true, usedToolsMask, resultMaterial, fillDuration);

        public static FillResult Failure()
            => new FillResult(false, ToolType.None, CraftedMaterialType.Unknown, 0f);
    }
}
