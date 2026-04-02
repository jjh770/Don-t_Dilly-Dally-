using System;

public readonly struct HospitalLevel
{
    public int Value { get; }

    public HospitalLevel(int value)
    {
        Value = Math.Max(0, value);
    }

    public static HospitalLevel Default => new(1);

    public HospitalLevel Upgrade() => new(Value + 1);
    public bool IsMaxLevel(HospitalLevelCatalogSO catalog)
        => catalog.GetNextLevel(Value) == null;
}