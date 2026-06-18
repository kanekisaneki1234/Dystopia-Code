using UnityEngine;
using Dystopia.Events;
using Dystopia.Core;
using Dystopia.Cards;

namespace Dystopia.Abilities
{
    [CreateAssetMenu(menuName = "Dystopia/Abilities/Shield")]
    public class ShieldAbility : AbilityData
    {
        [Header("Shield Settings")]
        public StatType statType              = StatType.Defence;

        [Range(0f, 1f)]
        public float    statPercent           = 0.5f;    // 50% of DEF as shield value
        public int      maxShield             = 500;
        public int      durationTurns         = 2;

        [Header("Resonance Boost")]
        [Range(0f, 1f)]
        public float    resonanceBoostedStatPercent = 0.75f;
        public int      resonanceBoostedMaxShield   = 600;
        public int      resonanceBoostedDuration    = 3;

        private bool _resonanceBoosted = false;

        public override void Execute(AbilityContext ctx)
        {
            float percent  = _resonanceBoosted ? resonanceBoostedStatPercent : statPercent;
            int   duration = _resonanceBoosted ? resonanceBoostedDuration    : durationTurns;
            int   cap      = _resonanceBoosted ? resonanceBoostedMaxShield   : maxShield;

            int statValue = statType switch
            {
                StatType.Attack  => ctx.CasterTeam.AggregateAttack,
                StatType.Defence => ctx.CasterTeam.AggregateDefence,
                StatType.Speed   => ctx.CasterTeam.AggregateSpeed,
                _                => ctx.CasterTeam.AggregateDefence
            };

            if (ctx.IsAmplified) percent *= ctx.AmplifyMult;

            int shieldValue = Mathf.Min(Mathf.RoundToInt(statValue * percent), cap);
            ctx.CasterTeam.ApplyShield(shieldValue, duration);

            BattleEvents.OnAbilityFired?.Invoke(abilityName, shieldValue);
        }

        public override void ApplyResonanceBoost() => _resonanceBoosted = true;

        public override string GetShortDescription(CardInstance caster)
        {
            float pct      = _resonanceBoosted ? resonanceBoostedStatPercent : statPercent;
            int   duration = _resonanceBoosted ? resonanceBoostedDuration    : durationTurns;
            int   cap      = _resonanceBoosted ? resonanceBoostedMaxShield   : maxShield;
            return $"Shield {pct * 100f:F0}% {statType} for {duration}t (cap {cap})";
        }
    }
}