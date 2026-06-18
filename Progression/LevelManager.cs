using System;
using Dystopia.Cards;
using Dystopia.Economy.Services;
using Dystopia.Events;

namespace Dystopia.Progression
{
    public class LevelManager
    {
        private readonly WalletService _wallet;

        // ── Events ────────────────────────────────────────────────────────
        public event Action<CardInstance, int> OnLevelUp;  // card, newLevel

        // ── Constructor ───────────────────────────────────────────────────
        public LevelManager(WalletService wallet)
        {
            _wallet = wallet;
        }

        // ── Single level up ───────────────────────────────────────────────
        public bool TryLevelUp(CardInstance card)
        {
            int tier = card.CurrentTier;
            int level = card.CurrentLevel;

            if (level >= card.data.MaxLevelForTier(tier)) return false;

            int fragmentCost = card.data.FragmentCostForLevel(tier, level);
            int goldCost     = card.data.GoldCostForLevel(tier, level);

            if (!_wallet.TrySpend(goldCost, fragmentCost)) return false;

            card.LevelUp();
            OnLevelUp?.Invoke(card, card.CurrentLevel);
            return true;
        }

        // ── Multiple level ups ────────────────────────────────────────────
        public int TryLevelUpBy(CardInstance card, int levels)
        {
            int levelsGained = 0;

            for (int i = 0; i < levels; i++)
            {
                if (!TryLevelUp(card)) break;
                levelsGained++;
            }

            return levelsGained;
        }

        // ── Level to max within current tier ──────────────────────────────
        public int TryLevelUpToMax(CardInstance card)
        {
            int remaining = card.data.MaxLevelForTier(card.CurrentTier) - card.CurrentLevel;
            return TryLevelUpBy(card, remaining);
        }

        // ── Cost preview (for UI confirmation dialogs) ────────────────────
        public void PreviewLevelUpCost(CardInstance card, out int fragments, out int gold)
        {
            fragments = card.data.FragmentCostForLevel(card.CurrentTier, card.CurrentLevel);
            gold      = card.data.GoldCostForLevel(card.CurrentTier, card.CurrentLevel);
        }

        public void PreviewCostToMax(CardInstance card, out int fragments, out int gold)
        {
            int cap = card.data.MaxLevelForTier(card.CurrentTier);
            fragments = card.FragmentCostForLevels(cap);
            gold      = card.GoldCostForLevels(cap);
        }

        public bool CanAffordLevelUp(CardInstance card)
        {
            int fragmentCost = card.data.FragmentCostForLevel(card.CurrentTier, card.CurrentLevel);
            int goldCost     = card.data.GoldCostForLevel(card.CurrentTier, card.CurrentLevel);
            return _wallet.CanAfford(goldCost, fragmentCost);
        }
    }
}