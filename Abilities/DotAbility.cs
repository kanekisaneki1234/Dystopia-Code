using UnityEngine;
using Dystopia.Events;
using Dystopia.Core;
using Dystopia.Cards;

namespace Dystopia.Abilities
{
    [CreateAssetMenu(menuName = "Dystopia/Abilities/DoT")]
    public class DotAbility : AbilityData
    {
        [Header("DoT Settings")]
        public bool  trueDamage               = true;
        public StatType statType              = StatType.Attack;

        [Range(0f, 1f)]
        public float statPercent              = 0.3f;   // 30% of ATK as base damage
        [Range(0f, 1f)]
        public float    damageScalePercent    = 0.2f;    // 20% increase per reapplication
        public int      maxDamage             = 400;     // fixed cap across all tiers
        [Header("Resonance Boost")]
        [Range(0f, 1f)]
        public float resonanceBoostedStatPercent    = 0.45f;
        public int      resonanceBoostedMaxDamage   = 600;   // R2 raises the cap

        private bool _resonanceBoosted = false;

        public override void Execute(AbilityContext ctx)
        {
            float basePercent = _resonanceBoosted
                ? resonanceBoostedStatPercent
                : statPercent;

            int cap = _resonanceBoosted
                ? resonanceBoostedMaxDamage
                : maxDamage;

            int statValue = statType switch
            {
                StatType.Attack  => ctx.CasterTeam.AggregateAttack,
                StatType.Defence => ctx.CasterTeam.AggregateDefence,
                StatType.Speed   => ctx.CasterTeam.AggregateSpeed,
                _                => ctx.CasterTeam.AggregateAttack
            };

            int damage = ctx.TargetTeam.CurrentDotDamage > 0
                ? Mathf.RoundToInt(ctx.TargetTeam.CurrentDotDamage * (1f + damageScalePercent))
                : Mathf.RoundToInt(statValue * basePercent);

            if (ctx.IsAmplified) damage = Mathf.RoundToInt(damage * ctx.        AmplifyMult);

            damage = Mathf.Min(damage, cap);

            ctx.TargetTeam.ApplyDot(damage, trueDamage);
            BattleEvents.OnAbilityFired?.Invoke(abilityName, damage);
        }

        public override void ApplyResonanceBoost() => _resonanceBoosted = true;

        public override string GetShortDescription(CardInstance caster)
        {
            float pct     = _resonanceBoosted ? resonanceBoostedStatPercent : statPercent;
            int   cap     = _resonanceBoosted ? resonanceBoostedMaxDamage   : maxDamage;
            string dmgTag = trueDamage ? "true " : "";
            return $"{dmgTag}DoT {pct * 100f:F0}% {statType}/turn (cap {cap})";
        }
    }
}