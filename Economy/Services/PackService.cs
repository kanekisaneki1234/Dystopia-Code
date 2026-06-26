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
            return _dailyContents.TryGetValue(pack, out var contents) ? contents : null;
        }

        // ── Check affordability ───────────────────────────────────────
        public bool CanAffordPack(PackData pack)
        {
            return _wallet.Diamonds >= pack.diamondCost;
        }

        // ── Purchase pack ─────────────────────────────────────────────
        // Cards are claimed sequentially to avoid hitting PlayFab Cloud Script rate limits.
        public void TryBuyPack(PackData pack, Action<List<CardInstance>> onComplete)
        {
            if (!_dailyContents.TryGetValue(pack, out var contents) || contents.Count == 0)
            {
                onComplete?.Invoke(null);
                return;
            }
            if (!_wallet.TrySpendDiamonds(pack.diamondCost))
            {
                onComplete?.Invoke(null);
                return;
            }

            _dailyContents.Remove(pack);

            var cards   = new List<CardData>(contents);
            var results = new List<CardInstance>();
            ClaimNext(cards, 0, results, onComplete);
        }

        private void ClaimNext(List<CardData> cards, int index,
            List<CardInstance> results, Action<List<CardInstance>> onComplete)
        {
            if (index >= cards.Count)
            {
                onComplete?.Invoke(results);
                return;
            }

            bool isNew = !_collection.OwnsCardOfType(cards[index]);

            _collection.ClaimCard(cards[index], card =>
            {
                if (card != null)
                {
                    results.Add(card);
                    if (isNew) OnNewCardUnlocked?.Invoke(card);
                    else       OnDuplicateGained?.Invoke(card);
                }

                ClaimNext(cards, index + 1, results, onComplete);
            });
        }
    }
}
