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

        // ── Last-operation costs (read by CollectionBootstrapper for spend-after-save) ──
        public int LastGoldCost { get; private set; }
        public int LastFragCost { get; private set; }

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

            if (!_wallet.CanAfford(goldCost, fragmentCost)) return false;

            LastGoldCost = goldCost;
            LastFragCost = fragmentCost;

            card.LevelUp();
            OnLevelUp?.Invoke(card, card.CurrentLevel);
            return true;
        }

        // ── Multiple level ups ────────────────────────────────────────────
        // Phase 1: accumulate how many levels are affordable without spending.
        // Phase 2: apply all LevelUp()s; CollectionBootstrapper spends after save.
        public int TryLevelUpBy(CardInstance card, int levels)
        {
            int cap = card.data.MaxLevelForTier(card.CurrentTier);

            int totalGold = 0, totalFrags = 0, affordable = 0;

            for (int i = 0; i < levels; i++)
            {
                int lvl = card.CurrentLevel + i;
                if (lvl >= cap) break;

                int goldCost = card.data.GoldCostForLevel(card.CurrentTier, lvl);
                int fragCost = card.data.FragmentCostForLevel(card.CurrentTier, lvl);

                if (_wallet.Gold < totalGold + goldCost || _wallet.Fragments < totalFrags + fragCost)
                    break;

                totalGold += goldCost;
                totalFrags += fragCost;
                affordable++;
            }

            if (affordable == 0) return 0;
            if (!_wallet.CanAfford(totalGold, totalFrags)) return 0;

            LastGoldCost = totalGold;
            LastFragCost = totalFrags;

            for (int i = 0; i < affordable; i++)
            {
                card.LevelUp();
                OnLevelUp?.Invoke(card, card.CurrentLevel);
            }

            return affordable;
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