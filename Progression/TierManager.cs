using System;
using Dystopia.Cards;
using Dystopia.Core;
using Dystopia.Economy.Services;

namespace Dystopia.Progression
{
    public class TierManager
    {
        private readonly WalletService _wallet;

        // ── Events ────────────────────────────────────────────────────────
        public event Action<CardInstance, int> OnTierUpgrade;  // card, newTier

        // ── Constructor ───────────────────────────────────────────────────
        public TierManager(WalletService wallet)
        {
            _wallet = wallet;
        }

        // ── Tier upgrade costs (from your economy design) ─────────────────
        private int FragmentCost(int currentTier) => currentTier switch
        {
            1 => 100,
            2 => 250,
            _ => 0
        };

        private int GoldCost(int currentTier) => currentTier switch
        {
            1 => 5000,
            2 => 15000,
            _ => 0
        };

        private int MaterialCost(int currentTier) => currentTier switch
        {
            1 => 3,
            2 => 6,
            _ => 0
        };

        // ── Attempt tier upgrade ──────────────────────────────────────────
        public bool TryTierUpgrade(CardInstance card)
        {
            int tier = card.CurrentTier;

            // Already at max tier
            if (tier >= 3) return false;

            // Must be at level cap for current tier
            if (card.CurrentLevel < card.data.MaxLevelForTier(tier)) return false;

            int fragments = FragmentCost(tier);
            int gold      = GoldCost(tier);
            int materials = MaterialCost(tier);

            if (!_wallet.TrySpendTierUpgrade(gold, fragments, card.data.cardClass, materials))
                return false;

            card.TierUpgrade();
            OnTierUpgrade?.Invoke(card, card.CurrentTier);
            return true;
        }

        // ── Validation (for UI) ───────────────────────────────────────────
        public bool CanAffordTierUpgrade(CardInstance card)
        {
            int tier = card.CurrentTier;
            if (tier >= 3) return false;
            if (card.CurrentLevel < card.data.MaxLevelForTier(tier)) return false;

            return _wallet.CanAffordTierUpgrade(
                GoldCost(tier),
                FragmentCost(tier),
                card.data.cardClass,
                MaterialCost(tier));
        }

        // ── Cost preview (for UI confirmation dialogs) ────────────────────
        public void PreviewTierUpgradeCost(CardInstance card, out int fragments, out int gold, out int materials)
        {
            int tier  = card.CurrentTier;
            fragments = FragmentCost(tier);
            gold      = GoldCost(tier);
            materials = MaterialCost(tier);
        }

        // ── Status check ──────────────────────────────────────────────────
        public bool IsAtLevelCap(CardInstance card)
        {
            return card.CurrentLevel >= card.data.MaxLevelForTier(card.CurrentTier);
        }

        public bool IsMaxTier(CardInstance card)
        {
            return card.CurrentTier >= 3;
        }
    }
}