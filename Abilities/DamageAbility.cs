using UnityEngine;
using Dystopia.Battle;
using Dystopia.Cards;

namespace Dystopia.Abilities
{
    [CreateAssetMenu(menuName = "Dystopia/Abilities/Damage")]
    public class DamageAbility : AbilityData
    {
        [Header("Damage Settings")]
        public float damageMultiplier     = 0.5f;
        public int damageCap              = 500;

        [Header("Resonance Boost")]
        public float resonanceBoostedMult = 0.75f;
        public int resonanceBoostedCap    = 750;
        private bool _resonanceBoosted = false;

        public override void Execute(AbilityContext ctx)
        {
            float mult = _resonanceBoosted ? resonanceBoostedMult : damageMultiplier;
            int   cap  = _resonanceBoosted ? resonanceBoostedCap  : damageCap;

            int bonusDamage = Mathf.RoundToInt(
                BattleSimulator.CalculateDamage(
                    ctx.CasterTeam.AggregateAttack,
                    ctx.TargetTeam.AggregateDefence,
                    ctx.TargetTeam.TypeMultiplier) * mult);

            bonusDamage = Mathf.Min(bonusDamage, cap);
            ctx.TargetTeam.TakeDamage(bonusDamage);
        }

        public override void ApplyResonanceBoost() => _resonanceBoosted = true;

        public override string GetShortDescription(CardInstance caster)
        {
            float mult = _resonanceBoosted ? resonanceBoostedMult : damageMultiplier;
            int   cap  = _resonanceBoosted ? resonanceBoostedCap  : damageCap;
            return $"Deal {mult * 100f:F0}% atk dmg (cap {cap})";
        }
    }
}