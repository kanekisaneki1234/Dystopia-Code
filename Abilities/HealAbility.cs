using UnityEngine;
using Dystopia.Events;
using Dystopia.Cards;

namespace Dystopia.Abilities
{
    [CreateAssetMenu(menuName = "Dystopia/Abilities/Heal")]
    public class HealAbility : AbilityData
    {
        [Header("Heal Settings")]
        [Range(0f, 1f)]
        public float healPercent         = 0.2f;   // % of max HP restored
        public int   maxHeal             = 600;
        
        [Range(0f, 1f)]
        public float resonanceBoostedPercent = 0.35f;  // % at R2
        public int   resonanceBoostedMaxHeal = 750;

        private bool _resonanceBoosted = false;

        public override void Execute(AbilityContext ctx)
        {
            float percent = _resonanceBoosted ? resonanceBoostedPercent : healPercent;
            int   cap     = _resonanceBoosted ? resonanceBoostedMaxHeal : maxHeal;

            if (ctx.IsAmplified) percent *= ctx.AmplifyMult;

            int healAmount = Mathf.Min(Mathf.RoundToInt(ctx.CasterTeam.MaxHP * percent), cap);
            
            ctx.CasterTeam.Heal(healAmount);
            
            BattleEvents.OnAbilityFired?.Invoke(abilityName, healAmount);
        }

        public override void ApplyResonanceBoost() => _resonanceBoosted = true;

        public override string GetShortDescription(CardInstance caster)
        {
            float pct = _resonanceBoosted ? resonanceBoostedPercent : healPercent;
            int   cap = _resonanceBoosted ? resonanceBoostedMaxHeal : maxHeal;
            return $"Heal {pct * 100f:F0}% max HP (cap {cap})";
        }
    }
}