namespace Dystopia.Battle
{
    public class BattleResult
    {
        public string    Outcome;              // "Victory", "Defeat", "Draw"
        public BattleTeam PlayerTeam;
        public BattleTeam OpponentTeam;
        public int        RoundsPlayed;
        public int        PlayerStatTotal;
        public int        OpponentStatTotal;
        public float      DifficultyMultiplier;
        public int        GoldReward;
        public int        FragmentReward;

        public bool PlayerWon => Outcome == "Victory";
        public bool IsDraw    => Outcome == "Draw";
    }
}
