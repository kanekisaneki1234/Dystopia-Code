using Dystopia.Battle;

namespace Dystopia.Abilities
{
    public class AbilityContext
    {
        public BattleTeam CasterTeam;
        public BattleTeam TargetTeam;
        public int        CurrentTurn;
        public bool       IsAmplified;
        public float      AmplifyMult;
    }
}