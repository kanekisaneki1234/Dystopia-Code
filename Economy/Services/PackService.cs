using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Dystopia.Cards;
using Dystopia.Core;
using Dystopia.Economy.Data;

namespace Dystopia.Economy.Services
{
    public class PackService
    {
        private readonly WalletService     _wallet;
        private readonly CollectionService _collection;
        private readonly CardData[]        _allCards;

        // ── Events ────────────────────────────────────────────────────
        public event Action<CardInstance> OnNewCardUnlocked;
        public event Action<CardInstance> OnDuplicateGained;

        // ── Today's generated contents per pack ───────────────────────
        private Dictionary<PackData, List<CardData>> _dailyContents = new();

        // ── Constructor ───────────────────────────────────────────────
        public PackService(WalletService wallet, CollectionService collection, CardData[] allCards)
        {
            _wallet     = wallet;
            _collection = collection;
            _allCards   = allCards;
        }

        // ── Generate daily contents for a pack ────────────────────────
        public List<CardData> GenerateDailyContents(PackData pack)
        {
            var contents = new List<CardData>();

            foreach (var entry in pack.rankEntries)
            {
                if (entry.appearanceChance < 1f && UnityEngine.Random.value > entry.appearanceChance)
                    continue;

                var pool = _allCards.Where(c => c.rank == entry.rank).ToArray();
                if (pool.Length == 0) continue;

                int count = UnityEngine.Random.Range(entry.minCards, entry.maxCards + 1);

                for (int i = 0; i < count; i++)
                    contents.Add(pool[UnityEngine.Random.Range(0, pool.Length)]);
            }

            _dailyContents[pack] = contents;
            return contents;
        }

        // ── Generate contents for all packs at once ───────────────────
        public void GenerateAllDailyContents(PackData[] packs)
        {
            _dailyContents.Clear();
            foreach (var pack in packs)
                GenerateDailyContents(pack);
        }

        // ── View today's contents (for UI) ────────────────────────────
        public List<CardData> GetDailyContents(PackData pack)
        {
            return _dailyContents.TryGetValue(pack, out var contents)
                ? contents
                : null;
        }

        // ── Check affordability ───────────────────────────────────────
        public bool CanAffordPack(PackData pack)
        {
            return _wallet.Diamonds >= pack.diamondCost;
        }

        // ── Purchase pack (get everything shown) ──────────────────────
        public List<CardInstance> TryBuyPack(PackData pack)
        {
            if (!_dailyContents.TryGetValue(pack, out var contents)) return null;
            if (contents.Count == 0) return null;
            if (!_wallet.TrySpendDiamonds(pack.diamondCost)) return null;

            var results = new List<CardInstance>();

            foreach (var cardData in contents)
                results.Add(ClaimCard(cardData));

            // Clear this pack's contents after purchase
            _dailyContents.Remove(pack);

            return results;
        }

        // ── Claim a single card (handles unlock vs duplicate) ─────────
        private CardInstance ClaimCard(CardData cardData)
        {
            var existing = _collection.GetOwnedCard(cardData);

            if (existing != null)
            {
                existing.AddDuplicate();
                OnDuplicateGained?.Invoke(existing);
                return existing;
            }

            var newCard = _collection.AddCard(cardData);
            OnNewCardUnlocked?.Invoke(newCard);
            return newCard;
        }
    }
}