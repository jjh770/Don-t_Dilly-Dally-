public enum RoleType
{
    None = 0,
    Surgeon = 1,
    Assistant1 = 2,
    Assistant2 = 3,
    Assistant3 = 4
}

public static class RoleTypeExtensions
{
    public static int GetAssistantIndex(this RoleType role)
    {
        return role switch
        {
            RoleType.Assistant1 => 0,
            RoleType.Assistant2 => 1,
            RoleType.Assistant3 => 2,
            _ => -1
        };
    }

    public static RoleType GetAssistantRole(int index)
    {
        return index switch
        {
            0 => RoleType.Assistant1,
            1 => RoleType.Assistant2,
            2 => RoleType.Assistant3,
            _ => RoleType.None
        };
    }
}
