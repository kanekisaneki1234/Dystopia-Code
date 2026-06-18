using System.Collections.Generic;
using Dystopia.Core;

namespace Dystopia.Battle
{
    public static class TypeResolver
    {
        private const float Advantage    = 1.25f;
        private const float Disadvantage = 0.75f;
        private const float Neutral      = 1.0f;

        public static float GetMultiplier(CardType attacker, CardType defender)
        {
            return (attacker, defender) switch
            {
                //---------Fire----------
                (CardType.Fire,     CardType.Grass)    => Advantage,
                (CardType.Fire,     CardType.Water)    => Disadvantage,
                //---------Water----------
                (CardType.Water,    CardType.Fire)     => Advantage,
                (CardType.Water,    CardType.Grass)    => Disadvantage,
                (CardType.Water,    CardType.Ground)   => Advantage,
                //---------Grass----------
                (CardType.Grass,    CardType.Water)    => Advantage,
                (CardType.Grass,    CardType.Fire)     => Disadvantage,
                (CardType.Grass,    CardType.Ground)   => Advantage,
                //---------Electric----------
                (CardType.Electric, CardType.Water)    => Advantage,
                (CardType.Electric, CardType.Ground)   => Disadvantage,
                //---------Ground----------
                (CardType.Ground,   CardType.Electric) => Advantage,
                (CardType.Ground,   CardType.Fire)     => Advantage,
                (CardType.Ground,   CardType.Grass)    => Disadvantage,
                //---------Light & Dark----------
                (CardType.Light,    CardType.Dark)     => Advantage,
                (CardType.Dark,     CardType.Light)    => Advantage,
                //---------Everything Else----------
                var (a, d) when a == d                 => Neutral,
                _                                      => Neutral
            };
        }

        // Calculates the average type multiplier across all attacker cards
        // against all defender cards. Every card contributes equally to the
        // aggregate — no single card can dominate or nullify the calculation.
        public static void ApplyMultipliers(BattleTeam player, BattleTeam opponent)
        {
            player.TypeMultiplier   = CalculateAggregateMultiplier(player, opponent);
            opponent.TypeMultiplier = CalculateAggregateMultiplier(opponent, player);
        }

        private static float CalculateAggregateMultiplier(
            BattleTeam attacker,
            BattleTeam defender)
        {
            if (attacker.Cards == null || attacker.Cards.Count == 0) return Neutral;
            if (defender.Cards  == null || defender.Cards.Count  == 0) return Neutral;

            float total = 0f;
            int   count = 0;

            foreach (var attackCard in attacker.Cards)
            {
                foreach (var defendCard in defender.Cards)
                {
                    total += GetMultiplier(
                        attackCard.data.cardType,
                        defendCard.data.cardType);
                    count++;
                }
            }

            return total / count;
        }
    }
}