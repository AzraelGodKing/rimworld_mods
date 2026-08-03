namespace LivingWorld
{
    public enum WorldEventKind : byte
    {
        Skirmish = 0,
        DecisiveVictory = 1,
        FamineRumor = 2,
        Founding = 3,
        Collapse = 4,
        Alliance = 5,
        Betrayal = 6,
        OwnershipFlip = 7,
        ProsperityRise = 8,
        ProsperityFall = 9,
        OutpostFounded = 10,
        SettlementAbandoned = 11,
        Epithet = 12,
    }

    public enum NewsSeverity : byte
    {
        Minor = 0,
        Normal = 1,
        Major = 2,
    }
}
