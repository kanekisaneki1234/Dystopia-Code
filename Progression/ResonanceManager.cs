using System;
using Dystopia.Cards;
using Dystopia.Core;
using Dystopia.Economy.Services;

namespace Dystopia.Progression
{
    public class ResonanceManager
    {
        private readonly WalletService     _wallet;
        private readonly CollectionService _collection;

        // ── Events ────────────────────────────────────────────────────────
        public event Action<CardInstance, ResonanceLevel> OnResonanceUp;

        // ── Constructor ───────────────────────────────────────────────────
        public ResonanceManager(WalletService wallet, CollectionService collection)
        {
            _wallet     = wallet;
            _collection = collection;
        }

        // ── Duplicate costs (rank × resonance level) ──────────────────────
        private int DuplicatesRequired(CardRank rank, ResonanceLevel target)
        {
            return (rank, target) switch
            {
                (CardRank.D, ResonanceLevel.R1) => 16,
                (CardRank.D, ResonanceLevel.R2) => 32,
                (CardRank.D, ResonanceLevel.R3) => 64,
                (CardRank.D, ResonanceLevel.R4) => 128,

                (CardRank.C, ResonanceLevel.R1) => 8,
                (CardRank.C, ResonanceLevel.R2) => 16,
                (CardRank.C, ResonanceLevel.R3) => 32,
                (CardRank.C, ResonanceLevel.R4) => 64,

                (CardRank.B, ResonanceLevel.R1) => 4,
                (CardRank.B, ResonanceLevel.R2) => 8,
                (CardRank.B, ResonanceLevel.R3) => 16,
                (CardRank.B, ResonanceLevel.R4) => 32,

                (CardRank.A, ResonanceLevel.R1) => 2,
                (CardRank.A, ResonanceLevel.R2) => 4,
                (CardRank.A, ResonanceLevel.R3) => 8,
                (CardRank.A, ResonanceLevel.R4) => 16,

                (CardRank.S, ResonanceLevel.R1) => 1,
                (CardRank.S, ResonanceLevel.R2) => 2,
                (CardRank.S, ResonanceLevel.R3) => 4,
                (CardRank.S, ResonanceLevel.R4) => 8,

                _ => 0
            };
        }

        // ── Gold cost per resonance level ─────────────────────────────────
        private int GoldCost(ResonanceLevel target) => target switch
        {
            ResonanceLevel.R1 => 500,
            ResonanceLevel.R2 => 1000,
            ResonanceLevel.R3 => 2500,
            ResonanceLevel.R4 => 5000,
            _                 => 0
        };

        // ── Fragment cost per resonance level ─────────────────────────────
        private int FragmentCost(ResonanceLevel target) => target switch
        {
            ResonanceLevel.R1 => 50,
            ResonanceLevel.R2 => 100,
            ResonanceLevel.R3 => 200,
            ResonanceLevel.R4 => 400,
            _                 => 0
        };

        // ── Next resonance level ──────────────────────────────────────────
        private ResonanceLevel? NextLevel(ResonanceLevel current) => current switch
        {
            ResonanceLevel.None => ResonanceLevel.R1,
            ResonanceLevel.R1   => ResonanceLevel.R2,
            ResonanceLevel.R2   => ResonanceLevel.R3,
            ResonanceLevel.R3   => ResonanceLevel.R4,
            _                   => null
        };

        // ── Attempt resonance upgrade ─────────────────────────────────────
        public bool TryResonanceUp(CardInstance card)
        {
            var next = NextLevel(card.resonanceLevel);
            if (next == null) return false;

            ResonanceLevel target = next.Value;
            int dupsNeeded     = DuplicatesRequired(card.data.rank, target);
            int goldNeeded     = GoldCost(target);
            int fragmentsNeeded = FragmentCost(target);

            // Check duplicates first
            if (card.duplicateCount < dupsNeeded) return false;

            // Check currencies (atomic — both or neither)
            if (!_wallet.TrySpend(goldNeeded, fragmentsNeeded)) return false;

            // Consume duplicates
            card.duplicateCount -= dupsNeeded;

            // Apply resonance
            card.SetResonance(target);
            OnResonanceUp?.Invoke(card, target);
            return true;
        }

        // ── Validation (for UI) ───────────────────────────────────────────
        public bool CanAffordResonanceUp(CardInstance card)
        {
            var next = NextLevel(card.resonanceLevel);
            if (next == null) return false;

            ResonanceLevel target = next.Value;
            return card.duplicateCount >= DuplicatesRequired(card.data.rank, target)
                && _wallet.CanAfford(GoldCost(target), FragmentCost(target));
        }

        // ── Cost preview (for UI confirmation dialogs) ────────────────────
        public void PreviewResonanceCost(
            CardInstance card,
            out int duplicates,
            out int gold,
            out int fragments,
            out ResonanceLevel target)
        {
            var next = NextLevel(card.resonanceLevel);
            if (next == null)
            {
                duplicates = 0;
                gold       = 0;
                fragments  = 0;
                target     = card.resonanceLevel;
                return;
            }

            target     = next.Value;
            duplicates = DuplicatesRequired(card.data.rank, target);
            gold       = GoldCost(target);
            fragments  = FragmentCost(target);
        }

        // ── Status checks ─────────────────────────────────────────────────
        public bool IsMaxResonance(CardInstance card)
        {
            return card.resonanceLevel >= ResonanceLevel.R4;
        }

        public bool HasEnoughDuplicates(CardInstance card)
        {
            var next = NextLevel(card.resonanceLevel);
            if (next == null) return false;
            return card.duplicateCount >= DuplicatesRequired(card.data.rank, next.Value);
        }
    }
}