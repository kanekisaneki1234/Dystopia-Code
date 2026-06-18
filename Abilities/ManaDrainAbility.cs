using UnityEngine;
using Dystopia.Events;
using Dystopia.Cards;

namespace Dystopia.Abilities
{
    [CreateAssetMenu(menuName = "Dystopia/Abilities/Mana Drain")]
    public class ManaDrainAbility : AbilityData
    {
        [Header("Mana Drain Settings")]
        [Range(0f, 1f)]
        public float drainPercent           = 0.3f;
        public int   maxDrain                  = 40;
        [Header("Resonance Boost")]
        [Range(0f, 1f)]
        public float resonanceBoostedPercent   = 0.45f;
        public int   resonanceBoostedMaxDrain  = 60;

        private bool _resonanceBoosted = false;

        public override void Execute(AbilityContext ctx)
        {
            float pct = _resonanceBoosted ? resonanceBoostedPercent : drainPercent;
            int   cap = _resonanceBoosted ? resonanceBoostedMaxDrain : maxDrain;

            int drain = Mathf.RoundToInt(ctx.TargetTeam.CurrentMana * pct);
            drain = Mathf.Min(drain, cap);

            if (ctx.IsAmplified) drain = Mathf.RoundToInt(drain * ctx.AmplifyMult);
            
            int actualDrain = Mathf.Min(drain, ctx.TargetTeam.CurrentMana);

            ctx.TargetTeam.SpendMana(actualDrain);
            ctx.CasterTeam.AddMana(actualDrain);

            BattleEvents.OnAbilityFired?.Invoke(abilityName, actualDrain);
        }

        public override void ApplyResonanceBoost() => _resonanceBoosted = true;

        public override string GetShortDescription(CardInstance caster)
        {
            float pct = _resonanceBoosted ? resonanceBoostedPercent : drainPercent;
            int   cap = _resonanceBoosted ? resonanceBoostedMaxDrain : maxDrain;
            return $"Drain {pct * 100f:F0}% enemy mana (cap {cap})";
        }
    }
}