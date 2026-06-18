namespace Dystopia.Core
{
    public enum CardRank    { D, C, B, A, S }
    public enum CardClass   { Assassin, Tank, Mage, Healer, Support, Guardian }
    public enum CardType    { Fire, Water, Grass, Electric, Ground, Light, Dark, Neutral }
    public enum AbilityKind { Active, Passive }
    public enum BattleMode  { OneVsOne, ThreeVsThree, Raid, Dungeon }

    public enum BattleState
    {
        Idle,
        Setup,
        RoundStart,
        AbilityWindow,
        Resolution,
        RoundEnd,
        Victory,
        Defeat,
        Draw
    }

    public enum ItemId
    {
        HealingShard,
        ManaCrystal,
        IronWall,
        SpeedSurge,
        AbilityAmplifier,
        ManaDrain
    }

    public enum TierLevel      { I = 1, II = 2, III = 3 }
    public enum ResonanceLevel { None = 0, R1 = 1, R2 = 2, R3 = 3, R4 = 4 }
    public enum StatType { Attack, Defence, Speed }
}
