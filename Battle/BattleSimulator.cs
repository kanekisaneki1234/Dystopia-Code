using UnityEngine;
using Dystopia.Core;
using Dystopia.Cards;
using Dystopia.Abilities;
using Dystopia.Events;
using Dystopia.Battle.Items;

namespace Dystopia.Battle
{
    public static class BattleSimulator
    {
        public static int CalculateDamage(int attack, int defence, float typeMult = 1f)
        {
            float a     = attack;
            float d     = defence;
            float term1 = (a / d) * (a / 2.9f);
            float term2 = 2200f / d;
            float raw   = (term1 + term2) / 3f * typeMult;
            return Mathf.Max(1, Mathf.RoundToInt(raw));
        }

        // ── Resolve one full round ────────────────────────────────────────
        public static void ResolveRound(
            BattleTeam player,
            BattleTeam opponent,
            CardInstance playerAbilityOverride)
        {
            if (player.EquippedItem != null) player.EquippedItem.TryActivate(player, opponent);
            if (opponent.EquippedItem != null) opponent.EquippedItem.TryActivate(opponent, player);

            bool playerFirst;
            if (player.ForceFirstStrike && !opponent.ForceFirstStrike)
                playerFirst = true;
            else if (opponent.ForceFirstStrike && !player.ForceFirstStrike)
                playerFirst = false;
            else if (player.AggregateSpeed != opponent.AggregateSpeed)
                playerFirst = player.AggregateSpeed > opponent.AggregateSpeed;
            else if (player.CurrentHP != opponent.CurrentHP)
                playerFirst = player.CurrentHP < opponent.CurrentHP;
            else
                playerFirst = Random.value >= 0.5f;

            player.ForceFirstStrike   = false;
            opponent.ForceFirstStrike = false;

            BattleTeam first  = playerFirst ? player   : opponent;
            BattleTeam second = playerFirst ? opponent  : player;

            CardInstance firstOverride = first == player ? playerAbilityOverride : null;
            AttackPhase(first, second, firstOverride);

            if (second.IsDefeated) return;

            CardInstance secondOverride = second == player ? playerAbilityOverride : null;
            AttackPhase(second, first, secondOverride);
        }

        // ── Single team attack phase ──────────────────────────────────────
        private static void AttackPhase(
            BattleTeam attacker,
            BattleTeam defender,
            CardInstance abilityOverride)
        {
            int basicDamage = CalculateDamage(
                attacker.AggregateAttack,
                defender.AggregateDefence,
                attacker.TypeMultiplier);

            defender.TakeDamage(basicDamage);

            CardInstance abilityCard = abilityOverride ?? attacker.GetAbilityCardForTurn();

            if (abilityCard == null) return;

            int cost = Mathf.RoundToInt(
                abilityCard.data.ability.ManaCost * abilityCard.ManaCostMultiplier);

            if (attacker.CurrentMana < cost) return;

            attacker.SpendMana(cost);

            bool  isAmplified = false;
            float amplifyMult = 1.0f;

            if (attacker.EquippedItem != null)
            {
                isAmplified = attacker.EquippedItem.IsAmplifier;
                if (attacker.EquippedItem is AbilityAmplifier amp)
                    amplifyMult = amp.amplifyMultiplier;
            }

            var ctx = new AbilityContext
            {
                CasterTeam  = attacker,
                TargetTeam  = defender,
                CurrentTurn = 0,
                IsAmplified = isAmplified,
                AmplifyMult = amplifyMult
            };

            abilityCard.data.ability.Execute(ctx);

            attacker.AdvanceAbilitySlot(abilityCard);
        }

        // ── Trigger passive abilities for both teams ──────────────────────
        public static void TriggerPassives(BattleTeam player, BattleTeam opponent)
        {
            TriggerTeamPassives(player, opponent);
            TriggerTeamPassives(opponent, player);
        }

        private static void TriggerTeamPassives(BattleTeam caster, BattleTeam target)
        {
            foreach (var card in caster.GetPassiveCards())
            {
                var ctx = new AbilityContext
                {
                    CasterTeam  = caster,
                    TargetTeam  = target,
                    CurrentTurn = 0,
                    IsAmplified = false,
                    AmplifyMult = 1.0f
                };

                card.data.ability.Execute(ctx);
            }
        }
    }
}