using UnityEngine;
using Dystopia.Core;
using Dystopia.Events;
using Dystopia.Battle;
using Dystopia.Cards;

namespace Dystopia.Abilities
{
    [CreateAssetMenu(menuName = "Dystopia/Abilities/Stat Mod")]
    public class StatModAbility : AbilityData
    {
        [Header("Stat Mod Settings")]
        public StatType stat          = StatType.Attack;
        public float    multiplier    = 1.3f;  // >1 = buff, <1 = debuff
        public int      durationTurns = 3;
        public bool     targetOpponent = false; // false = buff self, true = debuff opponent

        public int resonanceBoostedDuration = 5;
        private bool _resonanceBoosted = false;

        public override void Execute(AbilityContext ctx)
        {
            int duration = _resonanceBoosted ? resonanceBoostedDuration : durationTurns;
            BattleTeam target = targetOpponent ? ctx.TargetTeam : ctx.CasterTeam;
            target.ApplyStatMod(stat, multiplier, duration);

            BattleEvents.OnAbilityFired?.Invoke(abilityName, duration);
        }

        public override void ApplyResonanceBoost() => _resonanceBoosted = true;

        public override string GetShortDescription(CardInstance caster)
        {
            int    duration = _resonanceBoosted ? resonanceBoostedDuration : durationTurns;
            string effect   = multiplier >= 1f ? "Buff" : "Debuff";
            string target   = targetOpponent ? "enemy" : "self";
            return $"{effect} {target} {stat} ×{multiplier:F2} for {duration}t";
        }
    }
}